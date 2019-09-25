import sys, getopt
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import matplotlib
import os
import seaborn as sns
import shutil
import itertools
from matplotlib.colors import TABLEAU_COLORS

from pudb import set_trace as st

# https://www.sessions.edu/color-calculator/
NOISE_COLOR = '#9134ed'
HAPPINESS_COLOR = '#ed3491'
MOTIVATION_COLOR = '#edbf34'
ATTENTION_COLOR = '#91ed34'
STUDYING_COLOR = '#2e6ff0'
QUARREL_COLOR = '#ff3927'

agentBehaviors = ['Break(INACTIVE)', 'Break(TRANSITION)', 'Break(WAITING)', 'Break(EXECUTING)',
               'Chat(INACTIVE)', 'Chat(TRANSITION)', 'Chat(WAITING)', 'Chat(EXECUTING)',
               'StudyAlone(INACTIVE)','StudyAlone(TRANSITION)', 'StudyAlone(WAITING)', 'StudyAlone(EXECUTING)',
               'StudyGroup(INACTIVE)','StudyGroup(TRANSITION)', 'StudyGroup(WAITING)', 'StudyGroup(EXECUTING)',
               'Quarrel(INACTIVE)','Quarrel(TRANSITION)', 'Quarrel(WAITING)', 'Quarrel(EXECUTING)',
        ]

agentBehaviorsLabels = ['B(I)', 'B(T)', 'B(W)', 'B(E)',
               'C(I)', 'C(T)', 'C(W)', 'C(E)',
               'SA(I)','SA(T)', 'SA(W)', 'SA(E)',
               'SG(I)','SG(T)', 'SG(W)', 'SG(E)',
               'Q(I)','Q(T)', 'Q(W)', 'Q(E)',
        ]


NUM_ACTION_STATES = 4
behavior_colors = ['#512bc4', '#c4892b', '#9ec42b', '#c42b9e', '#5dc7b7']

STUDY_AGENTINFO_FILE = "AgentInfo.csv"
STUDY_COMPARISION_FILE = "Study_Comparision-AgentBased.png"
ANALYSIS_PERSONALITYTYPE_STATE_COMPARISION_FILE = "PersonalityType_State-Comparision"
ANALYSIS_PERSONALITYTYPE_BEHAVIOR_COMPARISION_FILE = "PersonalityType_Behavior-Comparision"


def generatePlots(classroom_stats, agents_stats, agent_infos, output_folder, skip_agent_plots=False):
    """Generate all plots of a single experiment (HA-Plot, Classroom Aggregats and AgentInfo)
    
    Arguments:
        classroom_stats_file {[type]} -- [description]
        agents_stats_file {[type]} -- [description]
        output_folder {[type]} -- [description]
    
    Keyword Arguments:
        skip_agent_plots {bool} -- [description] (default: {False})
    """
    output_folder = os.path.abspath(output_folder)

    agent_out = os.path.join(output_folder, 'HA-Plot.png')
    print('Plot Happiness vs Attention Plot to [%s] ... ' % agent_out)

    # Calculate plot limits based on means
    max_mean_durations = agent_infos['mean_durations'].apply(max).max() * 1.1
    max_relative_durations = agent_infos['relative_durations'].apply(max).max() * 1.1

    # Attention is calculated only during studying
    agent_attention = agents_stats[agents_stats['IsStudying']][['Tag', 'Attention']].groupby('Tag').mean()
    agent_means = agents_stats[['Tag', 'Happiness', ]].groupby('Tag').mean()
    agent_means = pd.concat([agent_attention, agent_means], axis=1)

    fig = plotHappinessAttentionGraph(agent_means.values.T[0], agent_means.values.T[1], labels=agent_means.index)
    fig.savefig(agent_out)
    plt.close(fig)

    if(not skip_agent_plots):
        # Generate Agent Plots
        for agent, info in agent_infos.iterrows():
            agent_info_out = os.path.join(output_folder, '%s-Stats.png' % (agent))
            print('Generating Agent Info plot: %s ...' % agent_info_out)
            plotAgentInfo(agent, info, agent_info_out, agentBehaviorsLabels, behavior_colors, mean_ylimits=(0.0, max_mean_durations), relative_ylimits=(0.0, max_relative_durations))

    classroom_out = os.path.join(output_folder, 'ClassroomAggregates.png')
    print('Plot Classroom Aggregates to [%s] ...' % classroom_out)
    plotClassroomAggregates(classroom_stats, classroom_out)
   

def plotHappinessAttentionGraph(attention, happiness, attention_std=None, happiness_std=None, width=None, height=None, suptitle='', labels=None, include_means=True, normalize=True, ax=None):

    # Use figure and axes given, or create a new figure with a single axis
    if ax:
        fig = ax.figure
    else:
        fig, ax = plt.subplots(1, 1, figsize=(10, 10))

    attention_mean = np.mean(attention)
    happiness_mean = np.mean(happiness)

    if not attention_std:
        attention_std = np.std(attention)
    if not happiness_std:
        happiness_std = np.std(happiness)

    if width is None:
        if labels is None:
            for a, h, in zip(attention, happiness):
                ax.scatter(h, a)
        else:
            for a, h, l in zip(attention, happiness, labels):
                ax.scatter(h, a, label=l)
    else:
        colors = itertools.cycle(list(TABLEAU_COLORS.values()))
        if labels is None:
            for a, h, w, hi, c in zip(attention, happiness, width, height, colors):
                w = max(w, 0.01)
                hi = max(hi, 0.01)
                ax.add_patch(matplotlib.patches.Ellipse((h, a), w, hi, color=c, alpha=0.3))
        else:
            for a, h, l, w, hi, c in zip(attention, happiness, labels, width, height, colors):
                w = max(w, 0.01)
                hi = max(hi, 0.01)
                ax.add_patch(matplotlib.patches.Ellipse((h, a), w, hi, label=l, color=c, alpha=0.3))
    
    if include_means:
        ax.axhline(attention_mean, linestyle='--', label='Mean + Std')
        ax.barh(attention_mean, left=0.0, width=2.0, height=attention_std, align='center', label=None, alpha=0.2, color='blue')
        ax.axvline(happiness_mean, linestyle='--', label=None)
        ax.bar(happiness_mean, height=1.0, width=happiness_std, align='center', label=None, alpha=0.2, color='blue')

        ax.text(1.01, attention_mean,'%1.2f'%attention_mean, fontsize=12, color='blue', va='center')
        ax.text(happiness_mean, 1.01,'%1.2f'%happiness_mean, fontsize=12, color='blue', ha='center')

    if normalize:
        ax.set_xlim(0.0, 1.0)
        ax.set_ylim(0.0, 1.0)
    else:
        xmax = max(happiness)*1.1
        xmin = min(happiness)*0.9
        ymax = max(attention)*1.1
        ymin = min(attention)*0.9
        ax.set_xlim(xmin, xmax)
        ax.set_ylim(ymin, ymax)

    ax.set_xlabel('Happiness')
    ax.set_ylabel('Attention')
    if labels is not None:
        ax.legend()
    
    fig.suptitle('Happiness vs Attention\n%s' % suptitle, fontsize=16)
    return fig
    #fig.savefig(output_file)
    #plt.close(fig)


def plotClassroomAggregates(table, output_file):
    X = table['Turn']
    fig, axs = plt.subplots(6, 1, figsize=(10, 15), sharex=True)
    plot_mean(X, table['NoiseLevel'], ax=axs[0], label='Noise Level', color=NOISE_COLOR, ylimits=(0.0, max(2.0, table['NoiseLevel'].max()*1.1)))
    plot_mean_with_std(X, table['Happiness_mean'], table['Happiness_std'], ax=axs[1], label='Happiness', color=HAPPINESS_COLOR, ylimits=(0.0, 1.0))
    plot_mean_with_std(X, table['Motivation_mean'], table['Motivation_std'], ax=axs[2], label='Motivation', color=MOTIVATION_COLOR, ylimits=(0.0, 1.0))
    plot_mean_with_std(X, table['Attention_mean'], table['Attention_std'], ax=axs[3], label='Attention', color=ATTENTION_COLOR, ylimits=(0.0, 1.0))
    plot_mean(X, table['Studying_sum'] / table['nAgents'] * 100.0, ax=axs[4], label='% Studying', color=STUDYING_COLOR, ylimits=(0.0, 100.0))
    plot_mean(X, table['Quarrel_sum'] / table['nAgents'] * 100.0, ax=axs[5], label='% Quarreling', color=QUARREL_COLOR, ylimits=(0.0, 100.0))

    [ax.set_xlabel('Turns') for ax in axs]
    fig.suptitle('Classroom Aggregates', fontsize=16)

    fig.savefig(output_file)
    plt.close(fig)


def plot_mean_with_std(x, means, stds, ax, label, color, ylimits=(None, None), marker=None):
    ax.plot(x, means, color=color, marker=marker, label=label + ' mean')
    if stds is not None:
        ax.fill_between(x, means-stds, means+stds, facecolor=color, alpha=0.5, label=label + ' std')

    # Add overall mean
    om = means.mean()
    ax.axhline(om, color=color, linestyle='--', label='Overall Mean')
    ax.text(max(x)*1.06, om+0.00,'%1.2f'%om, fontsize=12, color=color, va='center')

    ax.set_ylabel(label)  
    ax.set_ylim(ylimits)
    ax.grid(linestyle='--', alpha=0.5)
    ax.legend(loc='upper left')

def plot_mean(x, means, ax, label, color, ylimits=(None, None)):
    plot_mean_with_std(x, means, None, ax, label, color, ylimits=ylimits)



def plotAgentInfo(name, info, output_file, actions, action_colors, mean_ylimits=(0.0, 1.0), relative_ylimits=(0.0, 1.0)):
    """
    Generate an Agent Info plot and write it to given location
    """
    fig, axs = plt.subplots(3, 1, figsize=(10, 20))

    # Heatmap
    generateTransitionMatrixPlot(info['TM'], axs[0], actions, title='State Transition Probabilities')

    # Plot total Action duration
    generateActionBarsPlot(info['relative_durations'], None, axs[1], actions, action_colors, ylimits=relative_ylimits, title='Total Action Durations', ylabel='Duration [%]')
    
    # Show average action durations
    generateActionBarsPlot(info['mean_durations'], info['mean_durations_std'], axs[2], actions, action_colors, ylimits=mean_ylimits, title='Average Action Durations', ylabel='Duration [turns]')

    # Make sure everything fits into the figure 
    fig.suptitle('Agent Info: %s - %s' % (name, info['studentname']), fontsize=16)
    axs[0].set_title('%s: %s\n' % (info['personalitytype'], info['personality']))
    plt.tight_layout(rect=[0.0, 0.00, 1.0-0.05, 1.0-0.05])
    fig.savefig(output_file)
    plt.close(fig)


def generateTransitionMatrixPlot(tm, ax, actions, title=''):
    """Generate a heatmap
    """
    cmap = sns.cubehelix_palette(as_cmap=True, light=.9)
    sns.heatmap(tm, annot=True, fmt=".2f", ax=ax, square=False, cmap=cmap, cbar=False, vmin=0.0, vmax=1.0)

    ax.set_title(title)
    ax.set_xticklabels(actions, rotation=00)
    ax.set_yticklabels(actions, rotation=0)


def generateActionBarsPlot(y, std, ax, actions, action_colors, ylimits=(0.0, 1.0), title='', ylabel=''):
    """
    Generate a bar plot for each action and action state
    """
    width = 0.07
    xpos = []
    for idx, (i, color) in enumerate(zip(range(0, len(actions), NUM_ACTION_STATES), action_colors)):
        #xs = [0.5*idx-0.12, 0.5*idx+0.00, 0.5*idx+0.12]
        xs = [0.5*idx+x for x in np.linspace(-0.15, 0.15, num=NUM_ACTION_STATES)]
        #ys = [y[i+0], y[i+1], y[i+2]]
        ys = [y[i+x] for x in range(NUM_ACTION_STATES)]
        if std is None:
            ss = [None]*len(xs)
        else:
            #ss = [std[i+0], std[i+1], std[i+2]]
            ss = [std[i+x] for x in range(NUM_ACTION_STATES)]
        alpha = np.linspace(0.3, 1.00, num=NUM_ACTION_STATES)

        for _x, _y, _std, a in zip(xs, ys, ss, alpha):
            xpos.append(_x)
            ax.bar(_x, _y, width, yerr=_std, capsize=10, ecolor='grey', color=color, alpha=a)
            ax.text(_x+0.00, _y+0.01,'%2.0f'%_y, fontsize=10, color='black', va='bottom')
       
    ax.set_xticks(xpos)
    ax.set_xticklabels(actions, rotation=00, size=8) 
    ax.set_ylim(ylimits[0], ylimits[1])

    ax.set_title(title)
    ax.set_ylabel(ylabel)
