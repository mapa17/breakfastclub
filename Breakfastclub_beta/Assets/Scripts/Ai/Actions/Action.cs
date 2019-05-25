using System;

public abstract class Action
{
    public enum States : int { Study, GroupStudy, Break, Chat, Quarrel, Wait, Transition };

    // Check preconditions for this action
    public abstract bool possible(Agent agent);

    // Perform the action
    public abstract void perform(Agent agent);
}
