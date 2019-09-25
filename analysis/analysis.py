"""
"""
import subprocess
import sys
import random
import os
import shutil
import pandas as pd
import matplotlib.pyplot as plt
import click
import numpy as np
import seaborn as sns

# Import Analysis scripts
import generatePlots as gp
from generatePlots import agentBehaviors
from generatePlots import agentBehaviorsLabels

from pudb import set_trace as st


@click.command()
@click.version_option(0.3)
#@click.option('--skip-agent-plots', is_flag=True, help='If set will not generate Agent Info plots [Speeds up simulation analysis]')
#@click.option('--simulation-config-file', default='../Ressources/SimulationConfigs/SimulationConfigFile.json', help='Specify the configfy file to use')
#@click.option('--nInstances', 'nInstances', default=1, help='Specifies the number of instances to run')
@click.option('--seed', default=424242, help='Specify seed value')
@click.argument('studyfolder')
@click.argument('projectfolder')
def analysis(studyfolder, projectfolder, seed=42):
    """Analyze study results
    """
    # Make this batch run reproduceable
    random.seed(seed)

    agent_infos = pd.read_csv(os.path.join(studyfolder, gp.STUDY_AGENTINFO_FILE))

    os.makedirs(projectfolder, exist_ok=True)

    # Cluster the personality types, based on states and on behavior
    state_comp, behavior_comp = comparePersonalityTypeByStateDuration(agent_infos)
    
    plotPTComparision(state_comp, projectfolder, gp.ANALYSIS_PERSONALITYTYPE_STATE_COMPARISION_FILE+'.png')
    state_comp.to_csv(os.path.join(projectfolder, gp.ANALYSIS_PERSONALITYTYPE_STATE_COMPARISION_FILE+'.csv'), index=True)

    plotPTComparision(behavior_comp, projectfolder, gp.ANALYSIS_PERSONALITYTYPE_BEHAVIOR_COMPARISION_FILE+'.png')
    behavior_comp.to_csv(os.path.join(projectfolder, gp.ANALYSIS_PERSONALITYTYPE_BEHAVIOR_COMPARISION_FILE+'.csv'), index=True)

    print("Finished Analysis!")


###############################################################################

def comparePersonalityTypeByStateDuration(agent_infos):
    durations = agent_infos.filter(['Experiment', 'personalitytype', ] + gp.agentBehaviorsLabels, axis=1)

    # Drop Parts of break, because they are always zero
    durations.drop(['B(T)', 'B(W)'], axis=1, inplace=True)

    # Calculate means for each personality type
    state_means = durations.groupby(['Experiment', 'personalitytype']).mean()

    # Generate a separate dataframe summing all states for each behavior
    behaviors = []
    for name in ['B', 'C', 'SA', 'SG', 'Q']:
        series = state_means.filter(regex=name + "...").sum(axis=1)
        series.name = name
        behaviors.append(series)
    behavior_means = pd.concat(behaviors, axis=1)

    state_means_normalized = normalize_df(state_means)
    behavior_means_normalized = normalize_df(behavior_means)

    return state_means_normalized, behavior_means_normalized

def normalize_df(data, normalize_index=['ADHD-None', 'Normal']):
    # Use the ADHD-None, Normal type as reference.
    # Calculate the standard score (zscore) for each column based on the ADHD-None, Normal as mean
    normal_mean = data.loc['ADHD-None', 'Normal'].values
    normal_std = (data - normal_mean).abs().sum()/(data.shape[0]-1)
    normalized = (data - normal_mean)/normal_std
    return normalized


def plotPTComparision(data, outputfolder, outputfile):

    fig, ax = plt.subplots(1, 1, figsize=(14, data.shape[0]*0.5))
    #sns.heatmap(data, annot=True, fmt="2.2f", linewidths=.5, ax=ax)

    #g = sns.clustermap(data, col_cluster=False, annot=True, fmt="2.2f", linewidths=.5, figsize=(14, data.shape[0]*0.5), cbar_kws={"orientation": "horizontal"}, ax=ax, cbar_ax=cbar_ax)
    g = sns.clustermap(data, col_cluster=False, annot=True, fmt="2.2f", linewidths=.5)

    g.fig.suptitle('Personality type comparision (columns normalized towards ADHD-None, Normal)')
    g.fig.tight_layout(rect=[0.0, 0.00, 1.0-0.15, 1.0-0.55])

    g.savefig(os.path.join(outputfolder, outputfile))
    plt.close(g.fig)


###############################################################################


if __name__ == "__main__":
    analysis()
 