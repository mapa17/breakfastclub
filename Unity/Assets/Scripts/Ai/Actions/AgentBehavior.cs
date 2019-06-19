using System;
using System.Collections.Generic;


public abstract class AgentBehavior
{
    public enum Actions : int { StudyAlone, StudyGroup, Break, Chat, Quarrel };
    public enum ActionState : int { INACTIVE, WAITING, EXECUTING };

    // Each action has at least a state value and a name
    public Actions action { get; }
    public String name { get; }
    public float noise_inc { get; }
    public Agent agent { get; protected set; }

    public ActionState state;


    protected AgentBehavior(Agent agent, Actions state, String name, float noise_inc)
    {
        this.action = state;
        this.name = name;
        this.noise_inc = noise_inc;
        this.agent = agent;

        this.state = ActionState.INACTIVE;
    }

    protected float boundValue(float min, float value, float max)
    {
        return (Math.Max(min, Math.Min(max, value)));
    }

    public override string ToString()
    {
        return String.Format("Action {0}({1})", name, state);
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
    public abstract int rate();

    // The agent performs this action
    public abstract bool execute();

    // Called when agent ends action (e.g. switches to another)
    public abstract void end();


    private const float HAPPINESS_INCREASE = -0.2f;
    private const float ENERGY_INCREASE = -0.0f;

    private const float NEUROTICISM_WEIGHT = 1.0f;
    private const float AGREEABLENESS_WEIGHT = 0.5f;

    public (float, float) calculateWaitingEffect()
    {
        float strengh = boundValue(0.0f, agent.personality.neuroticism * NEUROTICISM_WEIGHT - agent.personality.agreeableness * AGREEABLENESS_WEIGHT, 1.0f);
        float happiness = boundValue(-1.0f, agent.happiness + strengh * HAPPINESS_INCREASE, 1.0f);
        float energy = boundValue(0.0f, agent.energy + ENERGY_INCREASE, 1.0f);
        return (energy, happiness);
    }

}
