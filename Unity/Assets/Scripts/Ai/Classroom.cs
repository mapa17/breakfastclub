using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

using System.IO;
using System.Text;

[Serializable]
public class GameConfig
{
    public string name;
    public int seed;
    public int ticks;
    public double timescale;
    public PersonalityType[] agent_types;
    public int[] nAgents;
}

public class Classroom : MonoBehaviour
{
    public double noise { get; protected set; }

    public string configfile = "ConfigFile.json";
    public TMPro.TextMeshProUGUI tickCounterText;
    public TMPro.TextMeshProUGUI onScreenLogText;

    [SerializeField] public Table[] groupTables;
    [SerializeField] public Table[] individualTables;
    [SerializeField] public AgentSpawner[] AgentSpawners;

    [NonSerialized] public Agent[] agents;
    [NonSerialized] public bool gamePaused = false;
    [NonSerialized] public Transform groundfloorTransform;

    private GlobalRefs GR;
    private CSVLogger Logger;
    [HideInInspector] public double turnCnt = 0;
    private int commandline_seed = 0;

    private GameConfig gameConfig = new GameConfig();


    private double motivation_mean;
    private double motivation_std;
    private double happiness_mean;
    private double happiness_std;
    private double attention_mean;
    private double attention_std;

    System.Random random;

    // Start is called before the first frame update
    void Start()
    {
        noise = 0.0;

        GetReferences();

        try { 
            ParseCommandLine(); 
        } catch { Debug.Log("Parsing command line failed!"); };


        LoadGameConfig(configfile);

        SpawnAgents();

        // Find all Agents
        agents = FindObjectsOfType<Agent>();

        //onScreenLogText.text = $"Seed {gameConfig.seed}\nConfig file {configfile}";
        Debug.Log($"Seed: {gameConfig.seed}\nConfig file: {configfile}");
    }

    private void GetReferences()
    {
        GR = GlobalRefs.Instance;
        Logger = GR.logger;
        groundfloorTransform = transform.Find("Groundfloor").GetComponent<Transform>();
    }

    private void ParseCommandLine()
    {
        // Dont do any parsing if we are in editor mode
        if (Application.isEditor)
            return;

        string[] args = System.Environment.GetCommandLineArgs();
        // Filter all args that start with -
        List<string> filtered_args = new List<string>();
        foreach (string s in args)
        {
            if (s[0] != '-')
            {
                filtered_args.Add(s);
            }
        }
        //onScreenLogText.text = string.Join(" ", filtered_args);

        try
        {
            configfile = filtered_args[1];
            commandline_seed = int.Parse(filtered_args[2]);
            string logfilepath = filtered_args[3];

            Logger.setLogfile(logfilepath);
        }
        catch { };

    }

    private void LoadGameConfig(string configpath)
    {
        //createGameConfig("NewGameConfig.json");

        // Load game config
        string config = System.IO.File.ReadAllText(@configpath);
        gameConfig = JsonUtility.FromJson<GameConfig>(config);

        // Command line seed overwrites config seed!
        if(commandline_seed != 0)
        { 
            gameConfig.seed = commandline_seed;
        }
        random = new System.Random(gameConfig.seed);
        Time.timeScale = (float)gameConfig.timescale;
    }

    private void SpawnAgents()
    {
        int nAgents = 0;
        for (int i = 0; i < Math.Min(gameConfig.agent_types.Length, gameConfig.nAgents.Length); i++)
        {
            for(int k = 0; k < gameConfig.nAgents[i]; k++)
            {
                System.Random newRandom = new System.Random(random.Next());
                AgentSpawner asp = AgentSpawners[random.Next(AgentSpawners.Length)];
                Personality p = new Personality(newRandom, gameConfig.agent_types[i]);

                GameObject newAgent = asp.SpawnAgent(newRandom, p);
                newAgent.name = $"Agent{nAgents:D2}";
                nAgents++;
            }
        }
    }


    // Used to create a teamplate json config file that later can be edited by hand
    private void createGameConfig(string filename)
    {
        GameConfig gc = new GameConfig();
        gc.name = "TestConfig";
        gc.seed = 42;
        gc.ticks = 100;
        gc.timescale = 100.0;
        gc.agent_types = new PersonalityType[2];
        gc.agent_types[0] = new PersonalityType("Type1", 0.8, 0.6, -1, -1, 0.6);
        gc.agent_types[1] = new PersonalityType("Type2", 0.6, 0.5, 0.8, 0.8, 0.2);
        gc.nAgents = new int[2];
        gc.nAgents[0] = 2;
        gc.nAgents[1] = 3;


        string json = JsonUtility.ToJson(gc);
        StreamWriter sw = new StreamWriter(filename);
        sw.Write(json);
        sw.Close();
    }

    private void Update()
    {
        tickCounterText.text = "Tick: " + turnCnt.ToString() + "\nNoise: " + noise.ToString();
        //Debug.Log("Update time :" + Time.deltaTime);
        if (Input.GetKeyDown("space"))
        {
            if (gamePaused)
            {
                Debug.Log("Resume Game");
                Time.timeScale = (float)gameConfig.timescale;
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

        Debug.Log($"Turn: {turnCnt}");

        // Update this list every turn (agents might leave or enter the classroom)
        agents = FindObjectsOfType<Agent>();

        UpdateStats();

        LogStats();

        if((gameConfig.ticks > 0) && (turnCnt >= gameConfig.ticks))
        {
            EndSimulation();
        }
    }

    public void EndSimulation()
    {
        Debug.Log("Ending Game!");
        Application.Quit();
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
            if((agent.currentAction is StudyAlone) || (agent.currentAction is StudyGroup))
            {
                values.Add(agent.attention);
            }
        }

        //if (values.Count() > 0)
        if (values.Any())
        {
            mean = Mean(values);
            std = Std(values);
        }
        else
        {
            mean = 0.0;
            std = 0.0;
        }

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
