# Breakfastclub - Documentation

## Table of Contents
1. [Overview](#overview)
2. [Classroom](#classroom)
3. [Agent Logic](#agent-logic)
4. [Actions](#actions)

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

Agents behavior is controlled by its internal logic (explained under [Agent Logic](#agent-logic)) and is derived from the underlying psychological model ([Big Five personality - OCEAN](https://en.wikipedia.org/wiki/Big_Five_personality_traits)) that is used to describe the agents personality.

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

<img src="/docs/images/prototype2.png" alt="Classroom" align="middle" width="400"/>

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
