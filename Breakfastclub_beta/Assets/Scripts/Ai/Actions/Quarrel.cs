using System;

public class Quarrel : AgentBehavior
{
    private const float NOISE_INC = 0.2f;
    private const float ENERGY_THRESHOLD = 0.2f;
    private const float HAPPINESS_BIAS = -0.2f; // If bias = 0.0f, -happiness * SCALE
    private const float HAPPINESS_WEIGHT = 0.5f;
    private const float SCORE_SCALE = 100.0f;

    private const float HAPPINESS_DECREASE = 0.1f;
    private const float ENERGY_DECREASE = 0.05f;


    public Quarrel(Agent agent) : base(agent, AgentBehavior.Actions.Quarrel, "Quarrel", NOISE_INC) { }
    /*
    • requirements: low happiness, presence of another agent, enough energy
    • effect: reduce energy a lot at every turn, increase noise a lot, reduce happiness a lot at every turn
    */
    public override bool possible()
    {
        if (agent.energy >= ENERGY_THRESHOLD)
            return true;
        else
            return false;
    }

    public override int evaluate()
    {
        float happiness = (boundValue(-1.0f, HAPPINESS_BIAS - agent.happiness, 1.0f));
        if (agent.energy >= ENERGY_THRESHOLD)
            return (int)(happiness * SCORE_SCALE);
        else
            return -1000;
        //float energy = boundValue(0.0f, agent.energy, 1.0f);
        //float score = (happiness * HAPPINESS_WEIGHT) + (energy * (1.0f - HAPPINESS_WEIGHT));
        //return (int)(score * SCORE_SCALE);
    }

    public override bool execute()
    {
        agent.happiness = Math.Max(-1.0f, agent.happiness - HAPPINESS_DECREASE);
        agent.energy = Math.Max(0.0f, agent.energy - ENERGY_DECREASE);
        return false;
    }

    public override void end()
    {
    }
}
