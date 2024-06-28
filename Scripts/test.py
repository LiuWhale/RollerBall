import numpy as np
import TD3
from utils import ReplayBuffer
from mlagents_envs.environment import ActionTuple
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from mlagents_envs.side_channel.side_channel import IncomingMessage
from StringLogChannel import StringLogChannel
import matplotlib.pyplot as plt
import torch
import argparse

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
    parser.add_argument('--max-timesteps', type=int, default=500, help='Number of steps to run the simulation')
    parser.add_argument('--no-render', action='store_true', default=False, help='Render the environment')
    parser.add_argument('--seed', type=int, default=0, help='Seed for random number generator')
    parser.add_argument('--time-scale', type=float, default=1.0, help='Time scale for unity')
    args = parser.parse_args()
    
    max_episodes = args.episodes
    max_timesteps = args.max_timesteps
    
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    # Create a Unity Environment
    try:
        env.close()
    except:
        pass
    # -----------------
    # Create the GridWorld Environment from the registry
    channel = EngineConfigurationChannel()
    string_channel = StringLogChannel()
    msg = IncomingMessage(bytearray())
    env = UnityEnvironment(file_name="/home/whale/下载/UnityBuild/USV.x86_64", \
        seed=args.seed, no_graphics=args.no_render, side_channels=[channel,string_channel], base_port=5415)
    # set time scale of the environment to speed the train up
    channel.set_configuration_parameters(time_scale = args.time_scale)
    print("USV Race environment created.")
    env.reset()
    behavior_name = list(env.behavior_specs)[0]
    spec = env.behavior_specs[behavior_name]
    
    state_dim = spec.observation_specs[0].shape[0]
    # action_dim = spec.action_spec.continuous_size
    action_dim = 1
    max_action = 1
    
    args = {
        'start_timesteps':1e4,
        'eval_freq': 5e3,
        'expl_noise': 0.1,
        'batch_size': 256,
        'discount': 0.90,
        'tau': 0.005,
        'policy_noise': 0.2,
        'noise_clip': 0.5,
        'policy_freq': 2
    }
    kwargs = {
        "state_dim": state_dim,
        "action_dim": action_dim,
        "max_action": max_action,
        "discount": args['discount'],
        "tau": args['tau'],
    }
    
    try:    
        replay_buffer = ReplayBuffer(state_dim, action_dim)
        policy = TD3.TD3(**kwargs)
        policy.load('model')
        stepcounter = 0
        traincounter = 1
        savecounter = 1
        rewardlog = []
        finished_count = 0
        
        for i_episode in range(max_episodes):
            done = False
            saved = False
            episode_reward = 0
            episode_timesteps = 0
            env.reset()
            state, r, done = get_states(env)
            finished = "False"
            wd = "False"
            distOut = "False"
            traj_x = []
            traj_y = []
            plt.clf()
            for step in range(max_timesteps):
                stepcounter += 1
                action = policy.select_action(state)
                action = action[0]
                action = np.array([[1, action]])
                action_tuple = ActionTuple()
                action_tuple.add_continuous(action)
                env.set_actions(behavior_name, action_tuple)
                # Perform a step in the simulation
                env.step()
                next_state, reward, done = get_states(env)
                string_channel.on_message_received(msg)
                info = string_channel.str.split(',')
                if len(info) > 3:
                    traj_x.append(float(info[3]))
                    traj_y.append(float(info[4]))
                if done:
                    finished = info[0]
                    wd = info[1]
                    distOut = info[2]
                    break
                state = next_state
                episode_reward += reward
                
            if finished == 'True': finished_count += 1
            # End of Episode
            rewardlog.append(episode_reward)
            plt.plot(rewardlog, label="reward")
            plt.legend("reward")
            plt.savefig("reward.png")
            plt.close()
            plt.plot(traj_x, traj_y, label="trajectory")
            plt.legend("trajectory")
            plt.savefig("trajectory.png")
            plt.close()
            print('episode:', i_episode, 'reward:', episode_reward, 'step:', step, 'finished:', finished, 'wd:', wd, 'distOut:', distOut, 'finished_count:', finished_count, 'stepcounter:',stepcounter, 'traincounter:', traincounter)
    except KeyboardInterrupt:
        print("\nTraining interrupted, continue to next cell to save to save the model.")
    finally:
        env.close()
