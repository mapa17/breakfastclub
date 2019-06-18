using System;
using System.Collections.Generic;
using UnityEngine;

public class Classroom : MonoBehaviour
{
    public float noise { get; protected set; }
    public Agent[] agents;

    [SerializeField] public Table[] groupTables;
    [SerializeField] public Table[] individualTables;

    [NonSerialized] public bool gamePaused = false;
    [NonSerialized] public Transform groundfloorTransform;
    // Start is called before the first frame update
    void Start()
    {
        noise = 0.0f;
        agents = FindObjectsOfType<Agent>();

        groundfloorTransform = transform.Find("Groundfloor").GetComponent<Transform>();
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
        // Reset noise
        noise = 0.0f;
        agents = FindObjectsOfType<Agent>();
        foreach(var agent in agents)
        {
            //Debug.Log("Found Agent " + agent + " at state " + agent.currentAction);
            updateNoise(agent.currentAction);
        }
    }

    private void updateNoise(AgentBehavior action)
    {
        noise = Math.Max(0.0f, Math.Min(1.0f, noise + action.noise_inc));
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
