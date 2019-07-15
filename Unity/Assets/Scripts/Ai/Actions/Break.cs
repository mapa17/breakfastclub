using System;
using UnityEngine;

public class Break : AgentBehavior
{
    private const double NOISE_INC = 0.05;
    private const double HAPPINESS_INCREASE = 0.02;
    private const double MOTIVATION_INCREASE = 0.02;

    private const double MOTIVATION_BIAS = -0.4; // Negative Values incourage work, positive to take a break
    private const double SCORE_SCALE = 100.0;
    private const double EXTRAVERSION_WEIGHT = 0.3;


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
        double extra = (1.0 - agent.personality.extraversion);
        double energy = boundValue(0.0, 1.0 + MOTIVATION_BIAS - agent.motivation, 1.0);
        double t = (extra * EXTRAVERSION_WEIGHT) + (energy * (1.0 - EXTRAVERSION_WEIGHT));

        int score = (int)(boundValue(0.0, t, 1.0) * SCORE_SCALE);
        return score;
    }

    public override bool execute()
    {
        agent.motivation = boundValue(0.0, agent.motivation + MOTIVATION_INCREASE, 1.0);
        agent.happiness = boundValue(-1.0, agent.happiness + HAPPINESS_INCREASE, 1.0);

        // Perform a random walk in the classroom
        Vector3 dest = agent.classroom.groundfloorTransform.TransformPoint(agent.random.Next(100) / 100.0f, agent.random.Next(100) / 100.0f, 0.0f);
        //Debug.Log("Random walk towards " + dest);
        agent.navagent.SetDestination(dest);

        state = ActionState.EXECUTING;
        return true;
    }

    public override void end()
    {
        state = ActionState.INACTIVE;
    }
}
