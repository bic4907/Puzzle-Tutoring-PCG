

def get_legend_name(x):
    if x['method'] == 'mcts':
        if x['objective'] == 'score':
            return f"{x['method']}_{x['objective']}_{x['simulation']}_{x['playerDepth']}"
        elif x['objective'] == 'knowledge':
            return f"{x['method']}_{x['objective']}_{x['almostRatio']}_{x['simulation']}_{x['playerDepth']}"
        elif x['objective'] == 'kp':
            return f"{x['method']}_{x['objective']}_{x['almostRatio']}_{x['simulation']}_{x['playerDepth']}"
    else:
        return f"{x['method']}"


# Define the function to identify and remove outliers
def remove_outliers(column):
    # Calculate the z-score for the column
    z_scores = (column - column.mean()) / column.std()

    threshold = 3

    # Define the threshold for outliers (e.g., z-score > 3 or z-score < -3)
    # Return the column without the outliers
    column[~(z_scores > -threshold)] = -9999
    column[~(z_scores < threshold)] = -9999

    return column
