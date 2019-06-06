using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;

public class CSVLogger : MonoBehaviour
{
    [SerializeField] private string logfile = "Log.csv";

    // Configure CSV Logger
    [SerializeField] private string SEP = ",";
    [SerializeField] private string[] HEADER = { "Time", "Tag", "Turn", "Type", "Message" };

    // Internal
    private StreamWriter sw;
    private StringBuilder sb = new StringBuilder();


    private static CSVLogger _instance;
    public static CSVLogger Instance { get { return _instance; } }
    void Awake()
    {
        // Implement singelton pattern
        if (_instance != null && _instance != this)
            Destroy(this.gameObject);
        else
            _instance = this;

        // Open Log file
        try
        {
            sw = new StreamWriter(logfile);
            log(HEADER, include_time: false);
            Debug.Log("Wrirting logfile to " + logfile);
        }
        catch
        {
            Debug.Log("<color=red>Error: Could not write to Logfile " + logfile + "</color>");
        }
    }

    public void log(string[] line, bool include_time=true)
    {
        sb.Clear();
        if (include_time) sb.Append(Time.time.ToString() + SEP);
        foreach (string msg in line)
        {
            sb.Append(msg + SEP);
        }
        sw.WriteLine(sb.ToString());
    }
    // Update is called once per frame
    void Update()
    {

        //string[] message = { "MyTag", "I", "Some message" };
        //log(message);
    }

    private void OnDestroy()
    {
        sw.Close();
        Debug.Log("Stopping logger!");
    }
}
