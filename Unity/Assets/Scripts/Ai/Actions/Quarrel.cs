using System;

public class Quarrel : AgentBehavior
{
    private int retry_cnter;

    private Agent otherAgent;

    public Quarrel(Agent agent) : base(agent, AgentBehavior.Actions.Quarrel, "Quarrel", agent.SC.Quarrel) { }  

    public override bool possible()
    {
    
        switch(state)
        {
            // Start to engage another agent
            case ActionState.INACTIVE:
                if (engageOtherAgent())
                {
                    state = ActionState.TRANSITION;
                    return true;
                }
                return false;

            case ActionState.TRANSITION:
                agent.navagent.destination = otherAgent.transform.position;
                if (IsCloseTo(otherAgent))
                {
                    state = ActionState.WAITING;
                }
                return true;

            // Either Change to active if the other agent is responing, or try to interact again
            // If we tried long enough, change to another target.
            case ActionState.WAITING:
                if ((otherAgent.Desire is Quarrel) || (otherAgent.currentAction is Quarrel))
                {
                    state = ActionState.EXECUTING;
                }
                else
                {
                    // We have someone we want to quarrel with but they have not responded 'yet', so try to convince them
                    if (retry_cnter >= (int)config["RETRY_THRESHOLD"])
                    {
                        agent.LogDebug(String.Format("Giving up to quarel with {0}. Will try another agent ...", otherAgent));
                        //engageOtherAgent();
                        state = ActionState.INACTIVE;
                    }
                    else
                    {
                        retry_cnter++;
                        otherAgent.Interact(agent, this);
                        agent.navagent.destination = otherAgent.transform.position;
                        agent.LogDebug(String.Format("Trying again {0} to quarrel with {1}", retry_cnter, otherAgent));
                    }
                }
                return true;
            case ActionState.EXECUTING:
                if ((otherAgent.Desire is Quarrel) || (otherAgent.currentAction is Quarrel))
                {
                    return true;
                } else {
                    // The other left; Execution will return false
                    agent.LogDebug(String.Format("Other agent {0} has left the quarrel ...", otherAgent));
                    otherAgent = null;
                    state = ActionState.INACTIVE;
                }
                return false;
        }

        return false;
    }

    public override double rate()
    {
        double score = CalculateScore(agent.personality.agreeableness, config["PERSONALITY_WEIGHT"], ExpGrowth(agent.motivation), config["MOTIVATION_WEIGHT"], ExpDecay(agent.happiness, power: 4), config["HAPPINESS_WEIGHT"]);
        return score;
    }

    public override bool execute()
    {
        switch (state)
        {
            case ActionState.INACTIVE:
            {
                agent.LogError(String.Format("Trying to find someone to quarrel with!"));
                (double energy, double happiness) = calculateWaitingEffect();
                agent.motivation = energy;
                agent.happiness = happiness;
                agent.navagent.destination = otherAgent.transform.position;
                return true;
            }

            case ActionState.TRANSITION:
            {
                (double energy, double happiness) = calculateTransitionEffect();
                agent.motivation = energy;
                agent.happiness = happiness;
                agent.navagent.destination = otherAgent.transform.position;
                return true;
            }

            case ActionState.WAITING:
            {
                (double energy, double happiness) = calculateWaitingEffect();
                agent.motivation = energy;
                agent.happiness = happiness;
                agent.navagent.destination = otherAgent.transform.position;
                return true;
            }

            case ActionState.EXECUTING:
                agent.LogDebug($"Continue to quarrel with {otherAgent} ...");
                agent.motivation = boundValue(0.0, agent.motivation + config["MOTIVATION_INCREASE"], 1.0);
                agent.happiness = boundValue(0.0, agent.happiness + config["HAPPINESS_INCREASE"], 1.0);
                agent.navagent.destination = otherAgent.transform.position;
                return true;
        }
        return false;
    }

    public override void end()
    {
        switch (state)
        {
            case ActionState.INACTIVE:
                // It can happen if the other one left the quarrel, and than we end quarrel
                //agent.LogError(String.Format("This should not happen!"));
                //throw new NotImplementedException();
                break;

            case ActionState.TRANSITION:
                // It can happen if the other one left the chat, and than we end chat
                break;

            case ActionState.WAITING:
                agent.LogDebug(String.Format("Giving up to wait for {0}!", otherAgent));
                break;

            case ActionState.EXECUTING:
                agent.LogDebug(String.Format("Ending Quarrel with {0}!", otherAgent));
                otherAgent = null;

                // Give the agent an happiness boost in order to not start quarrel again imediately
                agent.happiness += config["HAPPINESS_BOOST"];
                break;
        }
        retry_cnter = 0;
        state = ActionState.INACTIVE;
    }

    // Find another agent to chat with
    private bool engageOtherAgent()
    {
        // Reset retry counter for all conditions
        retry_cnter = 0;

        if (agent.classroom.agents.Length == 1)
        {
            agent.LogDebug(String.Format("No other Agent to quarrel with!"));
            return false;
        }

        // Select a random other agent
        int idx;
        do
        {
            idx = agent.random.Next(agent.classroom.agents.Length);
            otherAgent = agent.classroom.agents[idx];
        } while (otherAgent == agent);

        agent.LogDebug(String.Format("Agent tries to quarrel with agent {0}!", otherAgent));
        otherAgent.Interact(agent, this);
        agent.navagent.destination = otherAgent.transform.position;

        return true;
    }

    public void acceptInviation(Agent otherAgent)
    {
        agent.LogDebug(String.Format("{0} is accepting invitation to quarrel with {1}!", agent, otherAgent));
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
