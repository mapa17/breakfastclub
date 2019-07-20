using System;
using UnityEngine;
using UnityEngine.UI;


public class AgentStatsTooltip : MonoBehaviour
{

    [NonSerialized] public Text TitleText;
    [NonSerialized] public Text NameText;
    [NonSerialized] public Text GoNameText;
    [NonSerialized] public Text EnergyText;
    [NonSerialized] public Text HappinessText;
    [NonSerialized] public Text AttentionText;
    [NonSerialized] public Text PersonalityText;
    [NonSerialized] public Text ScoreText;
    [NonSerialized] public Text LogText;
    [NonSerialized] public Text ActionText;
    [NonSerialized] public Text DesireText;

    public Vector3 position_offset;

    public Agent agent { protected set; get; }
    
    // Start is called before the first frame update
    void Start()
    {
        TitleText = transform.Find("TitleText").GetComponent<Text>();
        NameText = transform.Find("NameText").GetComponent<Text>();
        GoNameText = transform.Find("GoNameText").GetComponent<Text>();
        EnergyText = transform.Find("MotivationText").GetComponent<Text>();
        HappinessText = transform.Find("HappinessText").GetComponent<Text>();
        AttentionText = transform.Find("AttentionText").GetComponent<Text>();
        PersonalityText = transform.Find("PersonalityText").GetComponent<Text>();
        ScoreText = transform.Find("ScoreText").GetComponent<Text>();
        LogText = transform.Find("LogText").GetComponent<Text>();
        ActionText = transform.Find("ActionText").GetComponent<Text>();
        DesireText = transform.Find("DesireText").GetComponent<Text>();
    }

    public void SetAgent(Agent newAgent)
    {
        agent = newAgent;
        if (agent)
        {
            gameObject.SetActive(true);
        }
        else { gameObject.SetActive(false); }
    }

    // Update is called once per frame
    void Update()
    {
        if(agent)
        {
            transform.position = agent.transform.position + position_offset;
            //Debug.Log("Plotting" + transform.position);
            NameText.text = agent.studentname;
            GoNameText.text = agent.name;
            EnergyText.text = agent.motivation.ToString("0.00");
            HappinessText.text = agent.happiness.ToString("0.00");
            AttentionText.text = agent.attention.ToString("0.00");
            PersonalityText.text = agent.personality.ToString();
            ScoreText.text = agent.GetScores();
            LogText.text = agent.lastMessage;
            ActionText.text = agent.currentAction.ToString();
            DesireText.text = agent.Desire.ToString();
        }
    }
}
