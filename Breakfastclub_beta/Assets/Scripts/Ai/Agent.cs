using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    private readonly int SCORE_BIAS = 50;
    private int ticksOnThisTask;

    [SerializeField] public int seed;

    private GlobalRefs GR;
    private CSVLogger Logger;

    [HideInInspector] public Classroom classroom;
    [HideInInspector] public NavMeshAgent navagent;
    [HideInInspector] public double turnCnt = 0;

    public Personality personality { get; protected set; }

    public float happiness { get; set; }
    public float energy { get; set; }
    public float attention { get; protected set;}

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

        navagent = GetComponent<NavMeshAgent>();

        // Define all possible actions
        behaviors.Add("Wait", new Wait());
        behaviors.Add("Break", new Break());
        behaviors.Add("Quarrel", new Quarrel());
        behaviors.Add("Chat", new Chat());
        behaviors.Add("StudyAlone", new StudyAlone());
        behaviors.Add("StudyGroup", new StudyGroup());

        // Set the default action state to Wait
        currentAction = behaviors["Wait"];
        Desire = behaviors["Wait"];

        // Initiate Happiness and Energy

        energy = Math.Max(0.5f, random.Next(100)/100.0f); // with a value between [0.5, 1.0]
        happiness = Math.Max(-0.5f, 0.5f - random.Next(100)/100.0f); // with a value between [-0.5, 0.5]

        personality.extraversion = 0.9f;
    }

    void Start()
    {
        GR = GlobalRefs.Instance;
        Logger = GR.logger;
        classroom = GR.classroom;

        logInfo("Agent Personality: " + personality);

        //Agents AG = GameObject.Find("Agents").GetComponent<Agents>();
        logState();
    }

    public void interact(Agent source, AgentBehavior action)
    {
        pendingInteractions.Enqueue(new InteractionRequest(source, action));
    }

    // Log message as info
    public void logInfo(string message)
    {
        string[] msg = { gameObject.name, turnCnt.ToString(), "I", message };
        Logger.log(msg);
    }

    // Helper function logging Agent state
    private void logState()
    {
        logInfo(String.Format("Energy {0}, Happiness {1}, Action {2}", energy, happiness, currentAction));
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        turnCnt++;
        //string[] msg = { gameObject.name, "I", "My message" };
        //Logger.log(msg);
        updateAttention();

        evaluate_current_action();

        handle_interactions();

        logState();
    }

    // attention = f(State, Environment, Personality)
    private void updateAttention()
    {
        attention = Math.Max((1.0f - classroom.noise) * personality.conscientousness * energy, 0.0f);
    }


    private void handle_interactions()
    {
        while(pendingInteractions.Count > 0)
        {
            InteractionRequest iR = (InteractionRequest)pendingInteractions.Dequeue();
            logInfo(String.Format("Interaction Request from {0} for action {1}", iR.source, iR.action));
            if (iR.action is Chat)
            {
                if (currentAction is Chat) {
                    logInfo(String.Format("Agent is already chatting ..."));
                    continue;
                }
                if (Desire is Chat)
                {
                    logInfo(String.Format("Agent wanted to chat! Now he can do so with {0} ...", iR.source));
                    currentAction = behaviors["Chat"];
                }
                else
                {
                    float x = random.Next(100) / 100.0f;
                    if (x >= personality.conscientousness)
                    {
                        logInfo(String.Format("Agent got convinced by {0} to start chatting ...", iR.source));
                        currentAction = behaviors["Chat"];
                    }
                }
            }
        }
    }

    // Main Logic
    private void evaluate_current_action() 
    {
        int best_rating = -1000;
        int rating = best_rating;
        AgentBehavior best_action = null;
        AgentBehavior behavior = null;

        foreach (KeyValuePair<string, AgentBehavior> kvp in behaviors)
        {
            behavior = kvp.Value;
            rating = behavior.evaluate(this);
            if (behavior == currentAction)
            {
                // No need to extend Wait
                if (!(behavior is Wait)) {
                    int score_bias = (int)(SCORE_BIAS * Math.Exp(-(1.0f - personality.conscientousness) * (float)ticksOnThisTask));
                    rating += score_bias;
                }
            }

            logInfo(String.Format("Behavior: {0}, rating {1}", behavior.name, rating));
            if(rating > best_rating)
            {
                best_rating = rating;
                best_action = behavior;
            }
        }

        if (best_action != null)
        {
            Desire = best_action;
            if (best_action.possible(this))
            {
                if (best_action != currentAction)
                {
                    logInfo(String.Format("Starting new action {0}. Executing ...", best_action.name));
                    best_action.execute(this);
                    currentAction = best_action;
                    ticksOnThisTask = 0;
                }
                else
                {
                    best_action.execute(this);
                    ticksOnThisTask++;
                }
            }
            else
            {
                // Agent cannot perform Action
                logInfo(String.Format("{0} is not possible, executing wait instead! ...", best_action));
                currentAction = behaviors["Wait"];
                currentAction.execute(this);
            }
        }

    }
}
