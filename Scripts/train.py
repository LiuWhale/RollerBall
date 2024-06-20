import numpy as np
import TD3
from utils import ReplayBuffer
from mlagents_envs.environment import ActionTuple
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from mlagents_envs.side_channel.side_channel import (
    IncomingMessage,
)
import matplotlib.pyplot as plt
import torch
import torch.nn as nn
import argparse
from tqdm import tqdm
from StringLogChannel import StringLogChannel

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
    parser.add_argument('--no-render', action='store_true', default=False, help='Render the environment')
    parser.add_argument('--seed', type=int, default=0, help='Seed for random number generator')
    parser.add_argument('--test', action='store_true', default=False, help='Test the model')
    parser.add_argument('--time-scale', type=float, default=20.0, help='Time scale for unity')
    args = parser.parse_args()
    max_episodes = 5000
    max_timesteps = 5000
    
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
    env = UnityEnvironment(base_port = 5006, file_name="/home/whale/下载/UnityBuild/USV.x86_64", \
        seed=args.seed, no_graphics=args.no_render, side_channels=[channel,string_channel])
    # set time scale of the environment to speed the train up
    channel.set_configuration_parameters(time_scale = args.time_scale)
    print("RollerBall environment created.")
    env.reset()
    behavior_name = list(env.behavior_specs)[0]
    spec = env.behavior_specs[behavior_name]

    state_dim = spec.observation_specs[0].shape[0]
    action_dim = spec.action_spec.continuous_size
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
    kwargs["policy_noise"] = args['policy_noise'] * max_action
    kwargs["noise_clip"] = args['noise_clip'] * max_action
    kwargs["policy_freq"] = args['policy_freq']
    
    try:    
        replay_buffer = ReplayBuffer(state_dim, action_dim)
        policy = TD3.TD3(**kwargs)
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
            finished = False
            wd = False
            distOut = False
            for step in range(max_timesteps):
                stepcounter += 1
                # Running policy_old:
                if stepcounter < 5000:
                    action = np.random.uniform(-1, 1, action_dim)
                else:
                    noise = np.random.normal(0, max_action * args['expl_noise'], \
                        size=action_dim).clip(-max_action, max_action)
                    action = policy.select_action(state) + noise
                action = np.array([action])
                action_tuple = ActionTuple()
                action_tuple.add_continuous(action)
                env.set_actions(behavior_name, action_tuple)
                # Perform a step in the simulation
                env.step()
                if finished == 'True': finished_count += 1
                next_state, reward, done = get_states(env)
                finished, wd, distOut = string_channel.on_message_received(msg)
                replay_buffer.add(state, action, next_state, reward, done)

                state = next_state
                episode_reward += reward
                
                if stepcounter > args['start_timesteps']:
                    policy.train(replay_buffer, args['batch_size'])
                    traincounter += 1
                # save TD3model
                if traincounter % 2000 == 0 and not saved:
                    policy.save('model_'+str(savecounter))
                    savecounter += 1
                    saved = True
                    print('TD3model', savecounter, 'saved!---------------------------') 
                if done:
                    break
            # End of Episode
            rewardlog.append(episode_reward)
            print('episode:', i_episode, 'reward:', episode_reward, 'step:', step, 'finished:', finished, 'wd:', wd, 'distOut:', distOut, 'finished_count:', finished_count, 'traincounter:', traincounter)
            if savecounter > 45:
                break
    except KeyboardInterrupt:
        print("\nTraining interrupted, continue to next cell to save to save the model.")
    finally:
        env.close()
