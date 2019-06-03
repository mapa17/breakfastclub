using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Agent : MonoBehaviour
{
    private readonly int SCORE_BIAS = 50;
    private int ticksOnThisTask;
     
    private GlobalRefs GR;
    private CSVLogger Logger;
    public Classroom classroom;
    public NavMeshAgent navagent;

    public Personality personality { get; protected set; }

    public float happiness { get; set; }
    public float energy { get; set; }
    public float attention { get; protected set;}

    private List<AgentBehavior> behaviors = new List<AgentBehavior>();
    public AgentBehavior currentAction { get; protected set; }
    public AgentBehavior Desire { get; protected set; }

    // Start is called before the first frame update
    private void OnEnable()
    {
        // Create a personality for this agent
        personality = new Personality();

        navagent = GetComponent<NavMeshAgent>();

        // Define all possible actions
        behaviors.Add(new Wait());
        behaviors.Add(new Break());
        behaviors.Add(new Quarrel());
        behaviors.Add(new Chat());
        behaviors.Add(new StudyAlone());
        behaviors.Add(new StudyGroup());

        // Set the default action state to Wait
        currentAction = behaviors[0];
        Desire = behaviors[0];

        // Initiate Happiness and Energy
        System.Random random = new System.Random();
        energy = Math.Max(0.5f, random.Next(100)/100.0f); // with a value between [0.5, 1.0]
        happiness = Math.Max(-0.5f, 0.5f - random.Next(100)/100.0f); // with a value between [-0.5, 0.5]
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

    // Log message as info
    private void logInfo(string message)
    {
        string[] msg = { gameObject.name, "I", message };
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
        //string[] msg = { gameObject.name, "I", "My message" };
        //Logger.log(msg);
        updateAttention();
        evaluate_current_action();

        logState();
    }

    // attention = f(State, Environment, Personality)
    private void updateAttention()
    {
        attention = Math.Max((1.0f - classroom.noise) * personality.conscientousness * energy, 0.0f);
    }

    // Main Logic
    private void evaluate_current_action() 
    {
        int best_rating = -1000;
        int rating = best_rating;
        AgentBehavior best_action = null;

        foreach (AgentBehavior behavior in behaviors)
        {

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

    }
}
