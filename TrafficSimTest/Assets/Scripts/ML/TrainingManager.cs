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
    public List<BehaviorParameters> behaviours;

    public TrainingManager()
    {
        agents = new List<TrafficLightAgent>();
        behaviours = new List<BehaviorParameters>();

        GameObject[] junctions = GameObject.FindGameObjectsWithTag("Junction"); 
        foreach (var j in junctions)
        {
            var agent = j.GetComponent<TrafficLightAgent>();
            agent.SetTraining(false);
            agents.Add(agent);
            
            var behaviour = j.GetComponent<BehaviorParameters>();
            behaviour.BehaviorType = BehaviorType.Default;
            behaviours.Add(behaviour);
        }
    }

    public void StartTraining()
    {
        foreach (var a in agents)
        {
            a.SetTraining(true);
        }
    }

    public void StopTraining()
    {
        foreach (var a in agents)
        {
            a.SetTraining(false);
        }
    }

}