using System;

public class Chat : AgentBehavior
{
    private int retry_cnter;

    private Agent otherAgent;

    public Chat(Agent agent) : base(agent, AgentBehavior.Actions.Chat, "Chat", agent.SC.Chat) { }

    // An Agent can chat if there is another Agent disponible
    public override bool possible()
    {

        switch (state)
        {
            // Start to engage another agent
            case ActionState.INACTIVE:
                if (engageOtherAgent())
                {
                    state = ActionState.TRANSITION;
                    transition_cnter = 2;
                    return true;
                }
                return false;

            case ActionState.TRANSITION:

                transition_cnter--;
                if (transition_cnter > 0)
                {

                }
                else
                {
                    state = ActionState.WAITING;
                }
                return true;

                agent.navagent.destination = otherAgent.transform.position;
                if (IsCloseTo(otherAgent))
                {
                    state = ActionState.WAITING;
                }
                return true;

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
                    if (retry_cnter >= (int)config["RETRY_THRESHOLD"])
                    {
                        agent.LogDebug(String.Format("Giving up to try to chat with {0}. Will try another agent ...", otherAgent));
                        //engageOtherAgent();
                        state = ActionState.INACTIVE;
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

                }
                else
                {
                    // The other left; Execution will return false
                    agent.LogDebug(String.Format("Other agent {0} has left the chat ...", otherAgent));
                    otherAgent = null;
                    state = ActionState.INACTIVE;
                }
                return true;
        }

        return false;
    }

    // High values of extroversion and low values of energy increase the score
    public override double rate()
    {
        //double score = CalculateScore(agent.personality.extraversion, 0.5, ExpDecay(agent.motivation), 0.25, ExpGrowth(agent.happiness), 0.25);
        double score = CalculateScore(agent.personality.extraversion, config["PERSONALITY_WEIGHT"], ExpDecay(agent.motivation), config["MOTIVATION_WEIGHT"], ExpGrowth(agent.happiness), config["HAPPINESS_WEIGHT"]);
        return score;
    }

    public override bool execute()
    {
        switch (state)
        {
            case ActionState.INACTIVE:
                if (engageOtherAgent())
                    state = ActionState.TRANSITION;
                return true;

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

            // Dont try to chat with agents that are quarreling
            if (otherAgent.currentAction is Quarrel)
                continue;
        } while (otherAgent == agent);

        agent.LogDebug(String.Format("Agent tries to chat with agent {0}!", otherAgent));
        if (otherAgent.currentAction is Chat)
        {
            agent.LogDebug(String.Format($"{otherAgent} is already chatting, join chat!"));
        }
        else
        {
            otherAgent.Interact(agent, this);
        }
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

            case ActionState.TRANSITION:
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
        state = ActionState.TRANSITION;
    }

    public override string ToString()
    {
        switch (state)
        {
            case ActionState.INACTIVE:
                return String.Format($"{name}({state})");
            case ActionState.TRANSITION:
                return String.Format($"{name}({state}) walking to {otherAgent.studentname}");
            case ActionState.WAITING:
                return String.Format($"{name}({state}) waiting for {otherAgent.studentname} retrying {retry_cnter}");
            case ActionState.EXECUTING:
                return String.Format($"{name}({state}) with {otherAgent.studentname}");
        }
        return "Invalid State!";
    }
}
