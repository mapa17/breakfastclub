using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using System.IO;

[Serializable]
public class GameConfig
{
    public string name;
    public PersonalityType[] agent_types;
    public int[] nAgents;
}

public class Classroom : MonoBehaviour
{
    public double noise { get; protected set; }

    public string configfile = "ConfigFile.json";
    [SerializeField] public Table[] groupTables;
    [SerializeField] public Table[] individualTables;

    [NonSerialized] public Agent[] agents;
    [NonSerialized] public bool gamePaused = false;
    [NonSerialized] public Transform groundfloorTransform;

    private GlobalRefs GR;
    private CSVLogger Logger;
    [HideInInspector] public double turnCnt = 0;

    private GameConfig gameConfig = new GameConfig();


    private double motivation_mean;
    private double motivation_std;
    private double happiness_mean;
    private double happiness_std;
    private double attention_mean;
    private double attention_std;


    // Start is called before the first frame update
    void Start()
    {
        noise = 0.0;

        GR = GlobalRefs.Instance;
        Logger = GR.logger;

        agents = FindObjectsOfType<Agent>();

        groundfloorTransform = transform.Find("Groundfloor").GetComponent<Transform>();

        string[] args = System.Environment.GetCommandLineArgs ();

        // Load game config
        string config = System.IO.File.ReadAllText(@configfile);
        gameConfig = JsonUtility.FromJson<GameConfig>(config);
    }


    // Used to create a teamplate json config file that later can be edited by hand
    private void createGameConfig(string filename)
    {
        GameConfig gc = new GameConfig();
        gc.name = "TestConfig";
        gc.agent_types = new PersonalityType[2];
        gc.agent_types[0] = new PersonalityType(0.8, 0.6, -1, -1, 0.6);
        gc.agent_types[1] = new PersonalityType(0.6, 0.5, 0.8, 0.8, 0.2);
        gc.nAgents = new int[2];
        gc.nAgents[0] = 2;
        gc.nAgents[1] = 3;


        string json = JsonUtility.ToJson(gc);
        StreamWriter sw = new StreamWriter(filename);
        sw.Write(json);
        sw.Close();
    }


    private bool LoadGameConfig(string game_config_path)
    {

        return false;
    }

    private void Update()
    {
        //Debug.Log("Update time :" + Time.deltaTime);
        if (Input.GetKeyDown("space"))
        {
            if (gamePaused)
            {
                Debug.Log("Resume Game");
                Time.timeScale = 1.0f;
            }
            else
            {
                Debug.Log("Pause Game");
                Time.timeScale = 0.0f;
            }
            gamePaused = !gamePaused;
        }
        else if(Input.GetKeyDown("q"))
        {
            Application.Quit();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        turnCnt++;

        // Update this list every turn (agents might leave or enter the classroom)
        agents = FindObjectsOfType<Agent>();

        UpdateStats();

        LogStats();
    }

    public double Mean(List<double> values)
    {
        return values.Sum()/values.Count();
    }

    public double Variance(List<double> values, double meanvalue)
    {
        double variance = 0.0;
        foreach(double v in values){
            variance += (v-meanvalue)*(v-meanvalue);
        }
        variance = variance / values.Count();
        return variance;
    }

    public double Std(List<double> values)
    {
        double m = Mean(values);
        double v = Variance(values, m);
        return Math.Sqrt(v);
    }

    public void LogX(string message, string type)
    {
        string[] msg = { gameObject.name, turnCnt.ToString(), type, message };
        Logger.log(msg);
    }

    public void LogStats(bool include_info_log = true)
    {
        if (include_info_log)
        {
            LogX(String.Format($"#Agents {agents.Count()} | Noise {noise} | Energy_mean {motivation_mean} | Energy_std {motivation_std} | Happiness_mean {happiness_mean} | Happiness_std {happiness_std} | Attention_mean {attention_mean} | Attention_std {attention_std}"), "I");
        }
        LogX(String.Format($"{agents.Count()}|{noise}|{motivation_mean}|{motivation_std}|{happiness_mean}|{happiness_std}|{attention_mean}|{attention_std}"), "S");

    }

    public void UpdateStats()
    {
        (motivation_mean, motivation_std) = GetAccEnergy();
        (happiness_mean, happiness_std) = GetAccHappiness();
        (attention_mean, attention_std) = GetAccAttention();

        //noise = AgentBehavior.boundValue(0.0, GetAccNoise(), 1.0); 
        noise = GetAccNoise();
    }

    private double GetAccNoise()
    {
        // Reset noise

        double noise_inc = 0.0;
        foreach (var agent in agents)
        {
            noise_inc += agent.currentAction.noise_inc;
        }
        return noise_inc;
    }

    private (double, double) GetAccEnergy()
    {
        double mean = 0.0;
        double std = 0.0;

        List<double> values = new List<double>();
        foreach(Agent agent in agents)
        {
            values.Add(agent.motivation);
        }
        mean = Mean(values);
        std = Std(values);

        return (mean, std);
    }

    private (double, double) GetAccHappiness()
    {
        double mean = 0.0;
        double std = 0.0;

        List<double> values = new List<double>();
        foreach (Agent agent in agents)
        {
            values.Add(agent.happiness);
        }
        mean = Mean(values);
        std = Std(values);

        return (mean, std);
    }

    private (double, double) GetAccAttention()
    {
        double mean = 0.0;
        double std = 0.0;

        List<double> values = new List<double>();
        foreach (Agent agent in agents)
        {
            values.Add(agent.attention);
        }
        mean = Mean(values);
        std = Std(values);

        return (mean, std);
    }


    public List<Agent> getAgentsByDesire(AgentBehavior filter)
    {
        List<Agent> available = new List<Agent>();
        foreach (var agent in agents)
        {
            if(agent.Desire.Equals(filter))
            {
                available.Add(agent);
            }
        }
        return available;
    }


    public List<Agent> getAgentsByAction(AgentBehavior filter)
    {
        List<Agent> available = new List<Agent>();
        foreach (var agent in agents)
        {
            if (agent.currentAction.Equals(filter))
            {
                available.Add(agent);
            }
        }
        return available;
    }
}
