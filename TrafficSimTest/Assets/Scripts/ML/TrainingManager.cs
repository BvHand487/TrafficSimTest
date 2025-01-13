using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.MLAgents.Policies;
using UnityEngine;

public class TrainingManager
{
    public List<TrafficLightAgent> agents;
    private BehaviorParameters behaviorParams;

    public TrainingManager(BehaviorParameters behaviorParams)
    {
        this.behaviorParams = behaviorParams;

        agents = new List<TrafficLightAgent>();
        GameObject[] junctions = GameObject.FindGameObjectsWithTag("Junction"); 
        foreach (var j in junctions)
        {
            var agent = j.GetComponent<TrafficLightAgent>();
            agents.Add(agent);
        }
    }

    public void StartTraining()
    {
        behaviorParams.BehaviorType = BehaviorType.Default; // Enable training mode
    }

    public void StopTraining()
    {
        behaviorParams.BehaviorType = BehaviorType.HeuristicOnly; // Stop training
    }

}