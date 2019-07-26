using System;
using UnityEngine;
using System.Collections.Generic;

public class StudyAlone : AgentBehavior
{

    private Table lastTable;
    private Vector3 destination;

    public StudyAlone(Agent agent) : base(agent, AgentBehavior.Actions.StudyAlone, "StudyAlone", agent.SC.StudyAlone) { }
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
                    state = ActionState.EXECUTING;
                    return true;
                }
                else
                {
                    agent.LogDebug("No free single table to study!");
                    return false;
                }


            case ActionState.WAITING:
            case ActionState.EXECUTING:
                if (agent.classroom.noise >= agent.personality.conscientousness * config["NOISE_THRESHOLD"])
                {
                    agent.LogDebug(String.Format($"Its too loud! Cannot learn! {agent.classroom.noise} > {agent.personality.conscientousness * config["NOISE_THRESHOLD"]}"));
                    return false;
                }
                return true;
        }
        return false;
    }

    public override double rate()
    {
        double score = CalculateScore(1.0 - agent.personality.extraversion, config["PERSONALITY_WEIGHT"], ExpGrowth(agent.motivation), config["MOTIVATION_WEIGHT"], ExpGrowth(agent.happiness), config["HAPPINESS_WEIGHT"]);
        return score;
    }

    public override bool execute()
    {

        switch (state)
        {
            case ActionState.INACTIVE:
                agent.LogError(String.Format("This should not happen!"));
                throw new NotImplementedException();

            case ActionState.WAITING:
                agent.LogError(String.Format("This should not happen!"));
                throw new NotImplementedException();

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
        foreach (Table table in agent.classroom.individualTables)
        {
            if (table.freeSpot())
            {
                return true;
            }
        }
        return false;
    }

    // Find a free Table and 
    private (Table, Transform) getTable()
    {
        List<int> indices = GetPermutedIndices(agent.classroom.individualTables.Length);
        foreach (int idx in indices)
        {
            Table table = agent.classroom.individualTables[idx];
            Transform seat = table.takeSeat(agent);
            if (seat != null)
            {
                //Debug.Log(String.Format("Getting table {0}", idx));
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
            case ActionState.WAITING:
                agent.LogError(String.Format("This should not happen!"));
                throw new NotImplementedException();

            case ActionState.EXECUTING:
                agent.LogDebug(String.Format("Ending study alone on table {0}!", lastTable));
                lastTable.releaseSeat(agent);
                lastTable = null;
                break;
        }
        state = ActionState.INACTIVE;
    }

    public override string ToString()
    {
        switch (state)
        {
            case ActionState.INACTIVE:
            case ActionState.WAITING:
                return String.Format("{0}({1})", name, state);
            case ActionState.EXECUTING:
                return String.Format("{0}({1}) study at {2}", name, state, lastTable);
        }
        return "Invalid State!";
    }
}
