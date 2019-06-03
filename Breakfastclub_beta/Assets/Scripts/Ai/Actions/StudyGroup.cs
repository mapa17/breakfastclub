using System;
using UnityEngine;

public class StudyGroup : AgentBehavior
{

    /*
    • requirements: no quarrel, free spot at shared table, another learning students at the table, attention
    • effects: learning in group, reduces energy every turn, increase noise slightly,
    */
    private const float NOISE_INC = 0.0f;
    private const float HAPPINESS_INCREASE = 0.00f;
    private const float ENERGY_INCREASE = -0.05f;
    private const float NOISE_SCALE = 2.0f;

    private const float ENERGY_THRESHOLD = 0.5f; // As of when an Agent will start Learning
    private const float SCORE_SCALE = 100.0f;
    private const float EXTRAVERSION_WEIGHT = 0.3f;

    public StudyGroup() : base(AgentBehavior.Actions.StudyGroup, "StudyGroup", NOISE_INC) { }
    /*
     *  • requirements: no quarrel, free individual table, attention
     *  • effects: learning, reduces energy every turn
    */
    public override bool possible(Agent agent)
    {
        if (agent.classroom.noise >= agent.personality.conscientousness * NOISE_SCALE)
            return false;
        return (true);
    }

    public override int evaluate(Agent agent)
    {
        // The score is defined by the vale of extraversion and the energy of the agent
        // Low values of extraversion and low values of energy increase the score (make this action more likely)

        // Agents low on extraversion prefare break (over chat)
        float extra = agent.personality.extraversion;
        float energy = boundValue(0.0f, agent.energy - ENERGY_THRESHOLD, 1.0f);
        float t = (extra * EXTRAVERSION_WEIGHT) + (energy * (1.0f - EXTRAVERSION_WEIGHT));

        int score = (int)(boundValue(0.0f, t, 1.0f) * SCORE_SCALE);
        return score;
    }

    public override bool execute(Agent agent)
    {
        if (agent.currentAction is StudyGroup)
        {
            agent.energy = boundValue(0.0f, agent.energy + ENERGY_INCREASE, 1.0f);
            agent.happiness = boundValue(-1.0f, agent.happiness + HAPPINESS_INCREASE, 1.0f);
            return true;
        }
        else
        {
            // Get a new table
            Transform seat = getTable(agent);
            if (seat != null)
            {
                agent.navagent.destination = seat.position;
                return true;
            }
        }
        return false;
    }


    private bool freeTableAvailable(Agent agent)
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

    // Find a free Table and 
    private Transform getTable(Agent agent)
    {
        foreach (Table table in agent.classroom.groupTables)
        {
            Transform seat = table.takeSeat(agent);
            if (seat != null)
            {
                return seat;
            }
        }
        return null;
    }
}
