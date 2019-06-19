using System;
using UnityEngine;
using System.Collections.Generic;

public class StudyAlone : AgentBehavior
{

    private const float NOISE_INC = 0.0f;
    private const float HAPPINESS_INCREASE = 0.00f;
    private const float ENERGY_INCREASE = -0.02f;
    private const float NOISE_SCALE = 1.0f;

    private const float ENERGY_THRESHOLD = 0.5f; // As of when an Agent will start Learning
    private const float SCORE_SCALE = 100.0f;
    private const float EXTRAVERSION_WEIGHT = 0.3f;

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
                return false;

            case ActionState.WAITING:
            case ActionState.EXECUTING:
                if (agent.classroom.noise >= agent.personality.conscientousness * NOISE_SCALE)
                    return false;
                return true;
        }
        return false;
    }

    public override int rate()
    {
        // The score is defined by the vale of extraversion and the energy of the agent
        // Low values of extraversion and low values of energy increase the score (make this action more likely)

        // Agents low on extraversion prefare break (over chat)
        float extra = (1.0f - agent.personality.extraversion);
        float energy = boundValue(0.0f, agent.energy - ENERGY_THRESHOLD, 1.0f);
        float t = (extra * EXTRAVERSION_WEIGHT) + (energy * (1.0f - EXTRAVERSION_WEIGHT));

        int score = (int)(boundValue(0.0f, t, 1.0f) * SCORE_SCALE);
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
                agent.energy = boundValue(0.0f, agent.energy + ENERGY_INCREASE, 1.0f);
                agent.happiness = boundValue(-1.0f, agent.happiness + HAPPINESS_INCREASE, 1.0f);
                agent.navagent.destination = destination;

                return true;
        }
        return false;
    }

    private bool freeTableAvailable()
    {
        foreach (Table table in agent.classroom.individualTables)
        {
            if (table.freeSpot()){
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
            if(seat != null)
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
                return String.Format("{0} ({1})", name, state);
            case ActionState.EXECUTING:
                return String.Format("{0} ({1}) study at {2}", name, state, lastTable);
        }
        return "Invalid State!";
    }
}
