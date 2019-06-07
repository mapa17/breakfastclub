using System;

public abstract class AgentBehavior
{
    public enum Actions : int { StudyAlone, StudyGroup, Break, Chat, Quarrel, Wait, Transition };

    // Each action has at least a state value and a name
    public Actions action { get; }
    public String name { get; }
    public float noise_inc { get; }
    public Agent agent { get; protected set; }

    protected AgentBehavior(Agent agent, Actions state, String name, float noise_inc)
    {
        this.action = state;
        this.name = name;
        this.noise_inc = noise_inc;
        this.agent = agent;
    }

    protected float boundValue(float min, float value, float max)
    {
        return (Math.Max(min, Math.Min(max, value)));
    }

    // Check preconditions for this action
    public abstract bool possible();

    // Evaluate how well suited this action is for the given agent
    public abstract int evaluate();

    // The agent performs this action
    public abstract bool execute();

    // Called when agent ends action (e.g. switches to another)
    public abstract void end();
}
