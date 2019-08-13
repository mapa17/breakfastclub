using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Text;
using System.Linq;

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
    private int ticksOnThisTask;
    public double[] scores;

    [SerializeField] public int seed;
    [NonSerialized] public string studentname;

    private GlobalRefs GR;
    private CSVLogger Logger;
    public SimulationConfig SC;

    [HideInInspector] public Classroom classroom;
    [HideInInspector] public NavMeshAgent navagent;
    [HideInInspector] public double turnCnt = -1;

    public Personality personality { get; protected set; }

    public double happiness { get; set; }
    public double motivation { get; set; }
    public double attention { get; protected set;}
    private int lastMessagesIdx = 0;
    private string[] lastMessages = new string[5];
    private StringBuilder lastMessageStringBuilder = new StringBuilder();

    //private List<AgentBehavior> behaviors = new List<AgentBehavior>();
    private Dictionary<string, AgentBehavior> behaviors = new Dictionary<string, AgentBehavior>();
    public AgentBehavior currentAction { get; protected set; }
    public AgentBehavior Desire { get; protected set; }
    public AgentBehavior previousAction { get; protected set; }

    private Queue pendingInteractions = new Queue();

    public System.Random random;


    public void initAgent(string name, System.Random random, Personality personality)
    {
        this.random = random;
        this.personality = personality;
        studentname = name;

        navagent = GetComponent<NavMeshAgent>();
        //navagent.updatePosition = false;
        //navagent.updateRotation = false;


        GR = GlobalRefs.Instance;
        Logger = GR.logger;
        classroom = GR.classroom;
        SC = classroom.simulationConfig;

        // Define all possible actions
        behaviors.Add("Break", new Break(this));
        behaviors.Add("Quarrel", new Quarrel(this));
        behaviors.Add("Chat", new Chat(this));
        behaviors.Add("StudyAlone", new StudyAlone(this));
        behaviors.Add("StudyGroup", new StudyGroup(this));

        // Set the default action state to Break
        currentAction = behaviors["Break"];
        previousAction = null;
        Desire = behaviors["Break"];
        scores = new double[behaviors.Count];

        // Initiate Happiness and Motivation
        motivation = random.Next(100) / 100.0; // with a value between [0.0, 1.0]
        happiness = random.Next(100) / 100.0; // with a value between [0.0, 1.0]

        //personality.extraversion = 0.9f;
    }

    // Start is called before the first frame update
    private void OnEnable()
    {
    }

    void Start()
    {
        // Export Agent information using the normal logging system
        // Indicate this 'special' info by setting the turncounter to a negative (invalid) value
        turnCnt = -2;
        LogX(String.Format($"{studentname}|{personality.name}|||"), "S");
        turnCnt = -1;
        LogX(String.Format($"{personality.openess}|{personality.conscientousness}|{personality.extraversion}|{personality.agreeableness}|{personality.neuroticism}"), "S");

        turnCnt = 0;
        LogInfo("Agent Personality: " + personality);

        //Agents AG = GameObject.Find("Agents").GetComponent<Agents>();
        LogState();
    }

    //private void Update()
    //{
    //    //transform.position += navagent.desiredVelocity * Time.deltaTime;
    //    //navagent.nextPosition = transform.position;
    //}

    // MAIN LOGIC : Called at each iteration
    void FixedUpdate()
    {
        //Vector3 diff = navagent.nextPosition - transform.position;
        //if(diff.magnitude > 0.1)
        //{
        //    diff.Normalize();
        //    //diff.Scale(3.0);
        //    transform.Translate(diff);
        //}
        //else
        //{
        //    //transform.
        //}
        turnCnt++;

        LogState();

        EvaluateActions();

        HandleInteractions();

        UpdateHappiness();

        UpdateAttention();


        //GetComponent<Rigidbody>().velocity = navagent.desiredVelocity;


    }

    // Add given Agent and Action to event Queue
    public void Interact(Agent source, AgentBehavior action)
    {
        pendingInteractions.Enqueue(new InteractionRequest(source, action));
    }

    // Log message as info
    public void LogError(string message)
    {
        AppendToLastMessage(message);
        LogX(message, "E");
    }

    public void LogInfo(string message)
    {
        LogX(message, "I");
    }

    public void LogDebug(string message)
    {
        AppendToLastMessage(message);
        LogX(message, "D");
    }

    public void LogX(string message, string type)
    {
        string[] msg = { gameObject.name, turnCnt.ToString(), type, message };
        Logger.log(msg);
    }

    private void AppendToLastMessage(string message)
    {
        lastMessages[lastMessagesIdx % lastMessages.Length] = message;
        lastMessagesIdx++;
    }

    public string GetLastMessage()
    {
        lastMessageStringBuilder.Clear();
        for (int i = 0; i < lastMessages.Length; i++) {
            lastMessageStringBuilder.AppendLine(lastMessages[(lastMessagesIdx + i) % lastMessages.Length]);
        }
        return lastMessageStringBuilder.ToString();
    }

    // Helper function logging Agent state
    private void LogState(bool include_info_log=true)
    {
        if(include_info_log)
            LogX(String.Format($"Motivation {motivation} | Happiness {happiness} | Attenion {attention} | Action {currentAction} | Desire {Desire}"), "I");
        LogX(String.Format($"{motivation}|{happiness}|{attention}|{currentAction}|{Desire}"), "S");
    }

    public string GetStatus()
    {
        return String.Format("{0}\nMotivation {1} Happiness {2} Attenion {3}\nAction {4}\nDesire {5}", gameObject.name, motivation, happiness, attention, currentAction, Desire);
    }



    // attention = f(State, Environment, Personality)
    private void UpdateAttention()
    {
        // Attention is zero untill the agent is actively studying!
        attention = 0.0;
        if((currentAction is StudyAlone) || (currentAction is StudyGroup))
        {
            if (currentAction.state == AgentBehavior.ActionState.EXECUTING)
            {
                attention = AgentBehavior.boundValue(0.0, personality.conscientousness + motivation - classroom.noise*SC.Agent["ATTENTION_NOISE_SCALE"], 1.0);
            }
        }
    }

    // If current_action equals desire we are happy, sad otherwise
    private void UpdateHappiness()
    {
        // Do not touch happiness if in quarrel because it will be regulated by Action execution
        if (currentAction is Quarrel)
        {
            return;
        }

        double change;
        if(currentAction == Desire)
        {
            //change = HAPPINESS_INCREASE;
            change = SC.Agent["HAPPINESS_INCREASE"];
        }
        else
        {
            //change = -HAPPINESS_DECREASE * (1.0 - personality.neuroticism);
            change = -SC.Agent["HAPPINESS_INCREASE"] * (1.0 - personality.neuroticism);
        }
        happiness = AgentBehavior.boundValue(0.0, happiness + change, 1.0);
    }


    private bool StartAction(AgentBehavior newAction, bool setDesire=true, bool applyDefaultAction=true)
    {
        if (newAction.possible())
        {
            if (newAction != currentAction)
            {
                LogInfo(String.Format("Ending current action {0}.", currentAction.name));
                currentAction.end();
                previousAction = currentAction;

                LogInfo(String.Format("Starting new action {0}. Executing ...", newAction.name));
                bool success = newAction.execute();
                if (!success)
                    LogDebug(String.Format("Executing new action failed! Will continou anyways! ..."));
                currentAction = newAction;
                ticksOnThisTask = 0;

                // If we start a new action make this the desired action
                if (setDesire)
                {
                    Desire = newAction;
                }
            }
            else
            {
                // Continue to execute the current action
                newAction.execute();
                if(newAction.state == AgentBehavior.ActionState.EXECUTING)
                    ticksOnThisTask++;
            }
            return true;
        }
        else
        {
            // We want to execute this action, but cannot
            Desire = newAction;

            ticksOnThisTask = 0;

            if (applyDefaultAction)
            {
                // Agent cannot perform Action, go into Wait instead
                LogDebug(String.Format("{0} is not possible. Executing break instead! ...", newAction));
                currentAction.end();
                previousAction = currentAction;

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
            LogDebug(String.Format("Interaction Request from {0} for action {1}", iR.source, iR.action));
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

    public string GetScores()
    {
        StringBuilder sb = new StringBuilder();
        for (int actionidx = 0; actionidx < behaviors.Count; actionidx++)
        {
            AgentBehavior behavior = behaviors.Values.ElementAt(actionidx);
            sb.Append(String.Format($"{behavior.name}:{this.scores[actionidx]:N2} "));
        }
        return sb.ToString();
    }

    // Main Logic
    private void EvaluateActions() 
    {
        double rating = 0;
        AgentBehavior best_action = null;
        AgentBehavior behavior = null;

        // Agents high on consciousness will stick longer to chosen actions
        // Look at:
        // https://www.wolframalpha.com/input/?i=plot+3.0+*+e**(-(1.0-0.3)*x)+from+x%3D0+to+5
        double score_bias = 0;
        //score_bias = (int)(ACTION_SCORE_BIAS * Math.Exp(-(1.0 - personality.conscientousness) * (float)ticksOnThisTask));
        score_bias = (int)(SC.Agent["ACTION_SCORE_BIAS"] * Math.Exp(-(1.0 - personality.conscientousness) * (float)ticksOnThisTask));

        for (int actionidx=0; actionidx < behaviors.Count; actionidx++)
        {
            behavior = behaviors.Values.ElementAt(actionidx);
            rating = behavior.rate();

            // The current action gets a score boost that declines exponetially
            if (behavior == currentAction)
            {
                rating += score_bias;
            }

            if(behavior == previousAction)
            {
                rating -= score_bias;
            }
            scores[actionidx] = rating;
        }
        LogInfo("Scores: " + GetScores());

        // Calculate combination of individual and peer aciton score
        double[] joinedscore;
        joinedscore = scores.Zip(classroom.peerActionScores, (x, y) => x * (1.0 - SC.Agent["PEER_PRESSURE_COMPLIANCE"]) + y * (SC.Agent["PEER_PRESSURE_COMPLIANCE"])).ToArray();

        // Chose action based on score
        int chosen_action = 0; 
        int prob_action = ChooseActionByDistribution(scores);
        int max_action = System.Array.IndexOf(scores, scores.Max());
        chosen_action = prob_action;

        best_action = behaviors.Values.ElementAt(chosen_action);

        if (best_action != null)
        {
            bool success = StartAction(best_action);
            if(success)
                LogInfo(String.Format("Starting Action {0}.", best_action));
            else
                LogInfo(String.Format("Starting Action {0} failed!", best_action));
        }

    }

    // Return the index of an action in score, based on its probability/ratio of the score
    // This is implemented by generating an array filled with action indexes
    // The number of entries for an action is defined by the score
    // One element of that array is chosen randomly (uniform) so that the probability of select and action is equal to its score (normalized by the sum of all scores)
    private int ChooseActionByDistribution(double[] ratings)
    {
        double sum = 0;
        for(int action=0; action < ratings.Length; action++){
            if(ratings[action] > 0)
            {
                sum += ratings[action]*ratings[action];
            }
        }
        int[] distribution = new int[100];

        int counter = 0;
        for (int action = 0; action < ratings.Length; action++)
        {
            if (ratings[action] > 0)
            {
                int normalized_rating = (int)(((ratings[action] * ratings[action]) / sum) * 100.0);
                for (int i = 0; i < normalized_rating; i++)
                {
                    distribution[counter + i] = action;
                }
                counter += normalized_rating;
            }
        }

        // Chose a random element from the action distribution
        return distribution[random.Next(counter)];
    }
}
