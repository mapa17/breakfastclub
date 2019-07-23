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

def interactive_experiment(configfile, seed, projectfolder):
    return experiment(configfile, seed, 1, projectfolder, interactive=True)


def main(argv):
    # Very simple argument parser
    try:
        configfile = argv[1]
        configfile = os.path.abspath(configfile)
        seed = int(argv[2])
        projectfolder = argv[3]
        projectfolder = os.path.abspath(projectfolder)
    except:
        print('%s [CONFIG_FILE] [SEED] [PROJECT_FOLDER]' % argv[0])
        sys.exit(1)

    interactive_experiment(configfile, seed, projectfolder)


if __name__ == "__main__":
    main(sys.argv)
