using System;

public class Personality
{
    public double neuroticism { get; protected set; }
    public double extraversion { get; set; }
    public double openess { get; protected set; }
    public double agreeableness { get; protected set; }
    public double conscientousness { get; protected set; }

    public Personality(Random random=null)
    {
        if (random == null){
            random = new Random();
        }

        neuroticism = random.Next(100) / 100.0;
        extraversion = random.Next(100) / 100.0;
        openess = random.Next(100) / 100.0;
        agreeableness = random.Next(100) / 100.0;
        conscientousness = random.Next(100) / 100.0;
    }

    public override string ToString() { return "N:" + neuroticism + " E:" + extraversion + " O:" + openess + " A:" + agreeableness +  " C:" + conscientousness; }
}
