import numpy as np
from network import ActorCritic
from utils import Memory
from mlagents_envs.environment import ActionTuple
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
import matplotlib.pyplot as plt
import torch
import torch.nn as nn
import argparse
from tqdm import tqdm

class PPO:
    def __init__(self, state_dim, action_dim, action_std, lr, betas, gamma, K_epochs, eps_clip, device):
        self.lr = lr
        self.betas = betas
        self.gamma = gamma
        self.eps_clip = eps_clip
        self.K_epochs = K_epochs

        self.policy = ActorCritic(state_dim, action_dim, action_std, device).to(device)
        self.optimizer = torch.optim.Adam(self.policy.parameters(), lr=lr, betas=betas)

        self.policy_old = ActorCritic(state_dim, action_dim, action_std, device).to(device)
        self.policy_old.load_state_dict(self.policy.state_dict())

        self.MseLoss = nn.MSELoss()

    def select_action(self, state, memory):
        state = torch.FloatTensor(state.reshape(1, -1)).to(device)
        return self.policy_old.act(state, memory).cpu().data.numpy().flatten()

    def update(self, memory):
        # Monte Carlo estimate of rewards:
        rewards = []
        discounted_reward = 0
        for reward, is_terminal in zip(reversed(memory.rewards), reversed(memory.is_terminals)):
            if is_terminal:
                discounted_reward = 0
            discounted_reward = reward + (self.gamma * discounted_reward)
            rewards.insert(0, discounted_reward)

        # Normalizing the rewards:
        rewards = torch.tensor(rewards, dtype=torch.float32).to(device)
        rewards = (rewards - rewards.mean()) / (rewards.std() + 1e-5)

        # convert list to tensor
        # 使用stack可以保留两个信息：[1. 序列] 和 [2. 张量矩阵] 信息，属于【扩张再拼接】的函数；
        old_states = torch.squeeze(torch.stack(memory.states).to(device), 1).detach()
        old_actions = torch.squeeze(torch.stack(memory.actions).to(device), 1).detach()
        old_logprobs = torch.squeeze(torch.stack(memory.logprobs), 1).to(device).detach()

        # Optimize policy for K epochs:
        for _ in range(self.K_epochs):
            # Evaluating old actions and values :
            logprobs, state_values, dist_entropy = self.policy.evaluate(old_states, old_actions)

            # Finding the ratio (pi_theta / pi_theta__old):
            ratios = torch.exp(logprobs - old_logprobs.detach())

            # Finding Surrogate Loss:
            advantages = rewards - state_values.detach()
            surr1 = ratios * advantages
            surr2 = torch.clamp(ratios, 1 - self.eps_clip, 1 + self.eps_clip) * advantages
            loss = -torch.min(surr1, surr2) + 0.5 * self.MseLoss(state_values, rewards) - 0.01 * dist_entropy

            # take gradient step
            self.optimizer.zero_grad()
            loss.mean().backward()
            self.optimizer.step()

        # Copy new weights into old policy:
        self.policy_old.load_state_dict(self.policy.state_dict())


def get_states(env):
    decision_steps, terminal_steps = env.get_steps(behavior_name)
    for agent_id_decisions in decision_steps:
        observation = decision_steps[agent_id_decisions].obs[0]
        reward = decision_steps[agent_id_decisions].reward
        terminated = False

    for agent_id_terminated in terminal_steps:
        observation =  terminal_steps[agent_id_terminated].obs[0]
        reward = terminal_steps[agent_id_terminated].reward
        terminated = not terminal_steps[agent_id_terminated].interrupted
    return observation, reward, terminated

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Training arguments')
    parser.add_argument('--episodes', type=int, default=50000, help='Number of episodes to train the model')
    parser.add_argument('--max-timesteps', type=int, default=20000, help='Number of steps to run the simulation')
    parser.add_argument('--k-epoch', type=int, default=5, help='Number of epochs to update the model')
    parser.add_argument('--gamma', type=float, default=0.90, help='Discount factor for computing expected Q values')
    parser.add_argument('--clip', type=float, default=0.2, help='Clipping factor for computing expected Q values')
    parser.add_argument('--lr', type=float, default=3.0e-4, help='Learning rate for optimizer')
    parser.add_argument('--no-render', action='store_true', default=False, help='Render the environment')
    parser.add_argument('--seed', type=int, default=0, help='Seed for random number generator')
    parser.add_argument('--test', action='store_true', default=False, help='Test the model')
    parser.add_argument('--time-scale', type=float, default=20.0, help='Time scale for unity')
    
    solved_reward = 0.94  # stop training if avg_reward > solved_reward
    log_interval = 50  # print avg reward in the interval
    action_std = 0.5  # constant std for action distribution (Multivariate Normal)
    update_timestep = 50  # update policy every n timesteps
    betas = (0.9, 0.999)
    env_name = "RollerBall"
    args = parser.parse_args()
    
    max_episodes = args.episodes
    max_timesteps = args.max_timesteps
    lr = args.lr
    K_epochs = args.k_epoch
    gamma = args.gamma
    eps_clip = args.clip
    
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    # Create a Unity Environment
    try:
        env.close()
    except:
        pass
    # -----------------
    # Create the GridWorld Environment from the registry
    channel = EngineConfigurationChannel()
    env = UnityEnvironment(base_port = 5006, file_name="/home/whale/下载/UnityBuild/USV.x86_64", \
        seed=args.seed, no_graphics=args.no_render, side_channels=[channel])
    # set time scale of the environment to speed the train up
    channel.set_configuration_parameters(time_scale = args.time_scale)
    print("RollerBall environment created.")

    state_dim = 6
    action_dim = 2
    
    env.reset()
    behavior_name = list(env.behavior_specs)[0]
    spec = env.behavior_specs[behavior_name]
    
    try:    
        memory = Memory()
        ppo = PPO(state_dim, action_dim, action_std, lr, betas, gamma, K_epochs, eps_clip, device)
        print(lr, betas)

        # logging variables
        running_reward = 0
        avg_length = 0
        time_step = 0
        reward_buf = []
        pbar = tqdm(range(max_episodes))
        for i_episode in pbar:
            state, r, done = get_states(env)
            for t in range(max_timesteps):
                time_step += 1
                # Running policy_old:
                if time_step < 5000:
                    action = np.random.uniform(-1, 1, action_dim)
                else:
                    noise = np.random.normal(0, 1 * 0.1, size=action_dim).clip(-1, 1)
                    action = ppo.select_action(state, memory) + noise
                action = np.array([action])
                action_tuple = ActionTuple()
                action_tuple.add_continuous(action)
                env.set_actions(behavior_name, action_tuple)
                # Perform a step in the simulation
                env.step()
                state, reward, done = get_states(env)
                # Saving reward and is_terminals:
                memory.rewards.append(reward)
                memory.is_terminals.append(done)
                # update if its time
                if time_step % update_timestep == 0:
                    ppo.update(memory)
                    memory.clear_memory()
                    time_step = 0
                running_reward += reward
                if done:
                    env.reset()
                    break

            avg_length += t

            # stop training if avg_reward > solved_reward
            if running_reward > (log_interval * solved_reward):
                print("########## Solved! ##########")
                torch.save(ppo.policy.state_dict(), './PPO_continuous_solved_{}_{}_{}.pth'.format(env_name, i_episode, running_reward))
                break

            # save every 500 episodes
            # if i_episode % 100 == 0:
            #     torch.save(ppo.policy.state_dict(), './PPO_continuous_{}_{}_{}.pth'.format(env_name, i_episode, running_reward))
            plt.clf()
            # logging
            if i_episode % log_interval == 0:
                avg_length = int(avg_length / log_interval)
                running_reward = float((running_reward / log_interval))
                reward_buf.append(running_reward)
                pbar.set_description(f'Episode {i_episode} \t Avg length: {avg_length} \t Avg reward: {running_reward:.6f}')
                running_reward = 0
                avg_length = 0
                plt.plot(reward_buf, label="reward mean")
                plt.legend()
                plt.savefig('reward.png')
        plt.close()
    except KeyboardInterrupt:
        print("\nTraining interrupted, continue to next cell to save to save the model.")
    finally:
        env.close()
