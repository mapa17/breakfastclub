using System;
using System.Collections.Generic;


public abstract class AgentBehavior
{
    public enum Actions : int { StudyAlone, StudyGroup, Break, Chat, Quarrel };
    public enum ActionState : int { INACTIVE, WAITING, EXECUTING };

    public double EXP1 = 1.718281828; //exp(1) - 1

    // Each action has at least a state value and a name
    public Actions action { get; }
    public String name { get; }
    public double noise_inc { get; }
    public Agent agent { get; protected set; }

    public ActionState state;


    protected AgentBehavior(Agent agent, Actions state, String name, double noise_inc)
    {
        this.action = state;
        this.name = name;
        this.noise_inc = noise_inc;
        this.agent = agent;

        this.state = ActionState.INACTIVE;
    }

    public static double boundValue(double min, double value, double max)
    {
        return (Math.Max(min, Math.Min(max, value)));
    }

    public override string ToString()
    {
        return String.Format("{0}({1})", name, state);
    }

    public List<int> GetPermutedIndices(int count)
    {
        List<int> pool = new List<int>();
        for (int i = 0; i < count; i++)
            pool.Add(i);

        List<int> indices = new List<int>();
        int j;
        for (int i = 0; i < count; i++)
        {
            j = agent.random.Next(pool.Count);
            indices.Add(pool[j]);
            pool.Remove(pool[j]);
        }

        return indices;
    }

    // Check preconditions for this action
    public abstract bool possible();

    // Evaluate how well suited this action is for the given agent
    public abstract double rate();

    // The agent performs this action
    public abstract bool execute();

    // Called when agent ends action (e.g. switches to another)
    public abstract void end();


    private const float HAPPINESS_INCREASE = -0.2f;
    private const float MOTIVATION_INCREASE = -0.02f;

    private const float NEUROTICISM_WEIGHT = 1.0f;
    private const float AGREEABLENESS_WEIGHT = 0.5f;

    public (double, double) calculateWaitingEffect()
    {
        double strengh = boundValue(0.0, agent.personality.neuroticism * NEUROTICISM_WEIGHT - agent.personality.agreeableness * AGREEABLENESS_WEIGHT, 1.0);
        double happiness = boundValue(-1.0, agent.happiness + strengh * HAPPINESS_INCREASE, 1.0);
        double energy = boundValue(0.0, agent.motivation + MOTIVATION_INCREASE, 1.0);
        return (energy, happiness);
    }

}
