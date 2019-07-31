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
from generatePlots import plotHappinessAttentionGraph
import experiment
from experiment import detectOS, run_analysis, run_simulation, experiment

from pudb import set_trace as st

def interactive_experiment(simulation_config_file, configfile, seed, projectfolder):
    return experiment(simulation_config_file, configfile, seed, 1, projectfolder, interactive=True)


def main(argv):
    # Very simple argument parser
    try:
        simulation_config_file = argv[1]
        simulation_config_file = os.path.abspath(simulation_config_file)
        classroom_config_file = argv[2]
        classroom_config_file = os.path.abspath(classroom_config_file)
        seed = int(argv[3])
        projectfolder = argv[4]
        projectfolder = os.path.abspath(projectfolder)
    except:
        print('%s [SIMULATION_CONFIG_FILE] [CLASSROOM_CONFIG_FILE] [SEED] [PROJECT_FOLDER]' % argv[0])
        sys.exit(1)

    interactive_experiment(simulation_config_file, classroom_config_file, seed, projectfolder)


if __name__ == "__main__":
    main(sys.argv)
