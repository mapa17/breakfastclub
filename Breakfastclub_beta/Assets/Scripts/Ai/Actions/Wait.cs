using System;

public class Wait : Action
{
    /*
    • requirements: None
    • effect: reduce happiness
    */  
    public override bool possible(Agent agent)
    {
        return true;
    }

    public override void perform(Agent agent)
    {
        throw new NotImplementedException();
        // TODO: how to modify happiness of the agent?
    }
}
