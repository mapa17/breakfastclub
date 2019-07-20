using System;

[Serializable]
public struct PersonalityType
{
    public string name;
    // Each Trial can have values between [0, 1]
    // A value of -1 will cause a random number to be used from a uniform distribution between [0, 1]
    public double openess;
    public double conscientousness;
    public double extraversion;
    public double agreeableness;
    public double neuroticism;

    public PersonalityType(string name, double o, double c, double e, double a, double n)
    {
        this.name = name;
        openess = o;
        conscientousness = c;
        extraversion = e;
        agreeableness = a;
        neuroticism = n;
    }
}


public class Personality
{
    public string name { get; protected set; }
    public double openess { get; protected set; }
    public double conscientousness { get; protected set; }
    public double extraversion { get; set; }
    public double agreeableness { get; protected set; }
    public double neuroticism { get; protected set; }

    public Personality(string name, double o, double c, double e, double a, double n)
    {
        this.name = name;
        openess = o;
        conscientousness = c;
        extraversion = e;
        agreeableness = a;
        neuroticism = n;
    }

    public Personality(Random random, PersonalityType pt)
    {
        name = pt.name;
        if (pt.openess < 0)
            openess = random.Next(100) / 100.0;
        else
            openess = pt.openess;

        if (pt.conscientousness < 0)
            conscientousness = random.Next(100) / 100.0;
        else
            conscientousness = pt.conscientousness;

        if (pt.extraversion < 0)
            extraversion = random.Next(100) / 100.0;
        else
            extraversion = pt.extraversion;

        if (pt.agreeableness < 0)
            agreeableness = random.Next(100) / 100.0;
        else
            agreeableness = pt.agreeableness;

        if (pt.neuroticism < 0)
            neuroticism = random.Next(100) / 100.0;
        else
            neuroticism = pt.neuroticism;
    }

    public Personality(Random random=null)
    {
        if (random == null){
            random = new Random();
        }

        name = "Random";
        neuroticism = random.Next(100) / 100.0;
        extraversion = random.Next(100) / 100.0;
        openess = random.Next(100) / 100.0;
        agreeableness = random.Next(100) / 100.0;
        conscientousness = random.Next(100) / 100.0;
    }

    public override string ToString() { return "T:" + name + " O:" + openess + " C:" + conscientousness + " E:" + extraversion + " A:" + agreeableness + " N:" + neuroticism; }
}
