using System;
using System.Collections.Generic;
using UnityEngine;

public class StudyGroup : AgentBehavior
{
    private Table lastTable;
    private Vector3 destination;

    private int retry_cnter;

    public StudyGroup(Agent agent) : base(agent, AgentBehavior.Actions.StudyGroup, "StudyGroup", agent.SC.StudyGroup) { }
    /*
     *  • requirements: no quarrel, free individual table, attention
     *  • effects: learning, reduces energy every turn
    */
    public override bool possible()
    {
        switch (state)
        {
            // Start to engage another agent
            case ActionState.INACTIVE:

                // Get a new table
                (Table table, Transform seat) = getTable();
                if (table != null)
                {
                    lastTable = table;
                    destination = seat.position;
                    agent.navagent.destination = destination;
                    //Debug.Log(String.Format("Taking seat at {0}", seat.position));

                    state = ActionState.TRANSITION;
                    retry_cnter = 0;
                    agent.LogDebug(String.Format("Got a table {0}!", lastTable));
                    return true;
                }
                else
                {
                    agent.LogDebug(String.Format("Unable to get a table!"));
                    return false;
                }


            case ActionState.TRANSITION:
                agent.navagent.destination = destination;
                if (IsCloseTo(destination))
                {
                    state = ActionState.WAITING;
                }
                return true;


            case ActionState.WAITING:
                if (table_ready())
                {
                    state = ActionState.EXECUTING;
                }
                else
                {
                    agent.navagent.destination = destination;
                    retry_cnter++;
                    agent.LogDebug(String.Format("Table not ready. Waiting for {0} turns!", retry_cnter));
                }
                return true;
            case ActionState.EXECUTING:
                return table_ready();
        }
        return false;
    }

    private bool table_ready()
    {
        agent.LogDebug("Check if there are still other agents on the table ...");
        // So we sit on the table do we have someone to study with?
        List<Agent> others = lastTable.getOtherAgents(agent);
        foreach (Agent other in others)
        {
            if (other.currentAction is StudyGroup)
            {
                if((other.currentAction.state is ActionState.WAITING) || (other.currentAction.state is ActionState.EXECUTING))
                {
                    if (agent.classroom.noise >= agent.personality.conscientousness * config["NOISE_THRESHOLD"])
                    {
                        agent.LogDebug(String.Format("Cant learn its too noisy {0} > {1}", agent.classroom.noise, agent.personality.conscientousness * config["NOISE_THRESHOLD"]));
                        state = ActionState.WAITING;
                        return false;
                    }
                    agent.LogDebug(String.Format("Found other agent {0} on table!", other));
                    return true;
                }
            }
        }
        agent.LogDebug(String.Format("Could not find anyone at the table ready to study!"));
        state = ActionState.WAITING;
        return false;
    }

    public override double rate()
    {
        double score = CalculateScore(agent.personality.extraversion, config["PERSONALITY_WEIGHT"], ExpGrowth(agent.motivation), config["MOTIVATION_WEIGHT"], ExpGrowth(agent.happiness), config["HAPPINESS_WEIGHT"]);
        return score;
    }

    public override bool execute()
    {
        switch (state)
        {
            case ActionState.INACTIVE:
                agent.LogError(String.Format("This should not happen!"));
                throw new NotImplementedException();

            case ActionState.TRANSITION:
            {
                (double energy, double happiness) = calculateTransitionEffect();
                agent.motivation = energy;
                agent.happiness = happiness;
                return true;
            }

            case ActionState.WAITING:
            {
                (double energy, double happiness) = calculateWaitingEffect();
                agent.motivation = energy;
                agent.happiness = happiness;
                return true;
            }

            case ActionState.EXECUTING:
                agent.motivation = boundValue(0.0, agent.motivation + config["MOTIVATION_INCREASE"], 1.0);
                agent.happiness = boundValue(0.0, agent.happiness + config["HAPPINESS_INCREASE"], 1.0);

                agent.navagent.destination = destination;
                return true;
        }
        return false;
    }


    private bool freeTableAvailable()
    {
        foreach (Table table in agent.classroom.groupTables)
        {
            if (table.freeSpot())
            {
                return true;
            }
        }
        return false;
    }

    // Find a group Table, prefare tables with other agents
    private (Table, Transform) getTable()
    {

        (Table table, Transform seat)  = _getTable(true);
        if (table)
            return (table, seat);
        return _getTable(false);
    }

    // Find a grouop Table
    private (Table, Transform) _getTable(bool hasAgents)
    {

        List<int> indices = GetPermutedIndices(agent.classroom.groupTables.Length);
        foreach (int idx in indices)
        {
            Table table = agent.classroom.groupTables[idx];

            if (hasAgents && table.nAgents() == 0)
                continue;

            Transform seat = table.takeSeat(agent);
            if (seat != null)
            {
                agent.LogDebug(String.Format("Agent takes seat on table {0}", table));
                lastTable = table;
                return (table, seat);
            }
        }
        return (null, null);
    }

    public override void end()
    {

        switch (state)
        {
            case ActionState.INACTIVE:
                agent.LogError(String.Format("This should not happen!"));
                throw new NotImplementedException();

            case ActionState.TRANSITION:
                agent.LogDebug(String.Format("Stopping before reaching the Table!"));
                break;

            case ActionState.WAITING:
                agent.LogDebug(String.Format("Stopping to wait for a study group at {0}!", lastTable));
                break;

            case ActionState.EXECUTING:
                agent.LogDebug(String.Format("Stop studying at {0}!", lastTable));
                break;
        }
        if (lastTable) {
            lastTable.releaseSeat(agent);
            lastTable = null;
        }

        state = ActionState.INACTIVE;
        retry_cnter = 0;
    }

    public override string ToString()
    {
        switch (state)
        {
            case ActionState.INACTIVE:
                return String.Format($"{name}({state})");
            case ActionState.TRANSITION:
                return String.Format($"{name}({state}) walking towards {lastTable}");
            case ActionState.WAITING:
                return String.Format($"{name}({state}) waiting at {lastTable} to study with someone for {retry_cnter} turns");
            case ActionState.EXECUTING:
                return String.Format($"{name}({state}) studying at {lastTable} with {lastTable.nAgents() - 1} others");
        }
        return "Invalid State!";
    }
}
