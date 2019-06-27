using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Text;

public struct InteractionRequest
{
    public Agent source;
    public AgentBehavior action;

    public InteractionRequest(Agent source, AgentBehavior action)
    {
        this.source = source;
        this.action = action;
    }
}

public class Agent : MonoBehaviour
{
    public List<String> studentnames = new List<String> { "Anton", "Esther", "Julian", "Marta", "Manuel", "Michael", "Pedro", "Anna", "Patrick", "Antonio", "Kertin", "Carol", "Laura", "Leticia", "Kathrin", "Sonya", "Herbert", "Felix", "Benjamin", "Juanma"};

    // A started action will get a bias in order to be repeated during the next turns
    private readonly int STICKY_ACTION_SCORE = 50;
    private readonly int STICKY_ACTION_BIAS = 10;
    private readonly double STICKY_ACTION_SCALE = 0.5; //Multiplier for lambda (lower values will reduce exponential decline speed)
    private int ticksOnThisTask;

    private readonly double HAPPINESS_INCREASE = 0.05;

    [SerializeField] public int seed;
    [NonSerialized] public string studentname;

    private GlobalRefs GR;
    private CSVLogger Logger;

    [HideInInspector] public Classroom classroom;
    [HideInInspector] public NavMeshAgent navagent;
    [HideInInspector] public double turnCnt = -1;

    public Personality personality { get; protected set; }

    public double happiness { get; set; }
    public double energy { get; set; }
    public double attention { get; protected set;}

    //private List<AgentBehavior> behaviors = new List<AgentBehavior>();
    private Dictionary<string, AgentBehavior> behaviors = new Dictionary<string, AgentBehavior>();
    public AgentBehavior currentAction { get; protected set; }
    public AgentBehavior Desire { get; protected set; }

    private Queue pendingInteractions = new Queue();

    public System.Random random;


    // Start is called before the first frame update
    private void OnEnable()
    {
        random = new System.Random(seed);

        // Create a personality for this agent
        personality = new Personality(random);

        studentname = studentnames[random.Next(studentnames.Count - 1)];

        navagent = GetComponent<NavMeshAgent>();

        // Define all possible actions
        behaviors.Add("Break", new Break(this));
        behaviors.Add("Quarrel", new Quarrel(this));
        behaviors.Add("Chat", new Chat(this));
        behaviors.Add("StudyAlone", new StudyAlone(this));
        behaviors.Add("StudyGroup", new StudyGroup(this));

        // Set the default action state to Break
        currentAction = behaviors["Break"];
        Desire = behaviors["Break"];

        // Initiate Happiness and Energy
        energy = Math.Max(0.5, random.Next(100)/100.0); // with a value between [0.5, 1.0]
        happiness = Math.Max(-0.5, 0.5 - random.Next(100)/100.0); // with a value between [-0.5, 0.5]

        //personality.extraversion = 0.9f;
    }

    void Start()
    {
        GR = GlobalRefs.Instance;
        Logger = GR.logger;
        classroom = GR.classroom;

        // Print personality traits as first stats line
        turnCnt = -1;
        LogX(String.Format($"{personality.openess}|{personality.conscientousness}|{personality.extraversion}|{personality.agreeableness}|{personality.neuroticism}"), "S");

        turnCnt = 0;
        LogInfo("Agent Personality: " + personality);

        //Agents AG = GameObject.Find("Agents").GetComponent<Agents>();
        LogState();
    }

    // Add given Agent and Action to event Queue
    public void Interact(Agent source, AgentBehavior action)
    {
        pendingInteractions.Enqueue(new InteractionRequest(source, action));
    }

    // Log message as info
    public void LogError(string message)
    {
        LogX(message, "E");
    }

    public void LogInfo(string message)
    {
        LogX(message, "I");
    }

    public void LogDebug(string message)
    {
        LogX(message, "D");
    }

    public void LogX(string message, string type)
    {
        string[] msg = { gameObject.name, turnCnt.ToString(), type, message };
        Logger.log(msg);
    }

    // Helper function logging Agent state
    private void LogState(bool include_info_log=true)
    {
        if(include_info_log)
            LogX(String.Format($"Energy {energy} | Happiness {happiness} | Attenion {attention} | Action {currentAction} | Desire {Desire}"), "I");
        LogX(String.Format($"{energy}|{happiness}|{attention}|{currentAction}|{Desire}"), "S");
    }

    public string GetStatus()
    {
        return String.Format("{0}\nEnergy {1} Happiness {2} Attenion {3}\nAction {4}\nDesire {5}", gameObject.name, energy, happiness, attention, currentAction, Desire);
    }

    // MAIN LOGIC : Called at each iteration
    void FixedUpdate()
    {
        turnCnt++;

        UpdateAttention();

        LogState();

        EvaluateActions();

        HandleInteractions();

        UpdateHappiness();
    }

    // attention = f(State, Environment, Personality)
    private void UpdateAttention()
    {
        attention = Math.Max((1.0 - classroom.noise) * personality.conscientousness * energy, 0.0);
    }

    // If current_action equals desire we are happy, sad otherwise
    private void UpdateHappiness()
    {
        double change;
        if(currentAction == Desire)
        {
            change = HAPPINESS_INCREASE;
        }
        else
        {
            change = -HAPPINESS_INCREASE;
        }
        happiness = Math.Max(-1.0, Math.Min(happiness + change, 1.0));
    }


    private bool StartAction(AgentBehavior newAction, bool setDesire=true, bool applyDefaultAction=true)
    {
        if (setDesire) {
            Desire = newAction;
        }
        if (newAction.possible())
        {
            if (newAction != currentAction)
            {
                LogDebug(String.Format("Ending current action {0}.", currentAction.name));
                currentAction.end();

                LogInfo(String.Format("Starting new action {0}. Executing ...", newAction.name));
                bool success = newAction.execute();
                if (!success)
                    LogDebug(String.Format("Executing new action failed! Will continou anyways! ..."));
                currentAction = newAction;
                ticksOnThisTask = 0;
            }
            else
            {
                newAction.execute();
                ticksOnThisTask++;
            }
            return true;
        }
        else
        {
            if (applyDefaultAction)
            {
                // Agent cannot perform Action, go into Wait instead
                LogInfo(String.Format("{0} is not possible. Executing break instead! ...", newAction));
                currentAction = behaviors["Break"];
                currentAction.execute();
            }
            return false;
        }
    }

    private void HandleInteractions()
    {
        while(pendingInteractions.Count > 0)
        {
            InteractionRequest iR = (InteractionRequest)pendingInteractions.Dequeue();
            LogInfo(String.Format("Interaction Request from {0} for action {1}", iR.source, iR.action));
            if (iR.action is Chat)
            {
                HandleChat(iR.source);
            }
            else if (iR.action is Quarrel)
            {
                HandleQuarrel(iR.source);
            }
        }
    }

    private void HandleChat(Agent otherAgent)
    {
        if( (currentAction is Chat) || (Desire is Chat))
        {
            LogDebug(String.Format("Accept invitation to chat with {0} ...", otherAgent));
            Chat chat = (Chat)behaviors["Chat"];
            chat.acceptInviation(otherAgent);
            StartAction(chat);
        }
        else
        {
            // An agent is convinced to chat based on its conscientousness trait.
            // Agents high on consciousness are more difficult to convince/distract
            float x = random.Next(100) / 100.0f;
            LogDebug(String.Format("Agent proposal {0} >= {1} ...", x, personality.conscientousness));
            if (x >= personality.conscientousness)
            {
                LogDebug(String.Format("Agent got convinced by {0} to start chatting ...", otherAgent));
                Chat chat = (Chat)behaviors["Chat"];
                chat.acceptInviation(otherAgent);
                StartAction(chat, false, false);
            }
            else
            {
                LogDebug(String.Format("Agent keeps to current action ({0} < {1})", x, personality.conscientousness));
            }
        }
    }

    private void HandleQuarrel(Agent otherAgent)
    {
        if( (currentAction is Quarrel) || (Desire is Quarrel))
        {
            LogDebug(String.Format("Agent wanted to Quarrel! Now he can do so with {0} ...", otherAgent));
            Quarrel quarrel = (Quarrel)behaviors["Quarrel"];
            quarrel.acceptInviation(otherAgent);
            StartAction(quarrel);
        }
        else
        {
            // An agent is convinced to chat based on its conscientousness trait.
            // Agents high on consciousness are more difficult to convince/distract
            double x = random.Next(100) / 100.0;
            LogDebug(String.Format("Agent proposal {0} >= {1} ...", x, personality.agreeableness));
            if (x >= personality.agreeableness)
            {
                LogDebug(String.Format("Agent got convinced by {0} to start quarreling ...", otherAgent));
                Quarrel quarrel = (Quarrel)behaviors["Quarrel"];
                quarrel.acceptInviation(otherAgent);
                StartAction(quarrel, false, false);
            }
            else
            {
                LogDebug(String.Format("Agent keeps to current action ({0} < {1})", x, personality.agreeableness));
            }
        }
    }

    // Main Logic
    private void EvaluateActions() 
    {
        StringBuilder sb = new StringBuilder();

        int best_rating = -100;
        int rating = best_rating;
        AgentBehavior best_action = null;
        AgentBehavior behavior = null;

        foreach (KeyValuePair<string, AgentBehavior> kvp in behaviors)
        {
            behavior = kvp.Value;
            rating = behavior.rate();

            // The current action gets a score boost that declines exponetially
            if (behavior == currentAction)
            {
                // Agents high on consciousness will stick longer to chosen actions
                double lambda = (1.0 - personality.conscientousness)*STICKY_ACTION_SCALE;
                int score_bias = STICKY_ACTION_BIAS + (int)(STICKY_ACTION_SCORE * Math.Exp(-lambda * (float)ticksOnThisTask));
                rating += score_bias;
            }

            //logInfo(String.Format("Behavior: {0} rating {1}", behavior.name, rating));
            sb.Append(String.Format("{0}:{1} ", behavior.name, rating));
            if (rating > best_rating)
            {
                best_rating = rating;
                best_action = behavior;
            }
        }
        LogInfo("Behavior: " + sb.ToString());

        if (best_action != null)
        {
            bool success = StartAction(best_action);
            if(success)
                LogInfo(String.Format("Starting Action {0}.", best_action));
            else
                LogInfo(String.Format("Starting Action {0} failed!", best_action));
        }

    }
}
