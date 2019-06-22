import sys, getopt
import numpy as np
import pandas as pd
from pudb import set_trace as st
import matplotlib.pyplot as plt

def main(argv):
    try:
        classroom_statsfile = argv[1]
        output_file = argv[2]
    except:
        print('%s [ClassRoom_Stats.csv] [OutputPath]' % argv[0])
        sys.exit(1)
        
    classroom_stats = pd.read_csv(classroom_statsfile)

    X = classroom_stats['Turn']
    fig, axs = plt.subplots(4, 1, figsize=(10, 10), sharex=True)
    plot_mean(X, classroom_stats['NoiseLevel'], ax=axs[0], label='Noise Leve', color='#9134ed', ylimits=(0.0, 1.0))
    plot_mean_with_std(X, classroom_stats['Happiness_mean'], classroom_stats['Happiness_std'], ax=axs[1], label='Happiness', color='#ed3491', ylimits=(-1.0, 1.0))
    plot_mean_with_std(X, classroom_stats['Energy_mean'], classroom_stats['Energy_std'], ax=axs[2], label='Energy', color='#edbf34', ylimits=(0.0, 1.0))
    plot_mean_with_std(X, classroom_stats['Attention_mean'], classroom_stats['Attention_std'], ax=axs[3], label='Attention', color='#91ed34', ylimits=(0.0, 1.0))

    [ax.set_xlabel('Turns') for ax in axs]
    fig.suptitle('Classroom Aggregates', fontsize=16)

    fig.savefig(output_file)
    plt.close(fig)

def plot_mean_with_std(x, means, stds, ax, label, color, ylimits=(None, None)):
    ax.plot(x, means, color=color, marker='o', label=label + ' mean')
    ax.fill_between(x, means-stds, means+stds, facecolor=color, alpha=0.5, label=label + ' std')

    ax.set_ylabel(label)  
    ax.set_ylim(ylimits)
    ax.grid(linestyle='--', alpha=0.5)
    ax.legend(loc='upper left')

def plot_mean(x, means, ax, label, color, ylimits=(None, None)):
    ax.plot(x, means, color=color, marker='o', label=label)

    ax.set_ylabel(label)  
    ax.set_ylim(ylimits)
    ax.grid(linestyle='--', alpha=0.5)
    ax.legend(loc='upper left')


if __name__ == "__main__":
    main(sys.argv)
