using System;

public class Quarrel : AgentBehavior
{
    private const double NOISE_INC = 0.2;
    private const double ENERGY_BIAS = 0.5;
    private const double MOTIVATION_THRESHOLD = 0.2;
    private const double HAPPINESS_BIAS = 0.0; // If bias = 0.0f, -happiness * SCALE
    private const double HAPPINESS_WEIGHT = 0.7;

    private const double HAPPINESS_INCREASE = -0.2;
    private const double MOTIVATION_INCREASE = -0.2;

    private const int RETRY_THRESHOLD = 3;
    private int retry_cnter;

    private Agent otherAgent;

    public Quarrel(Agent agent) : base(agent, AgentBehavior.Actions.Quarrel, "Quarrel", NOISE_INC) { }
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
        if (agent.motivation < MOTIVATION_THRESHOLD)
        {
            agent.LogInfo($"Cannot keep up Quarrel, motivationt too low. {agent.motivation} < {MOTIVATION_THRESHOLD} ...");
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
                    if (retry_cnter >= RETRY_THRESHOLD)
                    {
                        agent.LogInfo(String.Format("Giving up to quarel with {0}. Will try another agent ...", otherAgent));
                        engageOtherAgent();
                    }
                    else
                    {
                        retry_cnter++;
                        otherAgent.Interact(agent, this);
                        agent.navagent.destination = otherAgent.transform.position;
                        agent.LogInfo(String.Format("Trying again {0} to quarrel with {1}", retry_cnter, otherAgent));
                    }
                }
                return true;
            case ActionState.EXECUTING:
                if ((otherAgent.Desire is Quarrel) || (otherAgent.currentAction is Quarrel))
                {
                    return true;
                } else {
                    // The other left; Execution will return false
                    agent.LogInfo(String.Format("Other agent {0} has left the quarrel ...", otherAgent));
                    otherAgent = null;
                    state = ActionState.INACTIVE;
                }
                return false;
        }

        return false;
    }

    public override double rate()
    {
        double happiness = ExpDecay(agent.happiness);
        double motivation = ExpDecay(agent.motivation);
        double score = (happiness * HAPPINESS_WEIGHT) + (motivation * (1.0 - HAPPINESS_WEIGHT));
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
                agent.happiness = boundValue(0.0, agent.happiness + HAPPINESS_INCREASE, 1.0);
                agent.motivation = boundValue(0.0, agent.motivation + MOTIVATION_INCREASE, 1.0);

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
            agent.LogInfo(String.Format("No other Agent to quarrel with!"));
            return false;
        }

        // Select a random other agent
        int idx;
        do
        {
            idx = agent.random.Next(agent.classroom.agents.Length);
            otherAgent = agent.classroom.agents[idx];
        } while (otherAgent == agent);

        agent.LogInfo(String.Format("Agent tries to quarrel with agent {0}!", otherAgent));
        otherAgent.Interact(agent, this);
        agent.navagent.destination = otherAgent.transform.position;

        return true;
    }

    public void acceptInviation(Agent otherAgent)
    {
        agent.LogInfo(String.Format("{0} is accepting invitation to quarrel with {1}!", agent, otherAgent));
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
                return String.Format("{0}({1}) working with {2}", name, state, otherAgent.studentname);
        }
        return "Invalid State!";
    }
}
