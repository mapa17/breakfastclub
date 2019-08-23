using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject characterPrefab;

    [SerializeField]
    private string[] names;
    public Sprite[] sprites;


    public GameObject SpawnAgent(System.Random random, Personality personality)
    {
        // Get a rando position on the groundfloor
        Vector3 position = GlobalRefs.Instance.classroom.groundfloorTransform.TransformPoint((random.Next(200)-100) / 100.0f, 0.0f, (random.Next(200) - 100) / 100.0f);
        position.Scale(new Vector3(3.0f, 1.0f, 3.0f));

        // Instantiate agent, call agent constructor
        GameObject newAgent = Instantiate(characterPrefab, position, Quaternion.identity);
        Agent agent = newAgent.GetComponent<Agent>();
        agent.initAgent(names[random.Next(names.Length)], random, personality);

        //newAgent.transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite = sprites[random.Next(sprites.Length)];

        return newAgent;
    }
}
