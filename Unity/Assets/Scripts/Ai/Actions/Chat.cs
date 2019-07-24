using System;

public class Chat : AgentBehavior
{
    /*
    • requirements: free spot at shared table, other student at the table
    • effect: regenerate energy, increase noise, will increase happiness(amount is a function of extraversion)
    */

    private const double NOISE_INC = 0.1;
    private const double HAPPINESS_INCREASE = 0.00;
    private const double MOTIVATION_INCREASE = 0.05;

    private const double MOTIVATION_BIAS = -0.4; // Negative Values incourage work, positive to take a break
    //private const double MOTIVATION_BIAS = -0.0; // Negative Values incourage work, positive to take a break
    private const double EXTRAVERSION_WEIGHT = 0.3;

    //private const int MISSING_PARTNER_COST = -30;

    private const int RETRY_THRESHOLD = 3;
    private int retry_cnter;

    private Agent otherAgent;

    public Chat(Agent agent) : base(agent, AgentBehavior.Actions.Chat, "Chat", NOISE_INC) { }

    // An Agent can chat if there is another Agent disponible
    public override bool possible()
    {

        switch (state)
        {
            // Start to engage another agent
            case ActionState.INACTIVE:
                if (engageOtherAgent())
                {
                    state = ActionState.WAITING;
                    return true;
                }
                return false;

            // Either Change to active if the other agent is responing, or try to interact again
            // If we tried long enough, change to another target.
            case ActionState.WAITING:
                if ((otherAgent.Desire is Chat) || (otherAgent.currentAction is Chat))
                {
                    agent.LogDebug(String.Format("Agent {0} is ready to chat, lets go ...", otherAgent));
                    state = ActionState.EXECUTING;
                }
                else
                {
                    // We have someone we want to quarrel with but they have not responded 'yet', so try to convince them
                    if (retry_cnter >= RETRY_THRESHOLD)
                    {
                        agent.LogDebug(String.Format("Giving up to try to chat with {0}. Will try another agent ...", otherAgent));
                        engageOtherAgent();
                    }
                    else
                    {
                        retry_cnter++;
                        otherAgent.Interact(agent, this);
                        agent.navagent.destination = otherAgent.transform.position;
                        agent.LogDebug(String.Format("Trying again {0} to chat with {1}", retry_cnter, otherAgent));
                    }
                }
                return true;
            case ActionState.EXECUTING:
                if ((otherAgent.Desire is Chat) || (otherAgent.currentAction is Chat))
                {
                    agent.LogDebug(String.Format("Still chatting with {0} ...", otherAgent));
                    return true;
                }
                else
                {
                    // The other left; Execution will return false
                    agent.LogDebug(String.Format("Other agent {0} has left the chat ...", otherAgent));
                    otherAgent = null;
                    state = ActionState.INACTIVE;
                }
                return false;
        }

        return false;
    }

    // High values of extroversion and low values of energy increase the score
    public override double rate()
    {
        /*
        double extra = agent.personality.extraversion;
        double motivation = ExpDecay(agent.motivation);
        double combined = (extra * EXTRAVERSION_WEIGHT) + (motivation * (1.0 - EXTRAVERSION_WEIGHT));
        double happiness_adjusted = combined * ExpGrowth(agent.happiness);
        double score = boundValue(0.0, happiness_adjusted, 1.0);
        */

        /*
        double PERSONALITY_WEIGHT = 0.33;
        double MOTIVATION_WEIGHT = 0.33;
        double HAPPINESS_WEIGHT = 0.33;
        double personality = agent.personality.extraversion;
        double motivation = ExpDecay(agent.motivation);
        double happiness = ExpGrowth(agent.happiness);
        double wheighted = (personality * PERSONALITY_WEIGHT) + (motivation * MOTIVATION_WEIGHT) + (happiness * HAPPINESS_WEIGHT);

        double score = boundValue(0.0, wheighted, 1.0);
        */
        double score = CalculateScore(agent.personality.extraversion, 0.33, ExpDecay(agent.motivation), 0.33, ExpGrowth(agent.happiness), 0.33);
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
                (double energy, double happiness) = calculateWaitingEffect();
                agent.motivation = energy;
                agent.happiness = happiness;
                return true;

            case ActionState.EXECUTING:
                agent.motivation = boundValue(0.0, agent.motivation + MOTIVATION_INCREASE, 1.0);
                agent.happiness = boundValue(0.0, agent.happiness + HAPPINESS_INCREASE, 1.0);
                agent.navagent.destination = otherAgent.transform.position;
                return true;
        }
        return false;
    }

    // Find another agent to chat with
    private bool engageOtherAgent()
    {
        // Reset retry counter for all conditions
        retry_cnter = 0;

        if (agent.classroom.agents.Length == 1)
        {
            agent.LogDebug(String.Format("No other Agent to chat with!"));
            return false;
        }

        // Select a random other agent
        int idx;
        do
        {
            idx = agent.random.Next(agent.classroom.agents.Length);
            otherAgent = agent.classroom.agents[idx];
        } while (otherAgent == agent);

        agent.LogDebug(String.Format("Agent tries to chat with agent {0}!", otherAgent));
        otherAgent.Interact(agent, this);
        agent.navagent.destination = otherAgent.transform.position;

        return true;
    }

    public override void end()
    {
        switch (state)
        {
            case ActionState.INACTIVE:
                // It can happen if the other one left the chat, and than we end chat
                break;

            case ActionState.WAITING:
                agent.LogDebug($"Giving up to wait for {otherAgent}!");
                break;

            case ActionState.EXECUTING:
                agent.LogDebug($"Ending Chatting with {otherAgent}!");
                break;
        }
        retry_cnter = 0;
        otherAgent = null;
        state = ActionState.INACTIVE;
    }

    public void acceptInviation(Agent otherAgent)
    {
        agent.LogDebug(String.Format("{0} is accepting invitation to chat with {1}!", agent, otherAgent));
        this.otherAgent = otherAgent;
        state = ActionState.EXECUTING;
    }

    public override string ToString()
    {
        switch (state)
        {
            case ActionState.INACTIVE:
                return String.Format("{0}({1})", name, state);
            case ActionState.WAITING:
                return String.Format("{0}({1}) waiting for {2} retrying {3}", name, state, otherAgent.studentname, retry_cnter);
            case ActionState.EXECUTING:
                return String.Format("{0}({1}) with {2}", name, state, otherAgent.studentname);
        }
        return "Invalid State!";
    }
}
