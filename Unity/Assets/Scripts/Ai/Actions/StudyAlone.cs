using System;
using UnityEngine;
using System.Collections.Generic;

public class StudyAlone : AgentBehavior
{

    private const double NOISE_INC = 0.05;
    private const double MOTIVATION_INCREASE = -0.05;
    private const double HAPPINESS_INCREASE = 0.00;
    private const double NOISE_SCALE = 2.0;

    private const double MOTIVATION_THRESHOLD = 0.5; // As of when an Agent will start Learning
    private const double EXTRAVERSION_WEIGHT = 0.5;

    private Table lastTable;
    private Vector3 destination;

    public StudyAlone(Agent agent) : base(agent, AgentBehavior.Actions.StudyAlone, "StudyAlone", NOISE_INC) { }
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
                if (agent.classroom.noise >= agent.personality.conscientousness * NOISE_SCALE)
                {
                    agent.LogDebug(String.Format($"Its too loud! Cannot learn! {agent.classroom.noise} > {agent.personality.conscientousness * NOISE_SCALE}"));
                    return false;
                }
                return true;
        }
        return false;
    }

    public override double rate()
    {
        /*
        // The score is defined by the vale of extraversion and the energy of the agent
        // Low values of extraversion and low values of energy increase the score (make this action more likely)

        // Agents low on extraversion prefare break (over chat)
        double extra = (1.0 - agent.personality.extraversion);
        double motivation = ExpGrowth(agent.motivation);
        //double score = boundValue(0.0, (extra * EXTRAVERSION_WEIGHT) + (motivation * (1.0 - EXTRAVERSION_WEIGHT)), 1.0);
        double combined = (extra * EXTRAVERSION_WEIGHT) + (motivation * (1.0 - EXTRAVERSION_WEIGHT));
        double happiness_adjusted = combined * ExpGrowth(agent.happiness);
        double score = boundValue(0.0, happiness_adjusted, 1.0);
        return score;
        */

        /*
        double PERSONALITY_WEIGHT = 0.33;
        double MOTIVATION_WEIGHT = 0.33;
        double HAPPINESS_WEIGHT = 0.33;
        double personality = 1.0 - agent.personality.extraversion;
        double motivation = ExpGrowth(agent.motivation);
        double happiness = ExpGrowth(agent.happiness);
        double wheighted = (personality * PERSONALITY_WEIGHT) + (motivation * MOTIVATION_WEIGHT) + (happiness * HAPPINESS_WEIGHT);

        double score = boundValue(0.0, wheighted, 1.0);
        */
        double score = CalculateScore(1.0 - agent.personality.extraversion, 0.5, ExpGrowth(agent.motivation), 0.25, ExpGrowth(agent.happiness), 0.25);
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
                agent.motivation = boundValue(0.0, agent.motivation + MOTIVATION_INCREASE, 1.0);
                agent.happiness = boundValue(0.0, agent.happiness + HAPPINESS_INCREASE, 1.0);
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
