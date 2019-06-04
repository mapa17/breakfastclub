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

    private const int MISSING_GROUP_COST = -30;
    private Table lastTable;

    public StudyGroup() : base(AgentBehavior.Actions.StudyGroup, "StudyGroup", NOISE_INC) { }
    /*
     *  • requirements: no quarrel, free individual table, attention
     *  • effects: learning, reduces energy every turn
    */
    public override bool possible(Agent agent)
    {
        // Is the agent already part of a group?
        if(!(agent.currentAction is StudyGroup))
        {
            if (!freeTableAvailable(agent))
            {
                agent.logInfo("No free shared table!");
                return false;
            }
        }

        if (agent.classroom.noise >= agent.personality.conscientousness * NOISE_SCALE) {
            agent.logInfo(String.Format("Cant learn its too noisy {0} > {1}", agent.classroom.noise, agent.personality.conscientousness * NOISE_SCALE));
            return false;
        }
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

        // Studyig alone reduces score!
        if (lastTable && (lastTable.nAgents() <= 1))
        {
            score += MISSING_GROUP_COST;
        }
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

    // Find a group Table, prefare tables with other agents
    private Transform getTable(Agent agent)
    {
        Transform seat = null;
        seat = _getTable(agent, true);
        if (seat)
            return seat;
        return _getTable(agent, false);
    }

    // Find a grouop Table
    private Transform _getTable(Agent agent, bool hasAgents)
    {
        foreach (Table table in agent.classroom.groupTables)
        {
            if (hasAgents && table.nAgents() == 0)
                continue;

            Transform seat = table.takeSeat(agent);
            if (seat != null)
            {
                agent.logInfo(String.Format("Agent takes seat on table {0}", table));
                lastTable = table;
                return seat;
            }
        }
        return null;
    }
}
