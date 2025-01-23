using Generation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            behaviour.BehaviorType = BehaviorType.InferenceOnly;
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
    }

    public void StopTraining()
    {
        for (int i = 0; i < agents.Count; ++i)
        {
            behaviours[i].enabled = false;
            behaviours[i].BehaviorType = BehaviorType.InferenceOnly;
            agents[i].enabled = false;
        }
    }

    public void LoadModel(string path)
    {
        // if training -> stop training -> load .onnx from 'path'
    }

    public void SaveModel(string path)
    {
        // if training -> stop training -> go to temporary 'results' directory -> make copy of .onnx at 'path'
    }
}