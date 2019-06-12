using System;

public class Quarrel : AgentBehavior
{
    private const float NOISE_INC = 0.2f;
    private const float ENERGY_BIAS = 0.5f;
    private const float ENERGY_THRESHOLD = 0.2f;
    private const float HAPPINESS_BIAS = 0.0f; // If bias = 0.0f, -happiness * SCALE
    private const float HAPPINESS_WEIGHT = 0.7f;
    private const float SCORE_SCALE = 100.0f;

    private const float HAPPINESS_DECREASE = 0.1f;
    private const float ENERGY_DECREASE = 0.05f;

    private const int RETRY_THRESHOLD = 3;
    private int retry_cnter;

    private Agent otherAgent;
    private bool match;

    public Quarrel(Agent agent) : base(agent, AgentBehavior.Actions.Quarrel, "Quarrel", NOISE_INC) { }
    /*
    • requirements: low happiness, presence of another agent, enough energy
    • effect: reduce energy a lot at every turn, increase noise a lot, reduce happiness a lot at every turn
    */

    public override bool possible()
    {
        if (agent.energy >= ENERGY_THRESHOLD)
        {
            if(otherAgent)
            {
                if ((otherAgent.Desire is Quarrel) || (otherAgent.currentAction is Quarrel))
                    return true;
                else
                {
                    if (match)
                    {
                        // The other left; Execution will return false
                        agent.logInfo(String.Format("Other agent {0} has left the quarrel ...", otherAgent));
                        otherAgent = null;
                        match = false;
                    }
                    else
                    {
                        // We have someone we want to quarrel with but they have not responded 'yet', so try to convince them
                        if (retry_cnter >= RETRY_THRESHOLD)
                        {
                            agent.logInfo(String.Format("Giving up to quarel with {0}. Will try another agent ...", otherAgent));
                            engageOtherAgent();
                        }
                        else
                        {
                            retry_cnter++;
                            otherAgent.interact(agent, this);
                            agent.navagent.destination = otherAgent.transform.position;
                            agent.logInfo(String.Format("Trying again {0} to quarrel with {1}", retry_cnter, otherAgent));
                        }
                    }
                }
            }
            else
            {
                engageOtherAgent();
            }
        }

        return false;
    }

    public override int rate()
    {
        float happiness = (boundValue(-1.0f,  (-1.0f*agent.happiness) + HAPPINESS_BIAS, 1.0f));

        if (agent.energy >= ENERGY_THRESHOLD) {
            // Low energy can only reduce score, never boost it because of too high energy level
            float energy = boundValue(-1.0f, agent.energy - ENERGY_BIAS, 0.0f);
            float score = (happiness * HAPPINESS_WEIGHT) + (energy * (1.0f - HAPPINESS_WEIGHT));
            return (int)(score * SCORE_SCALE);
        }
        else
            return -1000;
    }

    public override bool execute()
    {
        agent.happiness = Math.Max(-1.0f, agent.happiness - HAPPINESS_DECREASE);
        agent.energy = Math.Max(0.0f, agent.energy - ENERGY_DECREASE);

        agent.navagent.destination = otherAgent.transform.position;
        match = true;

        return true;
    }

    public override void end()
    {
        agent.logInfo(String.Format("Stop quarrel with {0}!", otherAgent));
        otherAgent = null;
        match = false;
    }

    // Find another agent to chat with
    private bool engageOtherAgent()
    {
        // Reset retry counter for all conditions
        retry_cnter = 0;

        if (agent.classroom.agents.Length == 1)
        {
            agent.logInfo(String.Format("No other Agent to quarrel with!"));
            return false;
        }

        // Select a random other agent
        int idx;
        do
        {
            idx = agent.random.Next(agent.classroom.agents.Length);
            otherAgent = agent.classroom.agents[idx];
        } while (otherAgent == agent);

        agent.logInfo(String.Format("Agent tries to quarrel with agent {0}!", otherAgent));
        otherAgent.interact(agent, this);
        agent.navagent.destination = otherAgent.transform.position;
        match = false;
        return true;
    }

    public void acceptInviation(Agent otherAgent)
    {
        agent.logInfo(String.Format("{0} is accepting invitation to quarrel with {1}!", agent, otherAgent));
        this.otherAgent = otherAgent;
        match = true;
    }
}
