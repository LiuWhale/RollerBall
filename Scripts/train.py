import argparse
import numpy as np
import torch
import TD3
import utils
from mlagents_envs.environment import ActionTuple
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from mlagents_envs.side_channel.side_channel import IncomingMessage
from StringLogChannel import StringLogChannel

def process_state(s):
    return np.reshape(s, [1, -1])

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
    parser.add_argument('--time-scale', type=float, default=20.0, help='Time scale for unity')
    args = parser.parse_args()

    try:
        env.close()
    except:
        pass
    # action mapping function 
    # am = ActionMappingClass()
    channel = EngineConfigurationChannel()
    string_channel = StringLogChannel()
    msg = IncomingMessage(bytearray())
    env = UnityEnvironment(file_name="/home/whale/下载/UnityBuild/USV.x86_64", \
        seed=args.seed, no_graphics=args.no_render, side_channels=[channel,string_channel], num_areas=10)
    # set time scale of the environment to speed the train up
    channel.set_configuration_parameters(time_scale = args.time_scale)
    print("USV environment created.")
    env.reset()

    # set parameters
    behavior_name = list(env.behavior_specs)[0]
    spec = env.behavior_specs[behavior_name]
    state_dim = spec.observation_specs[0].shape[0]
    action_dim = 2
    dt = 0.01
    max_action = 1
    dt = 0.01

    args = {
        'start_timesteps':1e4, 
        'eval_freq': 5e3,
        'expl_noise': 0.1, 
        'batch_size': 256,
        'discount': 0.99,
        'tau': 0.005,
        'policy_noise': 0.2,
        'noise_clip': 0.5,
        'policy_freq': 2   # was 2
    }

    kwargs = {
        "state_dim": state_dim,
        "action_dim": action_dim,
        "max_action": max_action,
        "discount": args['discount'],
        "tau": args['tau'],
    }

    # Target policy smoothing is scaled wrt the action scale
    kwargs["policy_noise"] = args['policy_noise'] * max_action
    kwargs["noise_clip"] = args['noise_clip'] * max_action
    kwargs["policy_freq"] = args['policy_freq']
    policy = TD3.TD3(**kwargs)

    try:
        device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        replay_buffer = utils.ReplayBuffer(state_dim, action_dim, max_size=int(5e6))

        # set training counters
        stepcounter = 0
        traincounter = 1
        savecounter = 1
        trainlog = []

        for episode in range(10000):
            env.reset()
            ob, r, done = get_states(env)
            ob = process_state(ob)
            done = False
            saved = False
            episode_reward = 0
            episode_timesteps = 0
            finished_count = 0
            for step in range(10000):
                time = step * dt
                stepcounter += 1
                # generate action for carA
                if stepcounter < args['start_timesteps']:
                    action = np.random.uniform(-1, 1, action_dim)
                else:
                    noise = np.random.normal(0, max_action * args['expl_noise'], size=action_dim)
                    action = (policy.select_action(ob) + noise).clip(-max_action, max_action)  # clip here
                # action mapping 
                # action_in = am.mapping(env.car.spd, env.car.steer, action[0], action[1])
                action_in = np.array([action])
                action_tuple = ActionTuple()
                action_tuple.add_continuous(action_in)
                env.set_actions(behavior_name, action_tuple)
                # perform action
                env.step()
                next_ob, r, done = get_states(env)
                string_channel.on_message_received(msg)
                info = string_channel.str.split(',')
                # if len(info) > 3:
                #     traj_x.append(float(info[3]))
                #     traj_y.append(float(info[4]))
                finished = info[0]
                wd = info[1]
                distOut = info[2]
                next_ob = process_state(next_ob)  # convert to numpy.array
                # store replay buffer
                replay_buffer.add(ob, action, next_ob, r, done)

                # update state
                ob = next_ob
                episode_reward += r

                if done: break
                
                if stepcounter > args['start_timesteps']:
                    policy.train(replay_buffer, args['batch_size'])
                    traincounter += 1
                # save TD3 model
                if traincounter % 100000 == 0 and not saved:
                    policy.save('model_'+str(savecounter))
                    savecounter += 1
                    saved = True
                    print('TD3AM model', savecounter, 'saved!')
                if finished == 'True': finished_count += 1
            # End of Episode
            print('episode: %d  reward: %.1f  step: %d counter: %d finished: %s wd: %s distOut: %s finished_count: %.1f' % (episode, episode_reward, step, traincounter, finished, wd, distOut, finished_count))
            trainlog.append([episode, episode_reward, step, traincounter, finished, wd, distOut, finished_count])
            # save reward log
            if saved:
                np.save('trainlog.npy', trainlog)
    except KeyboardInterrupt:
        print("\nTraining interrupted, continue to next cell to save to save the model.")
    finally:
        env.close()