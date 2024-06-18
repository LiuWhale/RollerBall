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
    parser.add_argument('--episodes', type=int, default=5000, help='Number of episodes to train the model')
    parser.add_argument('--max-timesteps', type=int, default=2000, help='Number of steps to run the simulation')
    parser.add_argument('--no-render', action='store_true', default=False, help='Render the environment')
    parser.add_argument('--seed', type=int, default=0, help='Seed for random number generator')
    parser.add_argument('--test', action='store_true', default=False, help='Test the model')
    parser.add_argument('--time-scale', type=float, default=20.0, help='Time scale for unity')
    args = parser.parse_args()
    
    max_episodes = args.episodes
    max_timesteps = args.max_timesteps
    
    solved_reward = 0.94  # stop training if avg_reward > solved_reward
    log_interval = 50  # print avg reward in the interval
    action_std = 0.5  # constant std for action distribution (Multivariate Normal)
    update_timestep = 50  # update policy every n timesteps
    env_name = "RollerBall"
    
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    # Create a Unity Environment
    try:
        env.close()
    except:
        pass
    # -----------------
    # Create the GridWorld Environment from the registry
    channel = EngineConfigurationChannel()
    env = UnityEnvironment(base_port = 5006, file_name="/home/whale/下载/UnityBuild/RollerBall.x86_64", \
        seed=args.seed, no_graphics=args.no_render, side_channels=[channel])
    # set time scale of the environment to speed the train up
    channel.set_configuration_parameters(time_scale = args.time_scale)
    print("RollerBall environment created.")

    state_dim = 8
    action_dim = 2
    
    env.reset()
    behavior_name = list(env.behavior_specs)[0]
    spec = env.behavior_specs[behavior_name]
    
    try:    
        path = 'PPO_continuous_solved_RollerBall_750_47.350000059232116.pth'
        memory = Memory()
        ac = ActorCritic(state_dim, action_dim, action_std, device).to(device)
        ac.load(path)
        # logging variables
        running_reward = 0
        avg_length = 0
        time_step = 0
        reward_buf = []
        pbar = tqdm(range(max_episodes))
        for i_episode in pbar:
            state, r, done = get_states(env)
            for t in range(max_timesteps):
                state = torch.FloatTensor(state.reshape(1, -1)).to(device)
                time_step += 1
                # Running policy_old:
                action = ac.act(state, memory).cpu().data.numpy().flatten()
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
                    memory.clear_memory()
                    time_step = 0
                running_reward += reward
                if done:
                    env.reset()
                    break

            avg_length += t

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
