using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class AgentUI : MonoBehaviour
{
    private NavMeshAgent navAgent;
    private Camera cam;
    private Agent agent;
    private Canvas UICanvas;
    private AgentStatsTooltip statsTooltip;

    //private bool showStats = false;
    private TMPro.TextMeshPro AgentNameText;

    // Start is called before the first frame update
    void Start()
    {
        navAgent = gameObject.GetComponent<NavMeshAgent>();
        cam = FindObjectOfType<Camera>();
        agent = gameObject.GetComponent<Agent>();

        UICanvas = FindObjectOfType<Canvas>();
        statsTooltip = UICanvas.transform.Find("AgentStatsTooltip").GetComponent<AgentStatsTooltip>();
        AgentNameText = transform.Find("NameText").GetComponent<TMPro.TextMeshPro>();

        AgentNameText.SetText(agent.studentname);
    }

    // Update is called once per frame
    void Update()
    {
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
