using System;

public class Break : AgentBehavior
{
    private const float NOISE_INC = 0.0f;
    private const float HAPPINESS_INCREASE = 0.05f;
    private const float ENERGY_INCREASE = 0.05f;

    private const float ENERGY_BIAS = -0.4f; // Negative Values incourage work, positive to take a break
    private const float SCORE_SCALE = 100.0f;
    private const float EXTRAVERSION_WEIGHT = 0.3f;


    public Break(Agent agent) : base(agent, AgentBehavior.Actions.Break, "Break", NOISE_INC) { }

    /*
    • requirements: free spot on individual table
    • effect: regenerate energy, will increase happiness(amount is a function of extraversion)
    */
    public override bool possible()
    {
        return true;
    }

    public override int rate()
    {
        // The score is defined by the vale of extraversion and the energy of the agent
        // Low values of extraversion and low values of energy increase the score (make this action more likely)

        // Agents low on extraversion prefare break (over chat)
        float extra = (1.0f - agent.personality.extraversion);
        float energy = boundValue(0.0f, 1.0f + ENERGY_BIAS - agent.energy, 1.0f);
        float t = (extra * EXTRAVERSION_WEIGHT) + (energy * (1.0f - EXTRAVERSION_WEIGHT));

        int score = (int)(boundValue(0.0f, t, 1.0f) * SCORE_SCALE);
        return score;
    }

    public override bool execute()
    {
        agent.energy = boundValue(0.0f, agent.energy + ENERGY_INCREASE, 1.0f);
        agent.happiness = boundValue(-1.0f, agent.happiness + HAPPINESS_INCREASE, 1.0f);
        //agent.energy = Math.Max(-1.0f, Math.Min(1.0f, agent.energy + ENERGY_INCREASE)); ;
        //agent.happiness = Math.Max(-1.0f, Math.Min(1.0f, agent.happiness + HAPPINESS_INCREASE));
        return true;
    }

    public override void end()
    {
    }
}
