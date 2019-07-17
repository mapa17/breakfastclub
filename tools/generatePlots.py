import sys, getopt
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import os
import seaborn as sns

from pudb import set_trace as st

NOISE_COLOR = '#9134ed'
HAPPINESS_COLOR = '#ed3491'
MOTIVATION_COLOR = '#edbf34'
ATTENTION_COLOR = '#91ed34'

agentBehaviors = ['Break(INACTIVE)', 'Break(WAITING)', 'Break(EXECUTING)',
               'Chat(INACTIVE)', 'Chat(WAITING)', 'Chat(EXECUTING)',
               'StudyAlone(INACTIVE)', 'StudyAlone(WAITING)', 'StudyAlone(EXECUTING)',
               'StudyGroup(INACTIVE)', 'StudyGroup(WAITING)', 'StudyGroup(EXECUTING)',
               'Quarrel(INACTIVE)', 'Quarrel(WAITING)', 'Quarrel(EXECUTING)',
        ]
behavior_colors = ['#512bc4', '#c4892b', '#9ec42b', '#c42b9e', '#5dc7b7']


def main(argv):
    try:
        classroom_stats_file = argv[1]
        agents_stats_file = argv[2]
        project_name = argv[3]
    except:
        print('%s [ClassroomStats.csv] [AgentStats.csv] [OutputPath]' % argv[0])
        sys.exit(1)

    generatePlots(classroom_stats_file, agents_stats_file, project_name)
    print('Finished!')


def generatePlots(classroom_stats_file, agents_stats_file, output_folder):
    classroom_stats = pd.read_csv(classroom_stats_file)
    agents_stats = pd.read_csv(agents_stats_file)

    # First line in agent stats are personality traits
    personalities_df = agents_stats[agents_stats['Turn'] == -1][['Tag', 'Motivation', 'Happiness', 'Attention', 'Action', 'Desire']].reset_index(drop=True)
    personalities_df.rename(columns={'Motivation': 'Openess', 'Happiness': 'Conscientiousness', 'Attention': 'Extraversion', 'Action': 'Agreeableness', 'Desire': 'Neuroticism'}, inplace=True)
    personalities_df.set_index('Tag', inplace=True)
    personalities = personalities_df.apply(lambda x: ', '.join(['%s: %s'%(k, v) for k, v in x.to_dict().items()]), axis=1)
    
    output_folder = os.path.abspath(output_folder)

    classroom_out = os.path.join(output_folder, 'ClassroomAggregates.png')
    print('Plot Classroom Aggregates to [%s] ...' % classroom_out)
    plotAggregatedStats(classroom_stats, classroom_out)

    agent_out = os.path.join(output_folder, 'HA-Plot.png')
    print('Plot Happiness vs Attention Plot to [%s] ... ' % agent_out)
    plotHappinessAttentionGraph(agents_stats, agent_out)

    print('Calculating Agent infos ...')
    # Get Action indices
    agents_stats['Action_idx'] = agents_stats['Action'].apply(lambda x: identifyAction(x, agentBehaviors))
    agents_stats['Desire_idx'] = agents_stats['Desire'].apply(lambda x: identifyAction(x, agentBehaviors))

    # Calculate Agent Info
    gb = agents_stats.groupby('Tag')
    agent_infos = gb.apply(getAgentInfos)

    max_mean_durations = agent_infos['mean_durations'].apply(max).max() * 1.1
    max_relative_durations = agent_infos['relative_durations'].apply(max).max() * 1.1

    # Generate Agent Plots
    for agent, info in agent_infos.iterrows():
        agent_info_out = os.path.join(output_folder, '%s-Stats.png' % (agent))
        print('Generating Agent Info plot: %s ...' % agent_info_out)
        plotAgentInfo(agent, personalities.loc[agent], info, agent_info_out, agentBehaviors, behavior_colors, mean_ylimits=(0.0, max_mean_durations), relative_ylimits=(0.0, max_relative_durations))


def identifyAction(string, actions):
    for idx, a in enumerate(actions):
        if(string.find(a) > -1):
            return idx
    return -1


def plotHappinessAttentionGraph(agents_stats, output_file):
    fig, ax = plt.subplots(1, 1, figsize=(10, 10))

    agent_means = agents_stats[['Tag', 'Motivation', 'Attention']].groupby('Tag').mean()
    overall_happiness_mean, overall_attention_mean = agent_means.mean().values

    ax.scatter(agent_means.values.T[0], agent_means.values.T[1])

    ax.axhline(overall_attention_mean, linestyle='--', label='Overall Mean Attention')
    ax.axvline(overall_happiness_mean, linestyle='--', label='Overall Mean Happiness')

    ax.set_xlim(-1.0, 1.0)
    ax.set_ylim(0.0, 1.0)
    ax.set_xlabel('Happiness')
    ax.set_ylabel('Attention')
    
    fig.suptitle('Happiness vs Attention', fontsize=16)
    fig.savefig(output_file)
    plt.close(fig)


def plotAggregatedStats(table, output_file):
    X = table['Turn']
    fig, axs = plt.subplots(4, 1, figsize=(10, 10), sharex=True)
    plot_mean(X, table['NoiseLevel'], ax=axs[0], label='Noise Leve', color=NOISE_COLOR, ylimits=(0.0, 2.0))
    plot_mean_with_std(X, table['Happiness_mean'], table['Happiness_std'], ax=axs[1], label='Happiness', color=HAPPINESS_COLOR, ylimits=(-1.0, 1.0))
    plot_mean_with_std(X, table['Motivation_mean'], table['Motivation_std'], ax=axs[2], label='Motivation', color=MOTIVATION_COLOR, ylimits=(0.0, 1.0))
    plot_mean_with_std(X, table['Attention_mean'], table['Attention_std'], ax=axs[3], label='Attention', color=ATTENTION_COLOR, ylimits=(0.0, 1.0))

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


def getAgentInfos(table):
    """Calculate transition matrix and action sequence durations for each agent
    """
    tm, sequence_durations = getTransitionInfo(table['Action_idx'].values, len(agentBehaviors))
    relative_duration, mean_durations, mean_durations_std = calculate_duration_info(sequence_durations)
        
    index=['TM', 'relative_durations', 'mean_durations', 'mean_durations_std']
    return pd.Series([tm, relative_duration, mean_durations, mean_durations_std], index)


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


def plotAgentInfo(name, personality, info, output_file, actions, action_colors, mean_ylimits=(0.0, 1.0), relative_ylimits=(0.0, 1.0)):
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
    fig.suptitle('Agent Info: %s' % (name), fontsize=16)
    axs[0].set_title('%s\n' % personality)
    plt.tight_layout(rect=[0.0, 0.00, 1.0, 1.0-0.05])
    fig.savefig(output_file)
    plt.close(fig)


def generateTransitionMatrixPlot(tm, ax, actions, title=''):
    """Generate a heatmap
    """
    cmap = sns.cubehelix_palette(as_cmap=True, light=.9)
    sns.heatmap(tm, annot=True, fmt=".2f", ax=ax, square=False, cmap=cmap, cbar=False, vmin=0.0, vmax=1.0)

    ax.set_title(title)
    ax.set_xticklabels(actions, rotation=30)
    ax.set_yticklabels(actions, rotation=0)


def generateActionBarsPlot(y, std, ax, actions, action_colors, ylimits=(0.0, 1.0), title='', ylabel=''):
    """
    Generate a bar plot for each action
    """
    width = 0.1
    xpos = []
    for idx, (i, color) in enumerate(zip(range(0, len(actions), 3), action_colors)):
        xs = [0.5*idx-0.12, 0.5*idx+0.00, 0.5*idx+0.12]
        ys = [y[i+0], y[i+1], y[i+2]]
        if std is None:
            ss = [None]*len(xs)
        else:
            ss = [std[i+0], std[i+1], std[i+2]]
        alpha = [0.5, 0.7, 1.0]

        for _x, _y, _std, a in zip(xs, ys, ss, alpha):
            xpos.append(_x)
            ax.bar(_x, _y, width, yerr=_std, capsize=10, ecolor='grey', color=color, alpha=a)
            ax.text(_x+0.00, _y+0.01,'%2.0f'%_y, fontsize=10, color='black', va='bottom')
       
    ax.set_xticks(xpos)
    ax.set_xticklabels(actions, rotation=30, size=8) 
    ax.set_ylim(ylimits[0], ylimits[1])

    ax.set_title(title)
    ax.set_ylabel(ylabel)


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

if __name__ == "__main__":
    main(sys.argv)
