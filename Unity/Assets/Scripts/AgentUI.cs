using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using System;

public class AgentUI : MonoBehaviour
{
    // Idle 0
    // Walking/Break 1
    // Chat 2
    // Quarrel 3
    // Study 4
    public enum AnimationState : int { Idle=0, Walking, Chat, Quarrel, Study};

    public bool isFront;
    public AnimationState animationstate;
    public float distanceMoved;

    private NavMeshAgent navAgent;
    private Camera cam;
    private Agent agent;
    private Canvas UICanvas;
    private AgentStatsTooltip statsTooltip;
    private Animator agentAnimator;
    private Animator bubbleAnimator;

    //private bool showStats = false;
    private TMPro.TextMeshPro AgentNameText;

    private Vector3 prevPosition;

    // Start is called before the first frame update
    void Start()
    {
        navAgent = gameObject.GetComponent<NavMeshAgent>();
        cam = FindObjectOfType<Camera>();
        agent = gameObject.GetComponent<Agent>();
        agentAnimator = transform.Find("AgentAnimation").gameObject.GetComponent<Animator>();
        bubbleAnimator = transform.Find("BubbleAnimation").gameObject.GetComponent<Animator>();

        UICanvas = FindObjectOfType<Canvas>();
        statsTooltip = UICanvas.transform.Find("AgentStatsTooltip").GetComponent<AgentStatsTooltip>();
        AgentNameText = transform.Find("NameText").GetComponent<TMPro.TextMeshPro>();

        AgentNameText.SetText(agent.studentname);
    }

    // Update is called once per frame
    void Update()
    {
        SetAnimationState();
    }

    // Decide animation based on agent.currentAction
    // If agents are too far away from their navAgent destination their animation will be walking
    private void SetAnimationState()
    {
        distanceMoved = Vector3.Distance(transform.position, prevPosition);
        if(distanceMoved > 0.02)
        {
            animationstate = AnimationState.Walking;
            isFront = !((transform.position - prevPosition).x < 0.02);
        }
        else
        {
            isFront = true;
            if ((agent.currentAction is Quarrel) && (agent.currentAction.state == AgentBehavior.ActionState.EXECUTING))
            {
                animationstate = AnimationState.Quarrel;
            }
            else
            if (agent.currentAction is Break)
            {
                //animationstate = AnimationState.Walking;
                animationstate = AnimationState.Idle;
            }
            else
            if (((agent.currentAction is StudyAlone) || (agent.currentAction is StudyGroup)) && (agent.currentAction.state == AgentBehavior.ActionState.EXECUTING))
            {
                animationstate = AnimationState.Study;
            }
            else
            if ((agent.currentAction is Chat) && (agent.currentAction.state == AgentBehavior.ActionState.EXECUTING))
            {
                animationstate = AnimationState.Chat;
            }
            else
            {
                animationstate = AnimationState.Idle;
            }
        }
        //isFront = (navAgent.destination - transform.position).z < 0.5;
        //isFront = !((transform.position - prevPosition).z > 0.01) || ((transform.position - prevPosition).x > 0.01);


        //isFront = true;
        prevPosition = transform.position;


        agentAnimator.SetInteger("AgentAnimationState", (int)animationstate);
        agentAnimator.SetBool("IsFront", isFront);
        bubbleAnimator.SetInteger("AgentAnimationState", (int)animationstate);

        /*
        if ((agent.currentAction is Quarrel) && (agent.currentAction.state == AgentBehavior.ActionState.EXECUTING))
        {
            if (distanceToDestination < 2.0)
            {
                animationstate = AnimationState.Quarrel;
            } else { animationstate = AnimationState.Walking; }
        } else 
        if (agent.currentAction is Break)
        {
            animationstate = AnimationState.Walking;
            //animationstate = AnimationState.Idle;
        } else 
        if (((agent.currentAction is StudyAlone) || (agent.currentAction is StudyGroup)) && (agent.currentAction.state == AgentBehavior.ActionState.EXECUTING))
        {
            if (distanceToDestination < 1.0)
            {
                animationstate = AnimationState.Study;
            }
            else { animationstate = AnimationState.Walking; }
        } else 
        if ((agent.currentAction is Chat) && (agent.currentAction.state == AgentBehavior.ActionState.EXECUTING) )
        {
            if (distanceToDestination < 2.0)
            {
                animationstate = AnimationState.Chat;
            }
            else { animationstate = AnimationState.Walking; }
        }
        else
        {
            if (distanceToDestination < 1.0)
            { animationstate = AnimationState.Idle; }
            else { animationstate = AnimationState.Walking; }
        }*/
    }

    void OnMouseDown()
    {
        //return;
        //If your mouse hovers over the GameObject with the script attached, output this message
        //Debug.Log(string.Format("OnMouseDown GameObject {0}.", this.name));
        if(statsTooltip.agent == agent)
        {
            //Debug.Log(string.Format("Dissable stats."));
            statsTooltip.SetAgent(null);
        }
        else
        {
            //Debug.Log(string.Format("Enable stats."));
            statsTooltip.SetAgent(agent);
        }   
    }
}
