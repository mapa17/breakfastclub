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

    private const int MISSING_PARTNER_COST = -30;

    public Chat(Agent agent) : base(agent, AgentBehavior.Actions.Chat, "Chat", NOISE_INC) { }

    private Agent otherAgent;
    bool match = false;

    // An Agent can chat if there is another Agent disponible
    public override bool possible()
    {
        return true;
    }

    /*
    • requirements: free spot on individual table
    • effect: regenerate energy, will increase happiness(amount is a function of extraversion)
    */
    public override int evaluate()
    {
        // The score is defined by the vale of extraversion and the energy of the agent
        // High values of extraversion and low values of energy increase the score (make this action more likely)

        // Agents low on extraversion prefare break (over chat)
        float extra = agent.personality.extraversion;
        float energy = boundValue(0.0f, 1.0f + ENERGY_BIAS - agent.energy, 1.0f);
        float t = (extra * EXTRAVERSION_WEIGHT) + (energy * (1.0f - EXTRAVERSION_WEIGHT));

        int score = (int)(boundValue(0.0f, t, 1.0f) * SCORE_SCALE);

        if (otherAgent && !(otherAgent.currentAction is Chat))
        {
            score += MISSING_PARTNER_COST;
        }

        return score;
    }

    public override bool execute()
    {
        // Check if we have someone to chat with
        if (otherAgent)
        {
            if (otherAgent.currentAction is Chat)
            {
                agent.energy = boundValue(0.0f, agent.energy + ENERGY_INCREASE, 1.0f);
                agent.happiness = boundValue(-1.0f, agent.happiness + HAPPINESS_INCREASE, 1.0f);
                agent.navagent.destination = otherAgent.transform.position;
                match = true;
                return true;
            }
            else
            {
                // One has to distinguish between the other agent leaving the chat and the other agent not yet involved
                if (match)
                {
                    // The other left; Execution will return false
                    agent.logInfo(String.Format("Other agent {0} has left the chat ...", otherAgent));
                }
                else
                {
                    // This is a new 'target'
                    // Keep requesting interaction
                    otherAgent.interact(agent, this);
                    agent.logInfo(String.Format("Trying again to chat with {0}", otherAgent));
                }
            }
        }
        else
        {
            agent.logInfo(String.Format("Will try to find someone to chat with!"));
            engageOtherAgent();
            agent.navagent.destination = otherAgent.transform.position;
        }
        return false;
    }

    // Find another agent to chat with
    private bool engageOtherAgent()
    {
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
