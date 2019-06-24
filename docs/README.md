# Breakfastclub - Documentation

## Table of Contents
1. [Overview](#overview)
2. [Classroom](#classroom)
3. [Agent Logic](#agent-logic)
4. [Actions](#actions)
5. [Personality Traits](#personality-traits)
6. [Analysis](#analysis)
 

## Overview
**Breakfastclub** is a multi-agent based social simulation of a virtual classroom
developed using the [Unity3D](http://unity.com) Framework.

At the moment the simulation is running autonomous, with the user only being able to pause
the simulation and observe the state of an agent.
Future version will support interacting with agents and will provide a way for users
to learn to control the virtual classroom.

The main components of the simulation are a variable number of autonomous agents
and several individual and group tables used for studying.
The simulation is discrete and has several random processes that can be seeded (i.e. controlled).

Agents behavior is controlled by its internal logic (explained under [Agent Logic](#agent-logic)) and is derived from the underlying psychological model ([Big Five personality - OCEAN](https://en.wikipedia.org/wiki/Big_Five_personality_traits)) that is used to describe the agents personality. An overview is given in section [Personality Traits](#personality-traits).

Agents can perform one of the following actions (more details under [Actions](#actions))

* Take a break
* Chat with other agents
* Study alone
* Study in groups
* Quarrel with another agent

The agent logic will decide which action to perform based on the personalty of the agent, the internal states Energy and Happiness and the availability of tables and other agents.

## Classroom
The classroom defines the environment of the simulation, and holds all agents and
tables used during the simulation.

Agents can move freely about in the classroom, and do so in order to perform different actions that require them to be in specific locations (e.g. agents have to be at a table in order to study, or have to be close to each other in oder to chat).

While the agents perform different actions, they generate **noise** that is aggregated over all agents and produces a classroom wide noise level. The amount of noise
in the classroom has an effect on the possible actions an agent can perform (e.g.
agents cannot study if its too noisy).

<p align="center">
    <img src="/docs/images/Prototype2.png" alt="Classroom" align="middle" width="400"/>
</p>

## Agent Logic
The basic agents logic is defined as an infinite loop in which an Agent is
evaluating at each iteration which action to perform and executes the selected action.

### Actions
Actions are ranked independent of each other and the highest ranked action is chosen as the desired action. The desired action is executed if possible, but in case it cannot
the agent performs the **default action** Break (i.e. the agent takes a break).

Each action executed changes the two internal states of the agent

* **Energy**: A value between [0.0, 1.0] that is consumed by Studying and Quarrel, and recovered by either taking a break or chatting.
* **Happiness**: A value between [-1.0, 1.0] that is decreased if the agent cannot perform the desired action or Quarrels with another agent.

In addition there is a third internal variable, the **Attention** of the agent, that is calculated based on the Energy, Happiness and Noise level in the classroom.

It is not used to control agents behavior but evaluate the agents performance and study progress.

### Interactions
Certain actions require the interaction of Agents (i.e. chat, quarrel).
Those interactions have to be commenced by one agent (i.e. Agent A), and accepted by the other (i.e. Agent B).

Agent A will request an interaction in one iteration, and Agent B will evaluate that request in either the same or latest the next iteration. If the Agent B accepts the request depends on a random process that is governed by the personality trait **consciousness** of Agent B. High levels of consciousness will make it less likely for the agent to be accept the request (i.e. to be distracted from its current action
the Agent A).

Agent A on the other side will repeat requesting interactions a fixed number of times,
or based on its own consciousness level, whereby higher levels of consciousness cause the Agent to try more often (**NOTE: This still has to be implemented and tested**).

## Actions
The Agent has several actions it can perform in each iteration. Each action has three
internal states. 

* **Inactive**: While not being executed nor considered for execution
* **Waiting**: the agent started the action but cannot execute it because of some missing external factor
* **Executing**: the agent actually performs the action

An Action can be in the state waiting if it relies on another agent or if the executing agent is traversing the classroom to reach its destination.
Waiting for an action will reduce the agents Happiness level on each iteration.

### Break - Take a break

**High Score:** Low Energy, Low *Extraversion* 

**Requirements:** None

**Effects:** Restores Energy

This is the **default action** in which the agent can perform anywhere without any requirements. It is used to recover Energy.

### Chat - Chat with other agents

**High Score:** Low Energy, High *Extraversion* 

**Requirements:** Another Agent that is willing to chat.

**Effects:** Restores Energy, Produces noise

This is action equivalent to 'Break' but is preferred by Agents with a high level of *Extraversion*. The agent will start this action in the state waiting until it can convince another agent to start chatting. After several missing attempts, it will choose another agent to chat with. Agents with which to chat are chosen randomly.

### Study alone

**High Score:** High Happiness, Low *Extraversion* 

**Requirements:** A free individual table, Low noise level

**Effects:** Reduce Energy

The noise level until which the agent can study is dependent on its *conscientiousness* trait. This action is equivalent to Studying in a group, but is preferred by agents with low levels of *Extraversion*.

### Study in groups

**High Score:** High Happiness, High *Extraversion* 

**Requirements:** A seat on a group table, other agents studying, low noise level

**Effects:** Reduce Energy, Increase Noise

Equivalent to Study alone, but preferred by agents with high levels of *Extraversion*.
In order to avoid deadlocks, an Agent can start the action in its state Waiting without other agents. The agent will continuo to wait for a fixed number of iterations.

### Quarrel - Quarrel with another agent

**High Score:** Low Happiness and Low energy (with a threshold on the energy level)

**Requirements:** Another agent

**Effects:** Reduce Happiness and Energy, Increase Noise by a lot

Once the Happiness of an agent droppes to a very low level, the agent is likely to start
to quarrel with another agent, that is randomly chosen.

## Personality Traits
The personality Traits have been studied extensively since the 1960s, we base our model on
descriptions of the model in the works

Ehrler, D. J., Evans, J. G., & McGhee, R. L. (1999). Extending Big-Five theory into childhood: A preliminary investigation into the relationship between Big-Five personality traits and behavior problems in children. Psychology in the Schools, 36(6), 451–458. [DOI](https://doi.org/10.1002/(SICI)1520-6807(199911)36:6<451::AID-PITS1>3.0.CO;2-E)

Asendorpf, J. B., & Van Aken, M. A. G. (2003). Validity of Big Five Personality Judgments in Childhood: A 9 Year Longitudinal Study. European Journal of Personality, 17(1), 1–17. [DOI](https://doi.org/10.1002/per.460)

An overview of the models is given here

| Personality Trait | Description | Effects |
|-------------------|-------------|--------|
| Neuroticism       | The general tendency to experience negative affects such as fear, sadness, embarrassment, anger, guilt, and disgust is the core of the N domain. However, N includes more than susceptibility to psychological distress. Perhaps because disruptive emotions interfere with adaptation, those who score high in N are also prone to have irrational ideas, to be less able to control their impulses, and to cope more poorly then others with stress.   | Stronger Decrease in happiness |
| Extraversion      | E The general tendency to be outgoing. In addition, high E’s prefer large groups and gatherings and are assertive, active, and talkative. They like stimulation and tend to be cheerful in disposition. They are upbeat, energetic, and optimistic | Effects Actions Break and Chat |
| Openness           | The general tendency to be curious about both inner and outer worlds. O includes the elements of an active imagination, aesthetic sensitivity, attentiveness to inner feelings, preference for variety, intellectual curiosity, and independence of judgment. A high O also includes individuals who are unconventional, willing to question authority, and ready to entertain new ethical and social ideas.| Attention |
| Agreeableness     | A The general tendency to be altruistic. The high A is sympathetic to others and eager to help them, and believes that others will be equally helpful in return. By contrast, the low A is antagonistic and egocentric, skeptical of others’ intentions, and competitive rather than cooperative.| Counter balances Neuroticism |
| Conscientiousness | C The general tendency to be able to resist impulses and temptations. The conscientious individual is purposeful, strong-willed, and determined. On the positive side, high C is associated with academic and occupational achievement; on the negative side, it may lead to annoying fastidiousness, compulsive neatness, or workaholic behavior, Low C’s are not necessarily lacking in moral principles, but they are less exacting in applying them. | Effects number of retries and how likely to accept interactions |

## Analysis
THe simulation is analyzed using a series of python scripts that generate plots based on the csv log file that is gernerated during the execution of the simulation.

## Classroom Aggregates
Besides observing the simulation in real time, analysis is performed using a set of
python tools that will generate plots containing Classroom aggregated information
and individual Agent based information.

### Classroom Aggregates
The Classroom Aggregates Plot shows the mean aggregate of all agents in the
classroom over time.

<p align="center">
    <img src="/docs/images/ClassroomAggregates.png" alt="Classroom aggregates" width="400"/>
</p>

### Agent Info
For each agent, a separate plot containing information about the different behaviors
performed by the agent is generated.

Agent01  |  Agent02  |  Agent03
:-------------------------:|:-------------------------:
![](/docs/images/AgentInfo1.png)  |  ![](/docs/images/AgentInfo2.png) | ![](/docs/images/AgentInfo3.png) 


### Happiness vs Attentino Plot
This is the most abstract and most concise plot, showing the average Happiness
and Attention value for each agent and the classroom average.

<p align="center">
    <img src="/docs/images/HA-Plot.png" alt="Happiness vs Attention" width="400"/>
</p>

