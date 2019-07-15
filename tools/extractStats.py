import sys, getopt
import numpy as np
import pandas as pd
from pudb import set_trace as st

def main(argv):
    try:
        logfile = argv[1]
    except:
        print('Call with:\n%s [Logfile]' % argv[0])
        sys.exit(1)
    agents, classroom = load_data(logfile)

    classroom_columns=['nAgents', 'NoiseLevel', 'Motivation_mean', 'Motivation_std', 'Happiness_mean', 'Happiness_std', 'Attention_mean', 'Attention_std']
    agents_columns=['Motivation', 'Happiness', 'Attention', 'Action', 'Desire']

    classroom_df = extract_stats(classroom, classroom_columns)    
    agents_df = extract_stats(agents, agents_columns)    

    print('Writing output to ...\n%s\n%s'%('Classroom_Stats.csv', 'Agents_Stats.csv'))
    classroom_df.to_csv('Classroom_Stats.csv', index=False)
    agents_df.to_csv('Agents_Stats.csv', index=False)


def load_data(filepath):
    data = pd.read_csv(filepath)
    # Drop last column (introduced because of trailing ,)
    data = data.drop(data.columns[-1], axis=1)

    # Filter only Stats
    isStats = data['Type'] == 'S'
    data = data[isStats]

    # Split between Agents and Classroom
    isclassroom = data['Tag'] == 'Classroom'
    classroom = data[isclassroom]
    agents = data[isclassroom == False]

    classroom.reset_index(inplace=True)
    agents.reset_index(inplace=True)
    return agents, classroom


def extract_stats(data, columns, seperator='|'):
    # Parse messages
    messages = [x.split(seperator) for x in data['Message'].values]
    
    # Create a stats dataframe
    stats = pd.DataFrame(data=messages, columns=columns)

    # Extract metadata
    meta = data.drop('Message', axis=1)

    return pd.concat([meta, stats], axis=1).drop('index', axis=1)

if __name__ == "__main__":
    main(sys.argv)
