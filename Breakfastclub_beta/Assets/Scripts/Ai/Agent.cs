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
        behaviors.Add("Wait", new Wait(this));
        behaviors.Add("Break", new Break(this));
        behaviors.Add("Quarrel", new Quarrel(this));
        behaviors.Add("Chat", new Chat(this));
        behaviors.Add("StudyAlone", new StudyAlone(this));
        behaviors.Add("StudyGroup", new StudyGroup(this));

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
        logX(message, "I");
    }

    public void logDebug(string message)
    {
        logX(message, "D");
    }

    public void logX(string message, string type)
    {
        string[] msg = { gameObject.name, turnCnt.ToString(), type, message };
        Logger.log(msg);
    }

    // Helper function logging Agent state
    private void logState()
    {
        logInfo(String.Format("Energy {0} Happiness {1} Action {2}", energy, happiness, currentAction));
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        turnCnt++;

        logState();
        //string[] msg = { gameObject.name, "I", "My message" };
        //Logger.log(msg);
        updateAttention();

        evaluate_current_action();

        handle_interactions();
    }

    // attention = f(State, Environment, Personality)
    private void updateAttention()
    {
        attention = Math.Max((1.0f - classroom.noise) * personality.conscientousness * energy, 0.0f);
    }


    private bool startAction(AgentBehavior newAction)
    {
        if (newAction.possible())
        {
            if (newAction != currentAction)
            {
                currentAction.end();

                logInfo(String.Format("Starting new action {0}. Executing ...", newAction.name));
                bool success = newAction.execute();
                if (!success)
                    logInfo(String.Format("Executing new action failed! Will continou anyways! ..."));
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
            // Agent cannot perform Action
            logInfo(String.Format("{0} is not possible. Executing wait instead! ...", newAction));
            currentAction = behaviors["Wait"];
            currentAction.execute();
            return false;
        }
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
                    logDebug(String.Format("Agent wanted to chat! Now he can do so with {0} ...", iR.source));
                    Chat chat = (Chat)behaviors["Chat"];
                    chat.acceptInviation(iR.source);
                    startAction(chat);
                }
                else
                {
                    // An agent is convinced to chat based on its conscientousness trait.
                    // Agents high on consciousness are more difficult to convince/distract
                    float x = random.Next(100) / 100.0f;
                    if (x >= personality.conscientousness)
                    {
                        logDebug(String.Format("Agent got convinced by {0} to start chatting ...", iR.source));
                        Chat chat = (Chat)behaviors["Chat"];
                        chat.acceptInviation(iR.source);
                        startAction(chat);
                    } 
                    else
                    {
                        logDebug(String.Format("Agent keeps to current action ({0} < {1})", x, personality.conscientousness));
                    }
                }
            }
        }
    }

    // Main Logic
    private void evaluate_current_action() 
    {
        StringBuilder sb = new StringBuilder();

        int best_rating = -1000;
        int rating = best_rating;
        AgentBehavior best_action = null;
        AgentBehavior behavior = null;

        foreach (KeyValuePair<string, AgentBehavior> kvp in behaviors)
        {
            behavior = kvp.Value;
            rating = behavior.evaluate();
            if (behavior == currentAction)
            {
                // No need to extend Wait
                if (!(behavior is Wait)) {
                    int score_bias = (int)(SCORE_BIAS * Math.Exp(-(1.0f - personality.conscientousness) * (float)ticksOnThisTask));
                    rating += score_bias;
                }
            }

            //logInfo(String.Format("Behavior: {0} rating {1}", behavior.name, rating));
            sb.Append(String.Format("{0}:{1} ", behavior.name, rating));
            if (rating > best_rating)
            {
                best_rating = rating;
                best_action = behavior;
            }
        }
        logInfo("Behavior: " + sb.ToString());

        if (best_action != null)
        {
            Desire = best_action;
            bool success = startAction(best_action);
            if(success)
                logInfo(String.Format("Starting Action {0}.", best_action));
            else
                logInfo(String.Format("Starting Action {0} failed! Will continou anyways! ...", best_action));
        }

    }
}
