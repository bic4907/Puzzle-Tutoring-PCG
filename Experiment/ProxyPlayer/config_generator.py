import yaml

ENV_ARGS = list()



with open('base_config.yaml') as f:
    base_config = yaml.load(f, Loader=yaml.FullLoader)

ENV_ARGS = [
    ''
]


with open('base_config2.yaml', 'w') as f:
    yaml.dump(base_config, f)
