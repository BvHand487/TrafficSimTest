using System.Collections.Generic;
using Unity.MLAgents.Policies;
using UnityEngine;

public class TrainingManager : SingletonMonobehaviour<TrainingManager>
{
    [SerializeField] public bool timeDependentTraffic = false;
    [SerializeField] public bool twoModeJunctions = false;
    [SerializeField] public float episodeLength = 300.0f;  // in seconds simulated time

    private List<TrafficAgent> agents;
    private List<BehaviorParameters> behaviours;
    private string trainingId;

    public bool isTraining = false;
    private PythonBackendManager pythonBackend;

    public override void Awake()
    {
        base.Awake();

        pythonBackend = PythonBackendManager.Instance;
    }

    public void Start()
    {
        LoadAgents();
        LoadBehaviours();

        if (enabled && pythonBackend.enabled)
            trainingId = pythonBackend.StartMLAgents();
    }

    private void OnApplicationQuit()
    {
        if (pythonBackend.IsMLAgentsRunning())
            pythonBackend.StopMLAgents();
    }

    public void LoadAgents()
    {
        agents = new List<TrafficAgent>();

        GameObject[] junctions = GameObject.FindGameObjectsWithTag("Junction");
        foreach (var j in junctions)
        {
            var agent = j.GetComponentInChildren<TrafficAgent>();
            agents.Add(agent);
        }
    }

    public void LoadBehaviours()
    {
        behaviours = new List<BehaviorParameters>();

        GameObject[] junctions = GameObject.FindGameObjectsWithTag("Junction");
        foreach (var j in junctions)
        {
            var behaviour = j.GetComponentInChildren<BehaviorParameters>();
            behaviour.BehaviorType = BehaviorType.HeuristicOnly;
            behaviours.Add(behaviour);
        }
    }

    public void StartTraining()
    {
        for (int i = 0; i < agents.Count; ++i)
            behaviours[i].BehaviorType = BehaviorType.Default;

        isTraining = true;
    }

    public void StopTraining()
    {
        for (int i = 0; i < agents.Count; ++i)
            behaviours[i].BehaviorType = BehaviorType.HeuristicOnly;

        isTraining = false;
    }

    public void LoadModel(string path)
    {
        // ...
    }

    public void SaveModel(string path)
    {
        // if training -> stop training -> go to temporary 'results' directory -> make copy of .onnx at 'path'
    }
}