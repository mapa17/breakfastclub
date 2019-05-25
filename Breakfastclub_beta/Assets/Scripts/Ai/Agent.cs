using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    private GlobalRefs GR;
    private CSVLogger Logger;

    private Action.States action;
    private Personality personality;

    public float happiness { get; protected set; }
    public float energy { get; protected set; }

    // Start is called before the first frame update
    void Start()
    {
        GR = GlobalRefs.Instance;
        Logger = GR.logger;

        Agents AG = GameObject.Find("Agents").GetComponent<Agents>();

        personality = new Personality();
        action = Action.States.Wait;
    }


    // Update is called once per frame
    void Update()
    {
        //string[] msg = { gameObject.name, "I", "My message" };
        //Logger.log(msg);
    }
}
