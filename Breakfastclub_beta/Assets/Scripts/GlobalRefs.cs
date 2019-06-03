using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalRefs : MonoBehaviour
{
    // All References
    [SerializeField] public CSVLogger logger;
    [SerializeField] public Classroom classroom;
    [SerializeField] public Camera camera;

    private static GlobalRefs _instance;
    public static GlobalRefs Instance { get { return _instance; } }
    void Awake()
    {
        // Implement singelton pattern
        if (_instance != null && _instance != this)
            Destroy(this.gameObject);
        else
            _instance = this;

    }

    void Start()
    {
        // Get Singelton REFS
        logger = CSVLogger.Instance;
    }
    
}
