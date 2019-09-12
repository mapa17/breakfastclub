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


#@click.group()
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

    pt_comp = comparePersonalityTypeByStateDuration(agent_infos)
    plotPTComparision(pt_comp, projectfolder)

    print("Finished Analysis!")


###############################################################################

def comparePersonalityTypeByStateDuration(agent_infos):
    durations = agent_infos.filter(['Experiment', 'personalitytype', ] + gp.agentBehaviorsLabels, axis=1)

    # Drop Parts of break, because they are always zero
    durations.drop(['B(T)', 'B(W)'], axis=1, inplace=True)

    # Calculate means for each personality type
    means = durations.groupby(['Experiment', 'personalitytype']).mean()

    # Use the ADHD-None, Normal type as reference.
    # Calculate the standard score (zscore) for each column based on the ADHD-None, Normal as mean
    normal_mean = means.loc['ADHD-None', 'Normal'].values
    normal_std = (means - normal_mean).abs().sum()/(means.shape[0]-1)

    normalized = (means - normal_mean)/normal_std

    return normalized

def plotPTComparision(data, outputfolder):

    fig, ax = plt.subplots(1, 1, figsize=(14, data.shape[0]*0.5))
    #sns.heatmap(data, annot=True, fmt="2.2f", linewidths=.5, ax=ax)

    #g = sns.clustermap(data, col_cluster=False, annot=True, fmt="2.2f", linewidths=.5, figsize=(14, data.shape[0]*0.5), cbar_kws={"orientation": "horizontal"}, ax=ax, cbar_ax=cbar_ax)
    g = sns.clustermap(data, col_cluster=False, annot=True, fmt="2.2f", linewidths=.5)

    g.fig.suptitle('Personality type comparision (columns normalized towards ADHD-None, Normal)')
    g.fig.tight_layout(rect=[0.0, 0.00, 1.0-0.15, 1.0-0.55])

    g.savefig(os.path.join(outputfolder, gp.ANALYSIS_PERSONALITYTYPE_COMPARISION_FILE))
    plt.close(g.fig)


###############################################################################


if __name__ == "__main__":
    analysis()
 