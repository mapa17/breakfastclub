using System;
using UnityEngine;

public class Break : AgentBehavior
{
    private const double NOISE_INC = 0.05;
    private const double HAPPINESS_INCREASE = 0.00;
    private const double MOTIVATION_INCREASE = 0.05;

    //private const double MOTIVATION_BIAS = -0.5; // Negative Values encourage work, positive to take a break
    private const double MOTIVATION_BIAS = -0.0; // Negative Values encourage work, positive to take a break
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

    public override double rate()
    {
        // The score is defined by the vale of extraversion and the energy of the agent
        // Low values of extraversion and low values of energy increase the score (make this action more likely)

        /*
        // Agents low on extraversion prefare break (over chat)
        double extra = (1.0 - agent.personality.extraversion);
        double motivation = ExpDecay(agent.motivation);
        //double score = boundValue(0.0, (extra * EXTRAVERSION_WEIGHT) + (motivation * (1.0 - EXTRAVERSION_WEIGHT)), 1.0);
        double combined = (extra * EXTRAVERSION_WEIGHT) + (motivation * (1.0 - EXTRAVERSION_WEIGHT));
        double happiness_adjusted = combined * ExpGrowth(agent.happiness);
        double score = boundValue(0.0, happiness_adjusted, 1.0);

        return score;
        */

        double PERSONALITY_WEIGHT = 0.33;
        double MOTIVATION_WEIGHT = 0.33;
        double HAPPINESS_WEIGHT = 0.33;
        double personality = 1.0 - agent.personality.extraversion;
        double motivation = ExpDecay(agent.motivation);
        double happiness = ExpGrowth(agent.happiness);
        double wheighted = (personality * PERSONALITY_WEIGHT) + (motivation * MOTIVATION_WEIGHT) + (happiness * HAPPINESS_WEIGHT);

        double score = boundValue(0.0, wheighted, 1.0);
        return score;
    }

    public override bool execute()
    {
        agent.motivation = boundValue(0.0, agent.motivation + MOTIVATION_INCREASE, 1.0);
        agent.happiness = boundValue(0.0, agent.happiness + HAPPINESS_INCREASE, 1.0);

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
