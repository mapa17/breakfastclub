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

# Import Analysis scripts
from extractStats import extractStats
from generatePlots import generatePlots

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
#/usr/bin/open -W -n ../Unity/build/CurrentBuild.app --args -batchmode GameConfig.json 2332
MACOS_CMD_SIMULATION = ["/usr/bin/open", "-W", "-n", "../Unity/build/CurrentBuild.app", "--args" ,"-batchmode"]


def run_simulation(systemos, config_file, seed, outputfile):
    if systemos == MACOS:
        sys_cmd = MACOS_CMD_SIMULATION
    else:
        raise NotImplementedError

    try:
        cmd = sys_cmd + [config_file, str(seed), outputfile]
        print('Calling subprocess.run with ...\n[%s]' % cmd)
        subprocess.run(cmd, check=True, shell=False)
    except subprocess.CalledProcessError as e:
        print(e.args, e.returncode, e.stderr)
        return 1
    return 0


def run_analysis(systemos, config_file, seed, outputfile):
    # Extract Classroom and Agent csv
    classroom_stats_file, agents_stats_file = extractStats(outputfile)

    # Run a Simulation analysis and generate summary plots, and agent based plots
    generatePlots(classroom_stats_file, agents_stats_file, os.path.dirname(outputfile))


def plot_experiment_summary(summary_file, projectfolder):
    fig, ax = plt.subplots(1, 1, figsize=(10, 10))
    
    classrooms = summary_file[summary_file['Tag'] == 'Classroom']

    for _, data in classrooms.iterrows():
        ax.scatter(data['Happiness'], data['Attention'], label=data['Instance'])

    ax.set_xlim(-1.0, 1.0)
    ax.set_ylim(0.0, 1.0)
    ax.set_xlabel('Happiness')
    ax.set_ylabel('Attention')
    fig.suptitle('Happiness vs Attention\nExperiment: %s' % os.path.basename(projectfolder), fontsize=16)
    ax.legend()

    summary_outputfile = os.path.join(projectfolder, 'Experiment_summary.png')
    print('Writing Experiment summary to %s' % summary_outputfile)
    fig.savefig(summary_outputfile)
    plt.close(fig)


def main(argv):
    # Very simple argument parser
    try:
        configfile = argv[1]
        configfile = os.path.abspath(configfile)
        seed = int(argv[2])
        nInstances = int(argv[3])
        projectfolder = argv[4]
        projectfolder = os.path.abspath(projectfolder)
    except:
        print('%s [CONFIG_FILE] [SEED] [N_INSTANCES] [PROJECT_FOLDER]' % argv[0])
        sys.exit(1)

    current_os = detectOS()

    # Make this batch run reproduceable
    random.seed(seed)

    # Store all results in subfolders of the project folder
    shutil.rmtree(projectfolder, ignore_errors=True)
    os.makedirs(projectfolder, exist_ok=True)

    for i in range(nInstances):
        new_seed = random.randint(0, 10000)

        # Prepare output folder
        outputfile = os.path.join(projectfolder, 'Instance-%03d-%d'%(i, new_seed), 'Logfile.csv')
        os.makedirs(os.path.dirname(outputfile), exist_ok=True)

        run_simulation(current_os, configfile, new_seed, outputfile)
        #st()
        run_analysis(current_os, configfile, new_seed, outputfile)

    shutil.copy(configfile, projectfolder)
    
    summary_file = pd.read_csv(os.path.join(projectfolder, 'Experiment_summary.csv'))

    plot_experiment_summary(summary_file, projectfolder)

    print(f'Finished running Experiment with {nInstances} Instances ...')


if __name__ == "__main__":
    main(sys.argv)
