using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class TrafficLightAgent : Agent
{
    public Simulation simulation;
    public TrafficController trafficController;

    public override void Initialize()
    {
        simulation = GameManager.Instance.simulations.simulation;
        trafficController = simulation.junctionsDict[gameObject].trafficController;
    }

    public override void OnEpisodeBegin()
    {
        trafficController.ResetLights();
    }

    //public override void CollectObservations(VectorSensor sensor)
    //{
    //    // Add traffic state observations (e.g., car queue lengths, light state)
    //    sensor.AddObservation(trafficLight.GetQueueLengths());

    //    // Add current traffic lights configuration
    //    sensor.AddObservation(trafficController.lights.Select(l => l.greenInterval).ToList());
    //}

    //public override void OnActionReceived(float[] vectorAction)
    //{
    //    // Define action logic (e.g., switch lights based on vectorAction)
    //    trafficLight.SetLightState((int)vectorAction[0]);
    //    SetReward(trafficLight.CalculateReward());

    //    // Penalty for time
    //    SetReward(-0.1f);
    //}

    //public override void Heuristic(float[] actionsOut)
    //{
    //    // Optional: Manual control for testing
    //    actionsOut[0] = 0; // Default to no action
    //}
}
