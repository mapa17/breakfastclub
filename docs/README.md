# Breakfastclub - Documentation

## Table of Contents
* [Introduction](#Introduction)
* [Agent based models](#agent-based-models)
* [Classroom](#classroom)
* [Agents](#agents)
* [Agent Logic](#agent-logic)
* [Actions](#actions)
* [Personality Traits](#personality-traits)
* [Analysis](#analysis)
* [References](#References)

 

## Introduction
**Breakfastclub** is an [agent based social simulation](https://en.wikipedia.org/wiki/Agent-based_model) of a virtual classroom
developed using the [Unity3D](http://unity.com) Framework.

The classroom setting is similar to a self organized study group, sharing a single classroom
and where its members are able to independently choose to either to study or perform other recreational activities.

The main components of the system are

* the environment (i.e. classroom)
* the agents
* the simulation mechanism and agent logic

The simulation is discrete running in 'ticks' that correspond to short durations in time.
At each tick an agent is reasoning on what to do next and interacts with its environment and with other agents.
Agents behavior is controlled by its internal logic (explained under [Agent Logic](#agent-logic)) and is derived from the underlying psychological model ([Big Five personality - OCEAN](https://en.wikipedia.org/wiki/Big_Five_personality_traits)) that is used to describe the agents personality. An overview is given in section [Personality Traits](#personality-traits).

The simulation includes several probabilistic elements effecting the actions chosen by
agents as well as their interaction. This randomness can be controlled through seeding the simulation, producing deterministic and reproducible results.

The focus of this work is to provide an environment and platform in order to answer the question
```
How different personalities effect classroom attention and happiness?
```

## Agent based models
Agent based models<sup>[1](#ref1)</sup> are a type of computational model that is widely applied in different sciences (e.g. Biology, Economics, Social sciences) to study the behavior of independent agents and their interaction in a group.

First applications of agent based models in the social sciences go back to Shelling (1971)<sup>[2](#ref2)</sup> modeling the dynamics of social segregation, ore more recent studies with a biological focus on the spread of contagious disease<sup>[3](#ref3)</sup>.

The focus of many agent based models is to study and explain macroscopic group dynamics based on simple agent based mechanisms. In other words, how in a complex dynamic system of many interacting agents, specific group dynamic behavior [emerges](https://en.wikipedia.org/wiki/Emergence) based on specific properties of the individual agents.

## Classroom
The environment is modeled as classroom that contains all agents and several tables
that are used by students to study alone or in groups.

Agents can move freely about in the classroom, but approach each other when they
interact with one another or sit on a table. The movement or position in the classroom
has no effect on the simulation.

While the agents perform different actions, they generate **noise** that is accumulated over all agents and produces a classroom wide noise level. The amount of noise
in the classroom has an effect on the possible actions an agent can perform (e.g.
agents cannot study if its too noisy) and the attention that agents have during studying.

<p align="center">
    <img src="/docs/images/Prototype2.png" alt="Classroom" align="middle" width="400"/>
</p>

## Agents

<p align="center">
    <img src="/docs/images/AgentOverview.png" alt="Agent" align="middle" width="600"/>
</p>

Agents can perform one of the following action (more details under [Actions](#actions))

* Take a break
* Chat with other agents
* Study alone
* Study in groups
* Quarrel with another agent

The agent logic will decide which action to perform based on the personalty of the agent, the internal states **Motivation** and **Happiness** and the availability of tables and other agents.

The internal states modeled for each agents are

* Motivation
* Happiness
* Attention (only when studying)

The agent itself is a dynamic system, in which the actions are chosen by the internal states and the personality of the agent, and the actions performed in combination with the personality of the agent effect the internal states.

<p align="center">
    <img src="/docs/images/AgentDynamics.png" alt="Agent Dynamics" align="middle" width="400"/>
</p>

The complete system is the interaction of multiple agents with each other as well as with the environment.

<p align="center">
    <img src="/docs/images/GroupDynamics.png" alt="System Dynamics" align="middle" width="400"/>
</p>

## Agent Logic
The basic agents logic is defined as an infinite loop in which an Agent is
evaluating at each iteration which action to perform.

In addition the agent handles all interactions with other agents, and updates its internal states.

For a more detailed explanation on the agent Logic is found in the separate documentation explaining the [Agent Logic](AgentLogic.md).


## Actions
Actions are ranked independent of each other and at each iteration (tick) an agent
is calculate a rating for each possible action.

That rate depends on the internal states as well as on the personality of the agent.

One of the actions is selected to be performed during the current iteration.
The selection is a probabilistic process that is based on the scores, giving a higher
probability to be selected to the actions with a higher score.

The selected action is executed if possible, but in case it cannot the agent performs
the **default action** Break (i.e. the agent takes a break). More details on
how the score for an action is calculated can be found as part of the [Agent Logic
](AgentLogic.md) description.

The execution of an agent changes the two internal states of the agent

* **Motivation**: A value between [0.0, 1.0] that is consumed by Studying and Quarrel, and recovered by either taking a break or chatting.
* **Happiness**: A value between [0.0, 1.0] that is decreased if the agent cannot perform the desired action or Quarrels with another agent.

In addition there is a third internal variable, the **Attention** of the agent,
that is only calculated if the agent is studying and is based on the Motivation,
Happiness and Noise level in the classroom. A agent that is not studying has an
attention of zero.


### Interactions

Certain actions require the interaction of Agents (i.e. chat, quarrel).
Those interactions have to be commenced by one agent (i.e. Agent A), and
accepted by another (i.e. Agent B).

Agent A will request an interaction in one iteration, and Agent B will evaluate
that request in either the same or latest the next iteration. If the Agent B accepts
the request depends on a random process that is governed by the personality trait
consciousness of Agent B. High levels of consciousness will make it less likely
for the agent to be accept the request (i.e. to be distracted from its current action
the Agent A).

Agent A on the other side will repeat requesting interactions a fixed number of times,
or based on its own consciousness level, whereby higher levels of consciousness
cause the Agent to try more often (**NOTE: This still has to be implemented and tested**).



## Personality Traits

The study of human personality traits and how to measure and define personality
goes back to the beginning of the 20th century. Our work is based on the
([Big Five personality - OCEAN](https://en.wikipedia.org/wiki/Big_Five_personality_traits))
model that was developed in the 1960s<sup>[3](#ref3)</sup> and has been used since in
theoretical and applied Psychology. The model is derived from empirical studies (mostly self description of
patients about their behavior and image of themselves), and has no theoretical foundation.
Based on factor analysis the dimensions of the model have been extracted from the
available date.

As the name suggests, the model defines five orthogonal dimensions on which the personality
of a human can be defined.

<p align="center">
    <img src="/docs/images/OCEAN-model.png" alt="OCEAN Model" align="middle" width="600"/>
</p>

Each dimension is scaled to take a value between [0, 1] and the extremes of each dimension
are associated with a set of typical behavior patterns.


| Personality Trait | Description | Effects |
|-------------------|-------------|--------|
| Neuroticism       | The general tendency to experience negative affects such as fear, sadness, embarrassment, anger, guilt, and disgust is the core of the N domain. However, N includes more than susceptibility to psychological distress. Perhaps because disruptive emotions interfere with adaptation, those who score high in N are also prone to have irrational ideas, to be less able to control their impulses, and to cope more poorly then others with stress.   | Stronger Decrease in happiness |
| Extraversion      | E The general tendency to be outgoing. In addition, high E’s prefer large groups and gatherings and are assertive, active, and talkative. They like stimulation and tend to be cheerful in disposition. They are upbeat, energetic, and optimistic | Effects Actions Break and Chat |
| Openness           | The general tendency to be curious about both inner and outer worlds. O includes the elements of an active imagination, aesthetic sensitivity, attentiveness to inner feelings, preference for variety, intellectual curiosity, and independence of judgment. A high O also includes individuals who are unconventional, willing to question authority, and ready to entertain new ethical and social ideas.| Attention |
| Agreeableness     | A The general tendency to be altruistic. The high A is sympathetic to others and eager to help them, and believes that others will be equally helpful in return. By contrast, the low A is antagonistic and egocentric, skeptical of others’ intentions, and competitive rather than cooperative.| Counter balances Neuroticism |
| Conscientiousness | C The general tendency to be able to resist impulses and temptations. The conscientious individual is purposeful, strong-willed, and determined. On the positive side, high C is associated with academic and occupational achievement; on the negative side, it may lead to annoying fastidiousness, compulsive neatness, or workaholic behavior, Low C’s are not necessarily lacking in moral principles, but they are less exacting in applying them. | Effects number of retries and how likely to accept interactions |


### Big Five in the classroom
Most relevant to your work are empirical studies on the effect of different Big Five
Personality types on the school performance and behavior of students<sup>[4](#ref4)
</sup> <sup>[5](#ref5)</sup> as well as specific Personality Traits of children
with ADHD<sup>[6](#ref6)</sup>.

Our simulation is tuned to replicate those findings, so that it can be used to study
and compare different combinations of Personality Profiles.

## Analysis
The simulation is analyzed in three steps, running a set of python scripts that
are executing the simulation and analyze the csv files that are generated during
the simulation.

<p align="center">
    <img src="/docs/images/Analysis-Overview.png" alt="Analysis-Overview" width="600"/>
</p>

### Step 1 - Simulation
During the first step of the analysis the simulation is run, based on a configuration
that is defining the simulation mechanics, as well as another another configuration
that defines the classroom profile (the composition of personality types of the
different agents).

The goal of the first step is to answer, how a particular configuration of agents
behaves.

<p align="center">
    <img src="/docs/images/Simulation-Overview.png" alt="Simulation-Overview" width="800"/>
</p>

The result of the first step is the **Agent Plot**, that is an graph for each agent,
containing information about the different behaviors performed by the agent and
how long they lasted on average.

| Agent01  |  Agent02  |  Agent03 |
:-------------------------:|:------:|:-------------------:
| ![](/docs/images/AgentInfo1.png)  |  ![](/docs/images/AgentInfo2.png) | ![](/docs/images/AgentInfo3.png) |

In addition to information about every single agent, the **Classroom Aggregates** Plot
is generated that contains the several measures averaged over the whole classroom,
and their change over time.

<p align="center">
    <img src="/docs/images/ClassroomAggregates.png" alt="Classroom aggregates" width="600"/>
</p>

### Step 2 - Experiment
The second step is analyzing the effect of randomness (i.e. approximating a set of
not modeled background aspects, based on the assumption that a real world class would
not behave identical even if setup multiple times in the same way.).

It therefore executes the simulation multiple times using the same Simulation and
Classroom configuration, but different random seeds.

<p align="center">
    <img src="/docs/images/Experiment-Overview.png" alt="Classroom aggregates" width="600"/>
</p>

The result is a very concisest plot showing the average Happiness vs Attention (**HA-Plot**)
of the classroom over the complete simulation for all the Simulations executed step 1.

<p align="center">
    <img src="/docs/images/HA-Plot.png" alt="Happiness vs Attention" width="400"/>
</p>

### Step 3 - Study
The last step is comparing how different Experiments (i.e. Combinations of Personality Profiles)
relate to each other.

<p align="center">
    <img src="/docs/images/Study-Overview.png" alt="Study Overview" width="600"/>
</p>

The result of this Study is another HA-Plot, this time showing different Experiments,
displayed as ellipses, indicating the standard deviation of each single experiment.

<p align="center">
    <img src="/docs/images/Study-Comparision.png" alt="Study-Comparision" width="400"/>
</p>


# References
<a name="ref1">1</a>: Schelling, T. C. (1971). Dynamics Model of Segregation. Journal of Mathematical Sociology, 1(May 1969), 143–186. [DOI](https://doi.org/10.1080/0022250X.1971.9989794)

<a name="ref2">2</a>: Perez, L., & Dragicevic, S. (2009). An agent-based approach for modeling dynamics of contagious disease spread. 
International Journal of Health Geographics, 8(1), 1–17. [DOI](http://doi.org/10.1186/1476-072X-8-50)

<a name="ref3">3</a>: Norman, W. T. (1963). Toward an adequate taxonomy of personality attributes. Journal of Abnormal and Social Psychology, 66(6), 574–583. [DOI](https://doi.org/10.1037/h0040291)

<a name="ref4">4</a>: Ehrler, D. J., Evans, J. G., & McGhee, R. L. (1999). Extending Big-Five theory into childhood: A preliminary investigation into the relationship between Big-Five personality traits and behavior problems in children. Psychology in the Schools, 36(6), 451–458. [DOI](https://doi.org/10.1002/(SICI)1520-6807(199911)36:6<451::AID-PITS1>3.0.CO;2-E)

<a name="ref5">5</a>: Asendorpf, J. B., & Van Aken, M. A. G. (2003). Validity of Big Five Personality Judgments in Childhood: A 9 Year Longitudinal Study. European Journal of Personality, 17(1), 1–17. [DOI](https://doi.org/10.1002/per.460)

<a name="ref6">6</a>: Nigg, J. T., Blaskey, L. G., Huang-Pollock, C. L., Hinshaw, S. P., John, O. P., Willcutt, E. G., & Pennington, B. (2002). Big five dimensions and ADHD symptoms: Links between personality traits and clinical symptoms. Journal of Personality and Social Psychology, 83(2), 451–469. [DOI](https://doi.org/10.1037/0022-3514.83.2.451)