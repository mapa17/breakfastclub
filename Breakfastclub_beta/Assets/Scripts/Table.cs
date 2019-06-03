using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Table : MonoBehaviour
{
    [SerializeField] public Transform[] seats;
    private bool[]taken_seat;
    private Agent[] agents;
    GlobalRefs GR;
    CSVLogger Logger;

    // Start is called before the first frame update
    void Start()
    {
        taken_seat = new bool[seats.Length];
        agents = new Agent[seats.Length];

        GR = GlobalRefs.Instance;
        Logger = GR.logger;
        /*
        for(int i=seats.Length-1; i>=0; i--)
        {
            taken_seat[i] = false;
            agents[i] = null;
        }*/
    }

    // Log message as info
    private void logInfo(string message)
    {
        string[] msg = { gameObject.name, "I", message };
        Logger.log(msg);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Check if any 'seated' Agents changed to another action
        for (int i = seats.Length - 1; i >= 0; i--)
        {
            Agent agent = agents[i];
            if (agent != null)
            {
                if((agent.currentAction is StudyAlone) || (agent.currentAction is StudyAlone))
                {
                    //Still seated
                }
                else
                {
                    logInfo("Agent " + agent + " returns seat!");
                    taken_seat[i] = false;
                    agents[i] = null;
                }
            }
        }
    }

    public bool freeSpot()
    {
        for (int i = seats.Length - 1; i >= 0; i--)
        {
            if (!taken_seat[i])
            {
                return true;
            }
        }
        return false;
    }

    public Transform takeSeat(Agent agent)
    {
        for (int i = seats.Length - 1; i >= 0; i--)
        {
            if (!taken_seat[i])
            {
                taken_seat[i] = true;
                agents[i] = agent;
                return seats[i];
            }
        }
        return null;
    }
}
