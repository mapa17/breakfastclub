from generatePlots import plotHappinessAttentionGraph
import pandas as pd
import os
import sys

from pudb import set_trace as st

def study(summary_files):
    experiments = pd.DataFrame()
    for sf in summary_files:
        exp = pd.read_csv(sf)
        experiments = pd.concat([experiments, exp])

    # Extract mean and std over all experiments 
    aggs = experiments[experiments['Tag'] == 'Classroom'].groupby('Experiment').agg(['mean', 'std'])
    happiness = aggs['Happiness', 'mean']
    width = aggs['Happiness', 'std']
    attention = aggs['Attention', 'mean']
    height = aggs['Attention', 'std']

    print('Writing Results to %s ...' % 'Study_Comparision.png')
    plotHappinessAttentionGraph(attention, happiness, 'Study_Comparision.png', width=width, height=height, labels=aggs.index, include_means=False, suptitle='Experiment comparison')
    plotHappinessAttentionGraph(attention, happiness, 'Study_Comparision-NoneNormalized.png', width=width, height=height, labels=aggs.index, include_means=False, suptitle='Experiment comparison' ,normalize=False)


def main(argv):
    # Very simple argument parser
    try:
        summary_files = []
        for folder in argv[1:]:
            summary_file = os.path.join(os.path.abspath(folder), 'Experiment_summary.csv')
            if os.path.isfile(summary_file):
                summary_files.append(summary_file)
    except:
        print('%s [EXPERIMENT_FOLDER1] [EXPERIMENT_FOLDER2] ... [EXPERIMENT_FOLDERN]' % argv[0])
        sys.exit(1)

    study(summary_files)


if __name__ == "__main__":
    main(sys.argv)