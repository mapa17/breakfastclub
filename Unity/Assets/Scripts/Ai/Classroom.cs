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


// Helper struct that is used to generate SimulationConfig object from json file
[Serializable]
public struct SerializableSimulationConfig
{
    public string name;
    public NamedConfigValue[] Classroom;
    public NamedConfigValue[] Agent;
    public NamedConfigValue[] AgentBehavior;

    public NamedConfigValue[] Chat;
    public NamedConfigValue[] Break;
    public NamedConfigValue[] Quarrel;
    public NamedConfigValue[] StudyGroup;
    public NamedConfigValue[] StudyAlone;
}

[Serializable]
public struct NamedConfigValue
{
    public string field;
    public double value;
    public NamedConfigValue(string field, double value)
    {
        this.field = field;
        this.value = value;
    }
}

public class SimulationConfig
{
    public string name;
    public Dictionary<string, double> Classroom;
    public Dictionary<string, double> Agent;
    public Dictionary<string, double> AgentBehavior;
    public Dictionary<string, double> Chat;
    public Dictionary<string, double> Break;
    public Dictionary<string, double> Quarrel;
    public Dictionary<string, double> StudyGroup;
    public Dictionary<string, double> StudyAlone;

    public SimulationConfig(SerializableSimulationConfig sSC)
    {
        this.name = sSC.name;
        this.Classroom = List2Dict(sSC.Classroom);
        this.Agent = List2Dict(sSC.Agent);
        this.AgentBehavior = List2Dict(sSC.AgentBehavior);
        this.Chat = List2Dict(sSC.Chat);
        this.Break = List2Dict(sSC.Break);
        this.Quarrel = List2Dict(sSC.Quarrel); 
        this.StudyGroup = List2Dict(sSC.StudyGroup); 
        this.StudyAlone = List2Dict(sSC.StudyAlone);
    }

    private Dictionary<string, double>List2Dict(NamedConfigValue[] list)
    {
        Dictionary<string, double> dict = new Dictionary<string, double>();
        list.ToList().ForEach(x => dict.Add(x.field, x.value));
        return dict;
    }
}

public class Classroom : MonoBehaviour
{
    public double noise { get; protected set; }

    public string configfile = "ConfigFile.json";
    public string simulationConfigFile = "SimulationConfigFile.json";
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
    public SimulationConfig simulationConfig;

    public double[] peerActionScores { get; private set; }

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
        //UnityEngine.Random.InitState(42);

        GetReferences();

        try { 
            ParseCommandLine(); 
        } catch { Debug.Log("Parsing command line failed!"); };


        LoadSimulationConfig(simulationConfigFile);
        LoadGameConfig(configfile);

        SpawnAgents();

        // Find all Agents
        agents = FindObjectsOfType<Agent>();

        peerActionScores = new double[agents[0].scores.Length];

        //onScreenLogText.text = $"Seed {gameConfig.seed}\nConfig file {configfile}";
        Debug.Log($"Seed: {gameConfig.seed}\nConfig file: {configfile}");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        turnCnt++;

        Debug.Log($"Turn: {turnCnt}");

        // Update this list every turn (agents might leave or enter the classroom)
        agents = FindObjectsOfType<Agent>();

        UpdateStats();

        peerActionScores = GetPeerActionScore(agents);

        LogStats();

        if ((gameConfig.ticks > 0) && (turnCnt >= gameConfig.ticks))
        {
            EndSimulation();
        }
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
            simulationConfigFile = filtered_args[1];
            configfile = filtered_args[2];
            commandline_seed = int.Parse(filtered_args[3]);
            string logfilepath = filtered_args[4];

            Logger.setLogfile(logfilepath);
        }
        catch {
            Debug.LogError("Parsing Command line arguments failed!");
            Application.Quit();
        };
    }

    private void LoadGameConfig(string configpath)
    {
        //createGameConfig("NewGameConfig.json");
        Debug.Log($"Reading classroom config from {configpath} ...");
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

    private void LoadSimulationConfig(string configpath)
    {
        try
        {
            Debug.Log($"Reading classroom config from {configpath} ...");
            string json = System.IO.File.ReadAllText(@configpath);
            simulationConfig = new SimulationConfig(JsonUtility.FromJson<SerializableSimulationConfig>(json));
        } catch {
            Debug.LogError("Loading Simulation Config failed!");
            Application.Quit();
        };

    }

    private void SpawnAgents()
    {
        int nAgents = 0;
        for (int i = 0; i < Math.Min(gameConfig.agent_types.Length, gameConfig.nAgents.Length); i++)
        {
            for(int k = 0; k < gameConfig.nAgents[i]; k++)
            {
                int newseed = random.Next();
                System.Random newRandom = new System.Random(newseed);
                AgentSpawner asp = AgentSpawners[random.Next(AgentSpawners.Length)];
                Personality p = new Personality(newRandom, gameConfig.agent_types[i]);

                Debug.Log($"Spawning Agent {nAgents} with seed {newseed} ...");
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


    // Calculate a actino score vectoring representing the classroom interest
    private double[] GetPeerActionScore(Agent[] agents)
    {
        double[] scores = new double[agents[0].scores.Length];

        // Add scores
        foreach (var agent in agents)
        {
            scores = scores.Zip(agent.scores, (x, y) => x + y).ToArray();
        }

        // Normalize them, giving equal wheight to each agent (-> Flath dominace hierarchy)
        scores = scores.Select(x => x / scores.Length).ToArray();

        return scores;
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
