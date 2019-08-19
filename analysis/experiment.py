"""
Batch Processing script that will run multiple simulations, extract their stat
files and run the analysis.

Can be called similar to:
> python batchrun.py ../Unity/build/GameConfig.json 1331 2 outputProjectFolder
"""
import subprocess
import sys
import random
import os
import shutil
import pandas as pd
import matplotlib.pyplot as plt
import click

# Import Analysis scripts
from extractStats import extractStats
from generatePlots import generatePlots
from generatePlots import plotHappinessAttentionGraph

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
MACOS_CMD_SIMULATION = ["/usr/bin/open", "-W", "-n", "../Unity/build/CurrentBuild.app", "--args" ,"-batchmode"]


def run_simulation(systemos, simulation_config_file, game_config_file, seed, outputfile, headless=False):
    if systemos == MACOS:
        if headless:
            sys_cmd = MACOS_CMD_SIMULATION
        else:
            sys_cmd = MACOS_CMD_SIMULATION[0:-1]
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


def run_analysis(outputfile, skip_agent_plots):
    # Extract Classroom and Agent csv
    classroom_stats_file, agents_stats_file = extractStats(outputfile)

    # Run a Simulation analysis and generate summary plots, and agent based plots
    generatePlots(classroom_stats_file, agents_stats_file, os.path.dirname(outputfile), skip_agent_plots=skip_agent_plots)


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

        run_analysis(outputfile, skip_agent_plots)

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
