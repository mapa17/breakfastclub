using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class AgentBehavior
{
    public enum Actions : int { StudyAlone, StudyGroup, Break, Chat, Quarrel };
    public enum ActionState : int { INACTIVE, TRANSITION, WAITING, EXECUTING };

    public static double EXP1 = 1.718281828; //exp(1) - 1

    // Each action has at least a state value and a name
    public Actions action { get; }
    public String name { get; }
    public double noise_inc { get; }
    public Agent agent { get; protected set; }

    public ActionState state;

    public Dictionary<string, double> config;


    protected AgentBehavior(Agent agent, Actions state, String name, Dictionary<string, double> config)
    {
        this.agent = agent;
        this.action = state;
        this.name = name;
        this.config = config;
        this.noise_inc = config["NOISE"];

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

    public static double ExpGrowth(double x)
    {
        return (Math.Exp(x * x) - 1.0) / EXP1;
    }

    public static double ExpDecay(double x, double power=2)
    {
        return (Math.Exp(Math.Pow(1.0 - x, power)) - 1.0) / EXP1;
    }

    // Check preconditions for this action
    public abstract bool possible();

    // Evaluate how well suited this action is for the given agent
    public abstract double rate();

    // Helper function that is called in rate() implemented in each behavior
    protected double CalculateScore(double personality_term, double personlity_weight, double motivation_term, double motivation_weight, double happiness_term, double happiness_weight)
    {
        // Normalize the weights
        double sum = personlity_weight + motivation_weight + happiness_weight;

        double weighted = (personality_term * personlity_weight/sum) + (motivation_term * motivation_weight/sum) + (happiness_term * happiness_weight/sum);
        double score = boundValue(0.0, weighted, 1.0);
        return score;
    }

    // The agent performs this action
    public abstract bool execute();

    // Called when agent ends action (e.g. switches to another)
    public abstract void end();


    public (double, double) calculateWaitingEffect()
    {
        Dictionary<string, double> config = agent.SC.AgentBehavior;
        double intensity = boundValue(0.0, agent.personality.neuroticism * config["NEUROTICISM_WEIGHT"] - agent.personality.agreeableness * config["AGREEABLENESS_WEIGHT"], 1.0);
        double happiness = boundValue(0.0, agent.happiness + intensity * config["HAPPINESS_INCREASE"], 1.0);
        double motivation = boundValue(0.0, agent.motivation + config["MOTIVATION_INCREASE"], 1.0);
        return (motivation, happiness);
    }


    public (double, double) calculateTransitionEffect()
    {
        Dictionary<string, double> config = agent.SC.AgentBehavior;
        double intensity = boundValue(0.0, agent.personality.neuroticism * config["NEUROTICISM_WEIGHT"] - agent.personality.agreeableness * config["AGREEABLENESS_WEIGHT"], 1.0);
        double happiness = boundValue(0.0, agent.happiness + intensity * config["HAPPINESS_INCREASE"], 1.0);
        double motivation = boundValue(0.0, agent.motivation + config["MOTIVATION_INCREASE"], 1.0);
        return (motivation, happiness);
    }

    protected bool IsCloseTo(Agent otherAgent)
    {
        return IsCloseTo(otherAgent.transform.position);
    }

    protected bool IsCloseTo(Transform otherTransform)
    {
        return IsCloseTo(otherTransform.position);
    }

    protected bool IsCloseTo(Vector3 otherPosition)
    {
        float dist = Vector3.Distance(agent.transform.position, otherPosition);
        if (dist <= 2.0)
            return true;
        else
            return false;
    }

}
