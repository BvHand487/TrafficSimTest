using System.Collections.Generic;
using Unity.MLAgents.Policies;
using UnityEngine;

public class TrainingManager : SingletonMonobehaviour<TrainingManager>
{
    [SerializeField] public bool timeDependentTraffic = false;
    [SerializeField] public bool twoPhaseJunctions = false;
    [SerializeField] public float episodeLength = 300.0f;  // in seconds simulated time

    private List<TrafficLightAgent> agents;
    private List<BehaviorParameters> behaviours;
    private string trainingId;

    private bool isTraining = false;
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
        agents = new List<TrafficLightAgent>();

        GameObject[] junctions = GameObject.FindGameObjectsWithTag("Junction");
        foreach (var j in junctions)
        {
            var agent = j.GetComponentInChildren<TrafficLightAgent>();
            agent.enabled = false;
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
            behaviour.enabled = false;
            behaviour.BehaviorType = BehaviorType.HeuristicOnly;
            behaviours.Add(behaviour);
        }
    }

    public void StartTraining()
    {
        for (int i = 0; i < agents.Count; ++i)
        {
            behaviours[i].enabled = true;
            behaviours[i].BehaviorType = BehaviorType.Default;
            agents[i].enabled = true;
        }

        isTraining = true;
    }

    public void StopTraining()
    {
        for (int i = 0; i < agents.Count; ++i)
        {
            behaviours[i].enabled = false;
            behaviours[i].BehaviorType = BehaviorType.HeuristicOnly;
            agents[i].enabled = false;
        }

        isTraining = false;
    }

    public void LoadModel(string path)
    {
        if (isTraining)
            StopTraining();

        return;
    }

    public void SaveModel(string path)
    {
        // if training -> stop training -> go to temporary 'results' directory -> make copy of .onnx at 'path'
    }
}