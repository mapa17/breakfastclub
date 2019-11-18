"""
Batch Processing script that will run multiple simulations, extract their stat
files and run the analysis.

Can be called similar to:
> python ../analysis/experiment.py classconfigs/ADHD-None.json --simulation-config-file SimulationConfigFile.json --seed 11111 --headless PCA-test --skip-agent-plots
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

# Import Analysis scripts
import generatePlots as gp
from generatePlots import generatePlots
from generatePlots import plotHappinessAttentionGraph
from generatePlots import agentBehaviors
from generatePlots import agentBehaviorsLabels

from pudb import set_trace as st

LINUX = 0
MACOS = 1
WINDOWS = 2
UNKNOWN = 3

def detectOS():
    OS = {'linux': LINUX, 'win32': WINDOWS, 'darwin': MACOS}
    if sys.platform in OS:
        return OS[sys.platform]
    else:
        return UNKNOWN


# -nographics causes problems, not writing player.log file ...
#/usr/bin/open -W -n ../Unity/build/CurrentBuild.app --args -batchmode GameConfig.json 2332 outputfolder/Logfile.csv
MACOS_CMD_SIMULATION = ["/usr/bin/open", "-W", "-g", "-n", "../Unity/build/CurrentBuild.app", "--args" ,"-batchmode"]
WINDOWS_CMD_SIMULATION = ["../Unity/build/Breakfastclub.exe", "--args" ,"-batchmode"]


def run_simulation(systemos, simulation_config_file, game_config_file, seed, outputfile, headless=False):
    if systemos == MACOS:
        if headless:
            sys_cmd = MACOS_CMD_SIMULATION
        else:
            sys_cmd = MACOS_CMD_SIMULATION[0:-1]
    elif systemos == WINDOWS:
        if headless:
            sys_cmd = WINDOWS_CMD_SIMULATION
        else:
            sys_cmd = WINDOWS_CMD_SIMULATION[0:-1]
    else:
        raise NotImplementedError
    
    try:
        cmd = sys_cmd + [simulation_config_file, game_config_file, str(seed), outputfile]
        print('Calling subprocess.run with ...\n[%s]' % cmd)
        subprocess.run(cmd, check=True, shell=False)
    except subprocess.CalledProcessError as e:
        print(e.args, e.returncode, e.stderr)
        return 1
    return 0


#@click.group()
@click.command()
@click.version_option(0.3)
@click.option('--headless', is_flag=True, help='If set will run without visualization')
@click.option('--skip-agent-plots', is_flag=True, help='If set will not generate Agent Info plots [Speeds up simulation analysis]')
@click.option('--simulation-config-file', default='../Ressources/SimulationConfigs/SimulationConfigFile.json', help='Specify the configfy file to use')
@click.option('--nInstances', 'nInstances', default=1, help='Specifies the number of instances to run')
@click.option('--seed', default=424242, help='Specify seed value')
#@click.argument('classroom-config', 'classroom_config_file', help='JSON file containing classroom config')
@click.argument('classroom-config-file')
@click.argument('projectfolder')
def experiment(simulation_config_file, classroom_config_file, seed, nInstances, projectfolder, headless=False, skip_agent_plots=False):
    """Run breakfast simulation
    """
    current_os = detectOS()

    # Make this batch run reproduceable
    random.seed(seed)

    # Store all results in subfolders of the project folder
    shutil.rmtree(projectfolder, ignore_errors=True)
    os.makedirs(projectfolder, exist_ok=True)

    for i in range(nInstances):
        new_seed = random.randint(0, 10000)

        # Prepare output folder
        outputfile = os.path.abspath(os.path.join(projectfolder, 'Instance-%03d-%d'%(i, new_seed), 'Logfile.csv'))
        os.makedirs(os.path.dirname(outputfile), exist_ok=True)

        # Unity needs absolute paths
        simulation_config_file = os.path.abspath(simulation_config_file)
        classroom_config_file = os.path.abspath(classroom_config_file)

        run_simulation(current_os, simulation_config_file, classroom_config_file, new_seed, outputfile, headless=headless)

        process_results(outputfile, skip_agent_plots)

    # Copy the config file into the project folder
    shutil.copy(classroom_config_file, projectfolder)
    shutil.copy(simulation_config_file, projectfolder) 
    
    summary_file = pd.read_csv(os.path.join(projectfolder, 'Experiment_summary.csv'))
    classrooms = summary_file[summary_file['Tag'] == 'Classroom']

    f = plotHappinessAttentionGraph(classrooms['Attention'], classrooms['Happiness'], suptitle=os.path.basename(projectfolder), labels=classrooms['Instance'], normalize=True)

    # Generate another HA plot that is based on the results of the individual agents
    # The ellipses drawn should have a size corresponding to the std of the particular classroom
    # The overall std bars should be the average std of all classes
    instance_means = summary_file.groupby('Instance').mean()
    instance_std = summary_file.groupby('Instance').std()
    happiness_std = instance_std['Happiness'].mean()
    attention_std = instance_std['Attention'].mean()

    fn = plotHappinessAttentionGraph(instance_means['Attention'], instance_means['Happiness'], attention_std=attention_std, happiness_std=happiness_std, height=instance_std['Attention'], width=instance_std['Happiness'], suptitle=os.path.basename(projectfolder), labels=classrooms['Instance'], normalize=True)
    of = os.path.join(projectfolder, 'Experiment_summary.png')
    ofn = os.path.join(projectfolder, 'Experiment_summary-AgentStd.png')
    f.savefig(of)
    fn.savefig(ofn)
    plt.close(f)
    plt.close(fn)

    print(f'Finished running Experiment with {nInstances} Instances ...')

    return summary_file


def process_results(outputfile, skip_agent_plots):
    # Extract Classroom and Agent csv
    classroom_stats_file, agents_stats_file = extractStats(outputfile)

    classroom_stats = pd.read_csv(classroom_stats_file)
    agents_stats = pd.read_csv(agents_stats_file)
    agent_infos, agents_stats = calculate_agent_info(agents_stats)

    # Add the number of studying students to the classroom stats
    behavior_sums = agents_stats[['Turn', 'IsStudying', 'IsQuarrel']].groupby('Turn').sum().astype(int)
    classroom_stats = classroom_stats.join(behavior_sums, on='Turn').rename(columns={'IsStudying': 'Studying_sum', 'IsQuarrel': 'Quarrel_sum'})

    # Run a Simulation analysis and generate summary plots, and agent based plots
    generatePlots(classroom_stats, agents_stats, agent_infos, os.path.dirname(outputfile), skip_agent_plots=skip_agent_plots)

    write_experiment_summary(classroom_stats, agents_stats, agent_infos, os.path.dirname(outputfile))


 ###############################################################################

def identifyAction(string, actions):
    for idx, a in enumerate(actions):
        if(string.find(a) > -1):
            return idx
    return -1


def calculate_duration_info(sequence_durations):
    """Use Transition matrix to calculate serverel infos about the agent
    """
    # Calculate ratio of each action compared to the total
    relative_duration = [sum(l) for l in sequence_durations]
    relative_duration = np.array(relative_duration)
    relative_duration = relative_duration / sum(relative_duration)
    relative_duration *= 100.0

    # Calculate mean action durations
    mean_durations = [np.mean(d) for d in sequence_durations]
    # Fill nan with 0.0 
    mean_durations = np.array(mean_durations)
    mean_durations[np.isnan(mean_durations)] = 0

    # Calculate STD
    mean_durations_std = [np.std(l) for l in sequence_durations]

    return relative_duration, mean_durations, mean_durations_std


def getAgentInfos(table):
    """Calculate transition matrix and action sequence durations for each agent
    """
    tm, sequence_durations = getTransitionInfo(table['Action_idx'].values, len(agentBehaviors))
    relative_duration, mean_durations, mean_durations_std = calculate_duration_info(sequence_durations)
        
    index=['TM', 'relative_durations', 'mean_durations', 'mean_durations_std']
    return pd.Series([tm, relative_duration, mean_durations, mean_durations_std], index)


def getTransitionInfo(sequence, nActions):
    """Calculate the Transition Matrix and Durations of Action sequences
    """
    tm = np.zeros(shape=(nActions, nActions))

    # Calculate the transitions
    getTransitionMatrix(sequence, tm, rowNormalization=True)

    # Calculate durations of each action sequence
    durations = getDurations(sequence, nActions)

    return tm, durations


def getTransitionMatrix(sequence, tm, rowNormalization=True):
    for prev, next in zip(sequence, sequence[1:]):
        tm[prev, next] = tm[prev, next] + 1 
    if rowNormalization:
        ntm = tm / tm.sum(axis=1)[:, None]
        ntm[np.isnan(ntm)] = 0
        tm[:, :] = ntm[:, :]


def getDurations(sequence, nActions):
    durations = [[] for x in range(nActions)]

    l = 1
    for prev, next in zip(sequence, sequence[1:]):
        if(prev == next):
            l+=1
        else:
            durations[prev].append(l)
            l = 1
    # Store last subsequence length
    durations[next].append(l)

    return durations


def calculate_agent_info(agents_stats):
    print('Calculating Agent infos ...')
    # First line in agent stats are personality traits
    #meta_df = agents_stats[agents_stats['Turn'] == -2][['Tag', 'Motivation', 'Happiness', 'Attention', 'Action', 'Desire']].reset_index(drop=True)
    meta_df = agents_stats[agents_stats['Turn'] == -2][['Tag', 'Motivation', 'Happiness', 'Attention']].reset_index(drop=True)
    meta_df.rename(columns={'Motivation': 'studentname', 'Happiness': 'personalitytype', 'Attention': 'conformity', 'Action': 'x', 'Desire': 'x'}, inplace=True)
    meta_df.set_index('Tag', inplace=True)

    personalities_df = agents_stats[agents_stats['Turn'] == -1][['Tag', 'Motivation', 'Happiness', 'Attention', 'Action', 'Desire']].reset_index(drop=True)
    personalities_df.rename(columns={'Motivation': 'Openness', 'Happiness': 'Conscientiousness', 'Attention': 'Extraversion', 'Action': 'Agreeableness', 'Desire': 'Neuroticism'}, inplace=True)
    personalities_df.set_index('Tag', inplace=True)
    personalities = personalities_df.apply(lambda x: ', '.join(['%s: %s'%(k, v) for k, v in x.to_dict().items()]), axis=1)
    pdf = pd.DataFrame(index=personalities.index, data=personalities.values, columns=['personality'])

    pdf = pd.concat([meta_df, pdf, personalities_df], axis=1)

    # Remove all lines that are no stats, and set their datatype
    agents_stats = agents_stats[agents_stats['Turn'] >= 0].astype({'Action': 'str', 'Desire': 'str', 'Tag':'str', 'Motivation': 'float', 'Attention': 'float', 'Happiness': 'float'})

    # Get Action indices
    agents_stats['Action_idx'] = agents_stats['Action'].apply(lambda x: identifyAction(x, agentBehaviors))
    agents_stats['Desire_idx'] = agents_stats['Desire'].apply(lambda x: identifyAction(x, agentBehaviors))
    agent_infos = agents_stats.groupby('Tag').apply(getAgentInfos)

    # Add one field indicating if agent is studying
    agents_stats['IsStudying'] = agents_stats['Action_idx'].isin(range(8, 8+8))
    agents_stats['IsQuarrel'] = agents_stats['Action_idx'].isin(range(16, 16+4)) 

    agent_infos = pd.concat([agent_infos, pdf], axis=1)

    return agent_infos, agents_stats


def extractStats(logfile):
    agents, classroom = load_data(logfile)

    classroom_columns=['nAgents', 'NoiseLevel', 'Motivation_mean', 'Motivation_std', 'Happiness_mean', 'Happiness_std', 'Attention_mean', 'Attention_std']
    agents_columns=['Motivation', 'Happiness', 'Attention', 'Action', 'Desire']

    classroom_df = extract_stats(classroom, classroom_columns)    
    agents_df = extract_stats(agents, agents_columns)    

    # Prepare output files
    output_folder = os.path.dirname(os.path.abspath(logfile))
    classroom_output_file = os.path.join(output_folder, gp.EXPERIMENT_CLASSROOM_STATS)
    agents_output_file = os.path.join(output_folder, gp.EXPERIMENT_AGENT_STATS)
    print('Writing output to ...\n%s\n%s'%(classroom_output_file, agents_output_file))
    
    classroom_df.to_csv(classroom_output_file, index=False)
    agents_df.to_csv(agents_output_file, index=False)

    return classroom_output_file, agents_output_file


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


def write_experiment_summary(classroom_stats, agents_stats, agent_infos, output_folder):
    summary_file = os.path.join(os.path.dirname(output_folder), gp.EXPERIMENT_SUMMARY_FILE)
    print('Writing Experiment summary file to %s ...' % summary_file)
    
    agent_summary_file = os.path.join(os.path.dirname(output_folder), gp.EXPERIMENT_AGENT_SUMMARY_FILE)
    print('Writing Agent Experiment summary file to %s ...' % agent_summary_file)

    agent_stats_file = os.path.join(os.path.dirname(output_folder), gp.EXPERIMENT_AGENT_STAT_FILE)
    print('Writing Agent Stats summary file to %s ...' % agent_stats_file)

    classroom_means = classroom_stats[['Tag', 'Motivation_mean', 'Happiness_mean', 'Attention_mean']].rename({'Motivation_mean':'Motivation', 'Happiness_mean':'Happiness', 'Attention_mean':'Attention'}, axis=1)
    classroom_means = classroom_means.groupby('Tag').mean()

    # Meta infos
    Instance = os.path.basename(output_folder)
    Experiment = os.path.basename(os.path.dirname(output_folder)) 

    # Attention is calculated only during studying
    agent_attention = agents_stats[agents_stats['IsStudying']][['Tag', 'Attention']].groupby('Tag').mean()
    agent_means = agents_stats[agents_stats['Turn'] > 0][['Tag', 'Happiness', 'Motivation' ]].groupby('Tag').mean()
    agent_means = pd.concat([agent_attention, agent_means], axis=1)

    #agent_means = agents_stats[agents_stats['Turn'] > 0][['Tag', 'Motivation', 'Happiness', 'Attention']]
    means = pd.concat([classroom_means, agent_means], axis=0, sort=True)
    means['Instance'] = Instance
    means['Experiment'] = Experiment
    means.reset_index(inplace=True)
    
    # Prepare agent_infos
    agent_summary = agent_infos[['studentname', 'personalitytype', 'conformity', 'Openness', 'Conscientiousness', 'Extraversion', 'Agreeableness', 'Neuroticism']]
    agent_summary['Instance'] = Instance
    agent_summary['Experiment'] = Experiment

    # Prepare agent_stats
    agents_stats['Instance'] = Instance
    agents_stats['Experiment'] = Experiment

    # Get relative durations
    rD = pd.DataFrame(agent_infos.relative_durations.tolist(), columns=agentBehaviorsLabels)
    # Only keep data for states T, W, E
    rD = rD.filter(axis=1, regex=(r".{1,2}\([TWE]\)"))
    # Index have to match in order to concat
    rD.index = agent_summary.index
    agent_summary = pd.concat([agent_summary, rD], axis=1)

    if os.path.isfile(summary_file):
        header=False
    else:
        header=True

    with open(summary_file, 'a') as f:
        means.to_csv(f, header=header, index=False)

    with open(agent_summary_file, 'a') as f:
        agent_summary.to_csv(f, header=header, index=True)

    with open(agent_stats_file, 'a') as f:
        agents_stats.to_csv(f, header=header, index=False)
 

 ###############################################################################

def main(argv):
    # Very simple argument parser
    try:
        simulation_config_file = argv[1]
        simulation_config_file = os.path.abspath(simulation_config_file)
        classroom_config_file = argv[2]
        classroom_config_file = os.path.abspath(classroom_config_file)
        seed = int(argv[3])
        nInstances = int(argv[4])
        projectfolder = argv[5]
        projectfolder = os.path.abspath(projectfolder)
    except:
        print('%s [SIMULATION_CONFIG_FILE] [CLASSROOM_CONFIG_FILE] [SEED] [N_INSTANCES] [PROJECT_FOLDER]' % argv[0])
        sys.exit(1)

    experiment(simulation_config_file, classroom_config_file, seed, nInstances, projectfolder)


if __name__ == "__main__":
    # main(sys.argv)
    experiment()
