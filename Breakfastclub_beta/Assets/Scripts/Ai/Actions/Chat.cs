using System;

public class Chat : AgentBehavior
{
    /*
    • requirements: free spot at shared table, other student at the table
    • effect: regenerate energy, increase noise, will increase happiness(amount is a function of extraversion)
    */

    private const float NOISE_INC = 0.1f;
    private const float HAPPINESS_INCREASE = 0.05f;
    private const float ENERGY_INCREASE = 0.05f;

    private const float ENERGY_BIAS = -0.4f; // Negative Values incourage work, positive to take a break
    private const float SCORE_SCALE = 100.0f;
    private const float EXTRAVERSION_WEIGHT = 0.3f;

    //private const int MISSING_PARTNER_COST = -30;

    private const int RETRY_THRESHOLD = 3;
    private int retry_cnter;

    private Agent otherAgent;
    bool match = false;

    public Chat(Agent agent) : base(agent, AgentBehavior.Actions.Chat, "Chat", NOISE_INC) { }

    // An Agent can chat if there is another Agent disponible
    public override bool possible()
    {
        if (otherAgent)
        {
            if(otherAgent.Desire is Chat)
            {
                return true;
            }
            else
            {
                // One has to distinguish between the other agent leaving the chat and the other agent not yet involved
                if (match)
                {
                    // The other left; Execution will return false
                    agent.logInfo(String.Format("Other agent {0} has left the chat ...", otherAgent));
                    otherAgent = null;
                    match = false;
                }
                else
                {
                    // We have someone we want to quarrel with but they have not responded 'yet', so try to convince them
                    if (retry_cnter >= RETRY_THRESHOLD)
                    {
                        agent.logInfo(String.Format("Giving up to chat with {0}. Will try another agent ...", otherAgent));
                        engageOtherAgent();
                    }
                    else
                    {
                        // We have someone we want to talk to but they have not responded 'yet', so try to convince them
                        retry_cnter++;
                        otherAgent.interact(agent, this);
                        agent.navagent.destination = otherAgent.transform.position;
                        agent.logInfo(String.Format("Trying again to chat with {0}", otherAgent));
                    }
                }

            }
        }
        else
        {
            // Try to find someone to talk to
            bool success = engageOtherAgent();
        }
        return false;
    }

    /*
    • requirements: free spot on individual table
    • effect: regenerate energy, will increase happiness(amount is a function of extraversion)
    */
    public override int rate()
    {
        // The score is defined by the vale of extraversion and the energy of the agent
        // High values of extraversion and low values of energy increase the score (make this action more likely)

        // Agents low on extraversion prefare break (over chat)
        float extra = agent.personality.extraversion;
        float energy = boundValue(0.0f, 1.0f + ENERGY_BIAS - agent.energy, 1.0f);
        float t = (extra * EXTRAVERSION_WEIGHT) + (energy * (1.0f - EXTRAVERSION_WEIGHT));

        int score = (int)(boundValue(0.0f, t, 1.0f) * SCORE_SCALE);

        /*
        if (otherAgent && !(otherAgent.currentAction is Chat))
        {
            score += MISSING_PARTNER_COST;
        }*/

        return score;
    }

    public override bool execute()
    {
        // Check if we have someone to chat with
        if (otherAgent && otherAgent.Desire is Chat)
        {
            agent.energy = boundValue(0.0f, agent.energy + ENERGY_INCREASE, 1.0f);
            agent.happiness = boundValue(-1.0f, agent.happiness + HAPPINESS_INCREASE, 1.0f);
            agent.navagent.destination = otherAgent.transform.position;
            match = true;
            return true;
        }

        //throw new NotSupportedException("This should not happen!");
        agent.logError(String.Format("This should not happen!"));
        return false;
    }

    // Find another agent to chat with
    private bool engageOtherAgent()
    {
        // Reset retry counter for all conditions
        retry_cnter = 0;

        if (agent.classroom.agents.Length == 1)
        {
            agent.logInfo(String.Format("No other Agent to chat with!"));
            return false;
        }

        // Select a random other agent
        int idx;
        do
        {
            idx = agent.random.Next(agent.classroom.agents.Length);
            otherAgent = agent.classroom.agents[idx];
        } while (otherAgent == agent);

        agent.logInfo(String.Format("Agent tries to chat with agent {0}!", otherAgent));
        otherAgent.interact(agent, this);
        agent.navagent.destination = otherAgent.transform.position;
        match = false;
        return true;
    }

    public override void end()
    {
        agent.logInfo(String.Format("Stop chatting with {0}!", otherAgent));
        otherAgent = null;
        match = false;
    }

    public void acceptInviation(Agent otherAgent)
    {
        agent.logInfo(String.Format("Accepting invitation to chat with {0}!", otherAgent));
        this.otherAgent = otherAgent;
        match = true;
    }
}
