using System;

public class Quarrel : AgentBehavior
{
    private int retry_cnter;

    private Agent otherAgent;

    public Quarrel(Agent agent) : base(agent, AgentBehavior.Actions.Quarrel, "Quarrel", agent.SC.Quarrel) { }
    /*
    • requirements: low happiness, presence of another agent, enough energy
    • effect: reduce energy a lot at every turn, increase noise a lot, reduce happiness a lot at every turn
    */

    /*
     * States
     * 
     * Inactive: No other agent, no interaction
     * Waiting: We have a otherAgent, and wait for him to reply; keeping track of the retry_cnt
     * Executing: We have a otherAgent that is sharing the activity with us
     */      

    public override bool possible()
    {
        /*
        if (agent.happiness == 0.0)
        {
            //agent.LogInfo($"Cannot keep up Quarrel, motivationt too low. {agent.motivation} < {MOTIVATION_THRESHOLD} ...");
            agent.LogDebug($"Cannot continue quarrel, happiness is at bottom ...");
            return false;
        }*/

        switch(state)
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
                        engageOtherAgent();
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
                agent.LogError(String.Format("This should not happen!"));
                throw new NotImplementedException();

            case ActionState.WAITING:
                (double energy, double happiness) = calculateWaitingEffect();
                agent.motivation = energy;
                agent.happiness = happiness;
                return true;

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

            case ActionState.WAITING:
                agent.LogDebug(String.Format("Giving up to wait for {0}!", otherAgent));
                retry_cnter = 0;
                break;

            case ActionState.EXECUTING:
                agent.LogDebug(String.Format("Ending Quarrel with {0}!", otherAgent));
                otherAgent = null;
                retry_cnter = 0;

                // Give the agent an happiness boost in order to not start quarrel again imediately
                agent.happiness += config["HAPPINESS_BOOST"];
                break;
        }
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
        state = ActionState.WAITING;
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
