from generatePlots import plotHappinessAttentionGraph
import pandas as pd
import os
import sys
import matplotlib.pyplot as plt

import scikit_posthocs as sp
import statsmodels.api as sm

from pudb import set_trace as st


def study(output_folder, summary_files):
    experiments = pd.DataFrame()
    for sf in summary_files:
        exp = pd.read_csv(sf)
        experiments = pd.concat([experiments, exp])

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
    attention_sig = MANOVA.summary_frame.loc['Attention', 'Pr > F'][0] < 0.

    # use Tukey as post-hoc test to test individual group comparision for each dimension
    attention_test = sp.posthoc_tukey(agent_data, val_col='Attention', group_col='Experiment')
    happiness_test = sp.posthoc_tukey(agent_data, val_col='Happiness', group_col='Experiment')

    fig =  studyPlot(classroom_attention, classroom_happiness, classroom_width, classroom_height, classroom_data.index, attention_test, happiness_test, attention_sig, happiness_sig)
    of = os.path.join(output_folder, 'Study_Comparision.png')
    print('Writing Results to %s ...' % of)
    fig.savefig(of)
    plt.close(fig)

    classroom_happiness = agent_data_agg['Happiness', 'mean']
    classroom_width = agent_data_agg['Happiness', 'std']
    classroom_attention = agent_data_agg['Attention', 'mean']
    classroom_height = agent_data_agg['Attention', 'std']

    fig =  studyPlot(classroom_attention, classroom_happiness, classroom_width, classroom_height, classroom_data.index, attention_test, happiness_test, attention_sig, happiness_sig)

    of = os.path.join(output_folder, 'Study_Comparision-AgentBased.png')
    print('Writing Results to %s ...' % of)
    fig.savefig(of)
    plt.close(fig)


def studyPlot(classroom_attention, classroom_happiness, width, height, labels, attention_test, happiness_test, attention_sig, happiness_sig):
    fig = plt.figure(figsize=(10, 10*(4/3)))
    gs = plt.GridSpec(4, 3, figure=fig)
    ax1 = fig.add_subplot(gs[:3, :])
    ax2 = fig.add_subplot(gs[3, 0])
    ax3 = fig.add_subplot(gs[3, 1])
    ax_cb = fig.add_subplot(gs[3, 2])
   
    plotHappinessAttentionGraph(classroom_attention, classroom_happiness, width=width, height=height, labels=labels, include_means=False, suptitle='Experiment comparison', ax=ax1)

    # Calculate pos for lagend
    ax_cb.axis('off')
    cb_pos = list(ax_cb.figbox.bounds)
    cb_pos[2] = cb_pos[2] / 3.0 
    cb_pos[3] = cb_pos[3] / 3.0 

    ax2.set_title('Attention')  
    sp.sign_plot(attention_test, ax=ax2, cbar_ax_bbox=cb_pos)
    ax3.set_title('Happiness')  
    sp.sign_plot(happiness_test, ax=ax3, cbar_ax_bbox=cb_pos)

    ax_cb.text(-1.0, 0.8, 'MANOV Significancy', transform=ax_cb.transAxes)
    ax_cb.text(-1.0, 0.7, 'Happiness: %s' % ('True' if happiness_sig else 'False'), transform=ax_cb.transAxes) 
    ax_cb.text(-1.0, 0.6, 'Attention: %s' % ('True' if attention_sig else 'False'), transform=ax_cb.transAxes) 

    fig.tight_layout(rect=[0.0, 0.00, 1.0-0.1, 1.0-0.1])

    return fig

def main(argv):
    # Very simple argument parser
    try:
        output_folder = argv[1]
        summary_files = []
        for folder in argv[2:]:
            summary_file = os.path.join(os.path.abspath(folder), 'Experiment_summary.csv')
            if os.path.isfile(summary_file):
                summary_files.append(summary_file)
    except:
        print('%s [OUTPUT_FOLDER] [EXPERIMENT_FOLDER1] [EXPERIMENT_FOLDER2] ... [EXPERIMENT_FOLDERN]' % argv[0])
        sys.exit(1)

    study(output_folder, summary_files)


if __name__ == "__main__":
    main(sys.argv)