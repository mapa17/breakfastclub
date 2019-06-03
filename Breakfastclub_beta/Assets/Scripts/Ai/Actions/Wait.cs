using System;

public class Wait : AgentBehavior
{
    private const float NOISE_INC = 0.05f;
    private const float HAPPINESS_INCREASE = -1.0f;

    private const float NEUROTICISM_WEIGHT = 1.0f;
    private const float AGREEABLENESS_WEIGHT = 0.5f;

    public Wait() : base(AgentBehavior.Actions.Wait, "Wait", NOISE_INC) { }

    /*
    • requirements: None
    • effect: reduce happiness
    */
    public override bool possible(Agent agent)
    {
        return true;
    }

    public override int evaluate(Agent agent)
    {
        // Wait is always possible, but has the least possible score
        return 0;
    }

    public override bool execute(Agent agent)
    {
        /*
         * The amount of decreased energy is scaled by neuroticism and agreeableness
         * High values of neuroticism cause higher happiness loss
         * High values of agreeableness prevent high happiness loss
         * Agreeableness works as an antagonist to Neuroticism
         */       
        float rate = (float) Math.Max(agent.personality.neuroticism*NEUROTICISM_WEIGHT - agent.personality.agreeableness*AGREEABLENESS_WEIGHT, 0.0f);
        agent.happiness = boundValue(-1.0f, agent.happiness + rate * HAPPINESS_INCREASE, 1.0f);
        return true;
    }
}
