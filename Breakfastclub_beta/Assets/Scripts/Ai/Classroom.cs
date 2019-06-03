using System;
using System.Collections.Generic;
using UnityEngine;

public class Classroom : MonoBehaviour
{
    public float noise { get; protected set; }
    public Agent[] agents;

    [SerializeField] public Table[] groupTables;
    [SerializeField] public Table[] individualTables;

    // Start is called before the first frame update
    void Start()
    {
        noise = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        // Reset noise
        noise = 0.0f;
        agents = FindObjectsOfType<Agent>();
        foreach(var agent in agents)
        {
            Debug.Log("Found Agent " + agent + " at state " + agent.Action);
            updateNoise(agent.Action);
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
            if (agent.Action.Equals(filter))
            {
                available.Add(agent);
            }
        }
        return available;
    }
}
