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
            behaviour.BehaviorType = BehaviorType.Default;
            behaviours.Add(behaviour);
        }
    }

    public void StartTraining()
    {
        foreach (var a in agents)
        {
            a.enabled = true;
        }
    }

    public void StopTraining()
    {
        foreach (var a in agents)
        {
            a.enabled = false;
        }
    }

}