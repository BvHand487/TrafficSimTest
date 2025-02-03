using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEditor.MemoryProfiler;
using UnityEngine;

public class TrafficLightAgent : Agent
{
    private BehaviorParameters parameters;
    private TrainingManager trainingManager;

    private VehicleManager vehicleManager;
    private TrafficController trafficController;
    public CongestionTracker tracker;
    private List<TrafficLightAgent> neighbours;

    private float previousCongestion;
    private float previousReward;
    private float timeElapsed;

    protected override void Awake()
    {
        base.Awake();

        parameters = GetComponent<BehaviorParameters>();
        trainingManager = TrainingManager.Instance;

        trafficController = GetComponent<TrafficController>();
        tracker = GetComponent<CongestionTracker>();
        neighbours = new List<TrafficLightAgent>();
    }

    public void Start()
    {
        vehicleManager = trafficController.junction.simulation.vehicleManager;

        var thisJunction = trafficController.junction;
        neighbours = thisJunction.roads
            .Select(r => r.GetOtherJunction(thisJunction))
            .Where(j => j != null && j != thisJunction)
            .Distinct()
            .Select(neighbourJunction => neighbourJunction.GetComponent<TrafficLightAgent>())
            .ToList();

        
        parameters.BrainParameters.VectorObservationSize = 3 + neighbours.Count * 1;

        var actionParams = parameters.BrainParameters.ActionSpec;
        actionParams.NumContinuousActions = trafficController.lights.Count;

        if (trainingManager.twoPhaseJunctions)
            actionParams.BranchSizes = new int[] { 2 };
        else
            actionParams.BranchSizes = null;

        parameters.BrainParameters.ActionSpec = actionParams;


        timeElapsed = 0f;
    }

    public override void OnEpisodeBegin()
    {   
        if (!vehicleManager)
            vehicleManager = trafficController.junction.simulation.vehicleManager;

        tracker.ResetValues();
        vehicleManager.ClearVehicles();
        trafficController.ResetLights();
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed > trainingManager.episodeLength)
            EndEpisode();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (!tracker.ReadyToReport() || neighbours.Any(n => !n.tracker.ReadyToReport()))
            return;

        sensor.AddObservation(tracker.GetCumulativeCongestion());
        sensor.AddObservation(previousCongestion);
        sensor.AddObservation(previousReward);

        foreach (var agent in neighbours)
        {
            sensor.AddObservation(agent.tracker.GetCumulativeCongestion());
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        ReflectActions(actionBuffers);
        EvaluateRewards();
    }

    public void ReflectActions(ActionBuffers actionBuffers)
    {
        // define the min, max green interval duration and step size
        float minGreenInterval = 5.0f;
        float maxGreenInterval = 30.0f;
        float stepSize = 0.5f;

        var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;

        List<float> scaledIntervals = new List<float>();

        for (int i = 0; i < trafficController.lights.Count; i++)
        {
            float rawGreenInterval = continuousActions[i];

            float scaledGreenInterval = Mathf.Clamp(
                Mathf.Round(rawGreenInterval / stepSize) * stepSize,
                minGreenInterval,
                maxGreenInterval
            );

            scaledIntervals.Add(scaledGreenInterval);
        }

        if (trainingManager.twoPhaseJunctions)
        {
            if (discreteActions[0] == 0)
                trafficController.ConfigureLights(scaledIntervals, TrafficController.Mode.Single);

            if (discreteActions[0] == 1)
                trafficController.ConfigureLights(scaledIntervals, TrafficController.Mode.Double);
        }
        else
            trafficController.ConfigureLights(scaledIntervals, TrafficController.Mode.Double);

        SetReward(-0.1f);
    }

    public void EvaluateRewards()
    {
        var congestion = tracker.GetCumulativeCongestion();
        var neighboursCongestion = neighbours.Select(n => tracker.GetCumulativeCongestion()).Sum();

        var totalCongestion = congestion + neighboursCongestion;

        // Calculate the reward (e.g., minimize congestion)
        float reward = -congestion - neighboursCongestion * 0.5f; // Encouraging lower congestion at the junction and neighbors

        float collaborationReward = 0f;
        foreach (var agent in neighbours)
        {
            collaborationReward += Mathf.Max(0, previousCongestion - agent.tracker.GetCumulativeCongestion());
        }

        reward += collaborationReward;

        SetReward(reward);

        previousCongestion = congestion;
        previousReward = reward;
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
