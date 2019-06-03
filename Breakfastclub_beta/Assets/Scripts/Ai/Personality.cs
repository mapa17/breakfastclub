using System;

public class Personality
{
    public float neuroticism { get; protected set; }
    public float extraversion { get; protected set; }
    public float openess { get; protected set; }
    public float agreeableness { get; protected set; }
    public float conscientousness { get; protected set; }

    public Personality()
    {
        Random random = new Random();

        neuroticism = random.Next(100) / 100f;
        extraversion = random.Next(100) / 100f;
        openess = random.Next(100) / 100f;
        agreeableness = random.Next(100) / 100f;
        conscientousness = random.Next(100) / 100f;
    }

    public override string ToString() { return "N:" + neuroticism + " E:" + extraversion + " O:" + openess + " A:" + agreeableness +  " C:" + conscientousness; }
}
