using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Rendering;

public class TrafficLightAgent : Agent
{
    private VehicleManager vehicleManager;
    private TrainingManager trainingManager;
    private TrafficController trafficController;

    private float elapsedTime = 0f;

    protected override void Awake()
    {
        base.Awake();

        trainingManager = TrainingManager.Instance;
        trafficController = GetComponent<TrafficController>();
    }

    public void Start()
    {
        vehicleManager = trafficController.junction.simulation.vehicleManager;
    }

    public override void OnEpisodeBegin()
    {
        vehicleManager.ClearVehicles();
        trafficController.ResetLights();
        elapsedTime = 0f;
}

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        if (elapsedTime > trainingManager.episodeLength)
            EndEpisode();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Current max vehicles on the road
        sensor.AddObservation(trafficController.junction.simulation.vehicleManager.simulatedMaxVehicleCount);

        // Lights status (0: red, 1: green)
        sensor.AddObservation(trafficController.lights.Select(l => l.status == TrafficLight.Status.Green ? 1f : 0f).ToList());
        if (trafficController.lights.Count == 3)
            sensor.AddObservation(0f); // Dummy value for 3-way junctions

        // Elapsed time since last light change
        sensor.AddObservation(trafficController.elapsedTime);

        // Get vehicle queue lengths ? (not sure)
        //sensor.AddObservation(trafficController.lights.Select(l => (float) l.queue.Count()).ToList());

        // Get vehicles waiting at the junction right now
        sensor.AddObservation(trafficController.lights.Select(l => (float) l.queueLength).Sum());

        // Current lights mode (0: single, 1: double)
        if (trafficController.mode == TrafficController.Mode.Single)
            sensor.AddObservation(0);
        else
            sensor.AddObservation(1);

        // Current lights green intervals
        sensor.AddObservation(trafficController.lights.Select(l => l.greenInterval).ToList());
        if (trafficController.lights.Count == 3)
            sensor.AddObservation(0f); // Dummy value for 3-way junctions
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Define the min, max green interval duration and step size
        float minGreenInterval = 5.0f;
        float maxGreenInterval = 30.0f;
        float stepSize = 0.5f;

        // Number of traffic lights in the simulation
        int numLights = trafficController.lights.Count;

        // Get the continuous and discrete action buffers
        var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;

        //// Ensure the number of continuous actions matches the number of traffic lights
        //if (continuousActions.Length != numLights + 1)
        //{
        //    Debug.LogError("Training: Invalid number of continuous actions received.");
        //    return;
        //}

        //// Ensure there is exactly one discrete action for mode selection
        //if (discreteActions.Length != 1)
        //{
        //    Debug.LogError("Training: Invalid number of discrete actions received.");
        //    return;
        //}


        List<float> scaledIntervals = new List<float>();

        // Configure green intervals for each traffic light
        for (int i = 0; i < numLights; i++)
        {
            // The raw action for the green interval
            float rawGreenInterval = continuousActions[i];

            // Scale and clamp the raw action to the allowed range and step size
            float scaledGreenInterval = Mathf.Clamp(
                Mathf.Round(rawGreenInterval / stepSize) * stepSize,
                minGreenInterval,
                maxGreenInterval
            );

            scaledIntervals.Add(scaledGreenInterval);
        }

        // Configure the traffic control mode
        int modeAction = discreteActions[0]; // Last action is discrete
        if (modeAction == 0)
        {
            trafficController.ConfigureLights(scaledIntervals, TrafficController.Mode.Single);
        }
        else if (modeAction == 1)
        {
            trafficController.ConfigureLights(scaledIntervals, TrafficController.Mode.Double);
        }
        else
        {
            Debug.LogWarning("Training: Invalid traffic mode size received.");
        }

        // Apply a small penalty for time spent (encouraging efficiency)
        SetReward(-0.1f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Define the min, max green interval duration and step size
        float minGreenInterval = 5.0f;
        float maxGreenInterval = 30.0f;

        var continuousActions = actionsOut.ContinuousActions;
        var discreteActions = actionsOut.DiscreteActions;

        for (int i = 0; i < trafficController.lights.Count; i++)
            continuousActions[i] = Mathf.Clamp(20.0f, minGreenInterval, maxGreenInterval);

        // double mode
        discreteActions[0] = 1;
    }
}
