from generatePlots import plotHappinessAttentionGraph
import pandas as pd
import os
import sys
import matplotlib.pyplot as plt

import scikit_posthocs as sp
import statsmodels.api as sm

import seaborn as sns

from pudb import set_trace as st


def study(output_folder, summary_files, agent_files):
    experiments = pd.DataFrame()
    agents_info = pd.DataFrame()
    for sf, af in zip(summary_files, agent_files):
        exp = pd.read_csv(sf)
        experiments = pd.concat([experiments, exp])

        agent_info = pd.read_csv(af)
        agents_info = pd.concat([agents_info, agent_info]) 

    # Extract mean and std over all experiments 
    classroom_data = experiments[experiments['Tag'] == 'Classroom'].groupby('Experiment').agg(['mean', 'std'])
    agent_data = experiments[experiments['Tag'] != 'Classroom']
    agent_data_agg = agent_data.groupby('Experiment').agg(['mean', 'std'])

    # We could get nans in std if we have too few values!
    classroom_data.fillna(0.0, inplace=True)
    agent_data_agg.fillna(0.0, inplace=True)

    classroom_happiness = classroom_data['Happiness', 'mean']
    classroom_width = classroom_data['Happiness', 'std']
    classroom_attention = classroom_data['Attention', 'mean']
    classroom_height = classroom_data['Attention', 'std']

    # Perform a MANOVA analysis on the individual agent happiness/attention values
    MANOVA = sm.multivariate.MANOVA.from_formula(formula="Experiment ~ Happiness + Attention", data=agent_data).mv_test()
    happiness_sig = MANOVA.summary_frame.loc['Happiness', 'Pr > F'][0] < 0.5
    attention_sig = MANOVA.summary_frame.loc['Attention', 'Pr > F'][0] < 0.5

    # use Tukey as post-hoc test to test individual group comparision for each dimension
    attention_test = sp.posthoc_tukey(agent_data, val_col='Attention', group_col='Experiment')
    happiness_test = sp.posthoc_tukey(agent_data, val_col='Happiness', group_col='Experiment')

    os.makedirs(output_folder, exist_ok=True)

    """
    fig =  studyPlot(classroom_attention, classroom_happiness, classroom_width, classroom_height, classroom_data.index, attention_test, happiness_test, attention_sig, happiness_sig)
    of = os.path.join(output_folder, 'Study_Comparision.png')
    print('Writing Results to %s ...' % of)
    fig.savefig(of)
    plt.close(fig)
    """
    classroom_happiness = agent_data_agg['Happiness', 'mean']
    classroom_width = agent_data_agg['Happiness', 'std']
    classroom_attention = agent_data_agg['Attention', 'mean']
    classroom_height = agent_data_agg['Attention', 'std']

    # Calculate agent info based stats
    complete_agent_info = pd.merge(agents_info, agent_data, on=['Tag', 'Instance', 'Experiment'])
    agent_infos_correlates = complete_agent_info.corr(method='spearman')[['Happiness', 'Attention']]
    agent_infos_correlates.drop('Motivation', inplace=True)

    fig =  studyPlot(classroom_attention, classroom_happiness, classroom_width, classroom_height, classroom_data.index, attention_test, happiness_test, attention_sig, happiness_sig, agent_infos_correlates)

    of = os.path.join(output_folder, 'Study_Comparision-AgentBased.png')
    print('Writing Results to %s ...' % of)
    fig.savefig(of)
    plt.close(fig)

    print('Finished!')


def studyPlot(classroom_attention, classroom_happiness, width, height, labels, attention_test, happiness_test, attention_sig, happiness_sig, agent_infos_correlates):
    fig = plt.figure(figsize=(10, 10*(5/4)))
    gs = plt.GridSpec(5, 2, figure=fig)
    ax1 = fig.add_subplot(gs[:3, :])
    ax2 = fig.add_subplot(gs[4, 0])
    ax3 = fig.add_subplot(gs[4, 1])
    ax_cb = fig.add_subplot(gs[3, 1])
    ax_corr = fig.add_subplot(gs[3, 0])
   
    plotHappinessAttentionGraph(classroom_attention, classroom_happiness, width=width, height=height, labels=labels, include_means=False, suptitle='Experiment comparison', ax=ax1)

    # Calculate pos for lagend
    ax_cb.axis('off')
    #cb_pos = ax_cb.get_window_extent().transformed(fig.dpi_scale_trans.inverted())
    cb_pos = ax_cb.get_position() # get the original position 
    new_cb_pos = [cb_pos.x0, cb_pos.y0 + 0.03, cb_pos.width/2, cb_pos.height/2]

    ax3.set_title('Attention')  
    sp.sign_plot(attention_test, ax=ax3, cbar_ax_bbox=new_cb_pos)
    ax2.set_title('Happiness')  
    sp.sign_plot(happiness_test, ax=ax2, cbar_ax_bbox=new_cb_pos)

    ax_cb.text(-0.5, 0.8, 'MANOV Significancy (p < 0.05)', transform=ax_cb.transAxes)
    ax_cb.text(-0.5, 0.7, 'Happiness: %s' % ('True' if happiness_sig else 'False'), transform=ax_cb.transAxes) 
    ax_cb.text(-0.5, 0.6, 'Attention: %s' % ('True' if attention_sig else 'False'), transform=ax_cb.transAxes) 

    ax_corr.set_title('Spearman Rank-Order correlation')
    sns.heatmap(agent_infos_correlates, annot=True, fmt="0.2f", ax=ax_corr, cbar=False)

    fig.tight_layout(rect=[0.0, 0.00, 1.0-0.1, 1.0-0.1])

    return fig

def main(argv):
    # Very simple argument parser
    try:
        output_folder = argv[1]
        summary_files = []
        agent_summary_files = []
        for folder in argv[2:]:
            summary_file = os.path.join(os.path.abspath(folder), 'Experiment_summary.csv')
            agent_summary_file = os.path.join(os.path.abspath(folder), 'Agent_Experiment_summary.csv')
            if os.path.isfile(summary_file):
                summary_files.append(summary_file)
                agent_summary_files.append(agent_summary_file)
    except:
        print('%s [OUTPUT_FOLDER] [EXPERIMENT_FOLDER1] [EXPERIMENT_FOLDER2] ... [EXPERIMENT_FOLDERN]' % argv[0])
        sys.exit(1)

    study(output_folder, summary_files, agent_summary_files)


if __name__ == "__main__":
    main(sys.argv)