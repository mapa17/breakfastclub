using System;

public class Quarrel : Action
{
    /*
    • requirements: low happiness, presence of another agent, enough energy
    • effect: reduce energy a lot at every turn, increase noise a lot, reduce happiness a lot at every turn
    */
    public override bool possible(Agent agent)
    {
        throw new NotImplementedException();
    }

    public override void perform(Agent agent)
    {
        throw new NotImplementedException();
    }
}
