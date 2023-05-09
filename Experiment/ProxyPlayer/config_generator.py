import yaml
import os

ENV_ARGS = list()

MCTS_SIMULATION_TIMES = [100, 200, 300, 400, 500]
TARGET_PLAYER = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
METHOD = ['mcts', 'random']
OBJECTIVE = ['score', 'knowledge']
TARGET_EPISODE_COUNT = 1000

os.makedirs('generated', exist_ok=True)


with open('base_config.yaml') as f:
    base_config = yaml.load(f, Loader=yaml.FullLoader)


for i_method in METHOD:
    for i_player in TARGET_PLAYER:
        run_id = f'method_{i_method}_player_{i_player}'

        RUN_ID = list()
        RUN_ID.append('method'), RUN_ID.append(i_method)
        RUN_ID.append('player'), RUN_ID.append(i_player)

        if i_method == 'mcts':
            for i_objective in OBJECTIVE:
                for i_simulation in MCTS_SIMULATION_TIMES:
                    _RUN_ID = RUN_ID.copy()
                    _RUN_ID.append('objective'), _RUN_ID.append(i_objective)
                    _RUN_ID.append('simulation'), _RUN_ID.append(i_simulation)

                    _run_id = '_'.join(map(str, _RUN_ID))

                    ENV_ARGS = list()
                    ENV_ARGS.append('--runId'), ENV_ARGS.append(_run_id)
                    ENV_ARGS.append('--method'), ENV_ARGS.append(i_method)
                    ENV_ARGS.append('--targetPlayer'), ENV_ARGS.append(i_player)
                    ENV_ARGS.append('--logPath'), ENV_ARGS.append(f'/workspace/results/{_run_id}/')
                    ENV_ARGS.append('--mctsSimulation'), ENV_ARGS.append(i_simulation)
                    ENV_ARGS.append('--targetEpisodeCount'), ENV_ARGS.append(TARGET_EPISODE_COUNT)

                    base_config['env_settings']['env_args'] = ENV_ARGS
                    print(_RUN_ID)

                    with open(os.path.join('generated', f'{_run_id}.yaml'), 'w') as f:
                        yaml.dump(base_config, f)

        else:
            ENV_ARGS = list()
            _run_id = run_id

            ENV_ARGS.append('--runId'), ENV_ARGS.append(_run_id)
            ENV_ARGS.append('--method'), ENV_ARGS.append(i_method)
            ENV_ARGS.append('--targetPlayer'), ENV_ARGS.append(i_player)
            ENV_ARGS.append('--logPath'), ENV_ARGS.append(f'/workspace/results/{_run_id}/')
            ENV_ARGS.append('--targetEpisodeCount'), ENV_ARGS.append(TARGET_EPISODE_COUNT)
            base_config['env_settings']['env_args'] = ENV_ARGS
            with open(os.path.join('generated', f'{_run_id}.yaml'), 'w') as f:
                yaml.dump(base_config, f)
