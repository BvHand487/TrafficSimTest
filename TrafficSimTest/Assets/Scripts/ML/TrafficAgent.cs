using NUnit.Framework.Constraints;
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

public class TrafficAgent : Agent
{
    private BehaviorParameters parameters;
    private TrainingManager trainingManager;

    private VehicleManager vehicleManager;
    private TrafficController trafficController;
    public CongestionTracker tracker;
    private List<TrafficAgent> neighbours;

    private float previousCongestion;
    private float previousReward;
    private float timeElapsed;

    protected override void Awake()
    {
        base.Awake();

        parameters = GetComponent<BehaviorParameters>();

        trafficController = GetComponent<TrafficController>();
        tracker = GetComponent<CongestionTracker>();
        neighbours = new List<TrafficAgent>();
    }

    public void Start()
    {
        vehicleManager = trafficController.junction.simulation.vehicleManager;
        trainingManager = TrainingManager.Instance;

        var thisJunction = trafficController.junction;
        var neighbouingJunctions = thisJunction.roads
            .Select(r => r.GetOtherJunction(thisJunction))
            .Where(j => j != null && j != thisJunction && j.roads.Count > 1)
            .Distinct();

        neighbours = neighbouingJunctions
            .Select(neighbourJunction => neighbourJunction.GetComponentInChildren<TrafficAgent>())
            .ToList();

        parameters.BrainParameters.VectorObservationSize = 2 * thisJunction.roads.Count;
        foreach (var junction in neighbouingJunctions)
        {
            parameters.BrainParameters.VectorObservationSize += 2;
        }

        var actionParams = parameters.BrainParameters.ActionSpec;
        actionParams.NumContinuousActions = 0;
        actionParams.BranchSizes = new int[] { 2 };
        parameters.BrainParameters.ActionSpec = actionParams;

        Debug.Log($"j: {thisJunction.transform.position}, neighbouring agents: {neighbours.Count}, params: {parameters.BrainParameters.VectorObservationSize}");

        timeElapsed = 0f;
    }

    public override void OnEpisodeBegin()
    {
        if (!trainingManager.isTraining)
            return;

        if (!vehicleManager)
            vehicleManager = trafficController.junction.simulation.vehicleManager;

        tracker.ResetValues();
        vehicleManager.ClearVehicles();
        trafficController.SetLights(Mathf.RoundToInt(Random.value));
    }

    private void Update()
    {
        timeElapsedLights += Time.deltaTime;

        if (!trainingManager.isTraining)
            return;

        timeElapsed += Time.deltaTime;

        if (timeElapsed > trainingManager.episodeLength)
            EndEpisode();

        timeElapsed -= trainingManager.episodeLength;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(tracker.GetQueueLengths().Select(v => (float) v).ToArray());
        sensor.AddObservation(tracker.GetTotalWaitingTimes().Select(v => (float) v).ToArray());

        foreach (var agent in neighbours)
        {
            sensor.AddObservation(agent.tracker.GetQueueLengths().Sum());
            sensor.AddObservation(agent.tracker.GetTotalWaitingTimes().Sum());
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
        //float minGreenInterval = 5.0f;
        //float maxGreenInterval = 30.0f;
        //float stepSize = 0.5f;

        //var continuousActions = actionBuffers.ContinuousActions;
        //var discreteActions = actionBuffers.DiscreteActions;

        //List<float> scaledIntervals = new List<float>();

        //for (int i = 0; i < trafficController.lights.Count; i++)
        //{
        //    float continuousGreenInterval = Mathf.Lerp(minGreenInterval, maxGreenInterval, 0.5f * continuousActions[i] + 0.5f);

        //    float scaledGreenInterval = Mathf.Clamp(
        //        Mathf.Round(continuousGreenInterval / stepSize) * stepSize,
        //        minGreenInterval,
        //        maxGreenInterval
        //    );

        //    scaledIntervals.Add(scaledGreenInterval);
        //}

        //if (trainingManager.twoPhaseJunctions)
        //{
        //    if (discreteActions[0] == 0)
        //        trafficController.ConfigureLights(scaledIntervals, TrafficController.Mode.Single);

        //    if (discreteActions[0] == 1)
        //        trafficController.ConfigureLights(scaledIntervals, TrafficController.Mode.Double);
        //}
        //else
        //    trafficController.ConfigureLights(scaledIntervals, TrafficController.Mode.Double);

        // minimum light time
        if (timeElapsedLights < 10f)
            return;

        timeElapsedLights = 0f;

        int phase = actionBuffers.DiscreteActions[0];

        trafficController.SetLights(phase);
    }

    public void EvaluateRewards()
    {
        // current and nearby congestion - negative reward
        var congestion = tracker.GetAverageCongestion();
        var neighboursCongestion = neighbours.Select(n => tracker.GetAverageCongestion()).Sum();
        var totalCongestion = congestion + neighboursCongestion;
        float reward = -congestion - neighboursCongestion * 0.5f;

        // collaborative work - positive reward
        float collaborationReward = 0f;
        foreach (var agent in neighbours)
        {
            collaborationReward += Mathf.Max(0, previousCongestion - agent.tracker.GetAverageCongestion()) * 0.2f;
        }
        reward += collaborationReward;

        // exponentially worse reward for long waiting time
        float expWaitingTimePenalty = -0.1f * trafficController.lights.Sum(l => l.vehicleQueue.Sum(v => Mathf.Exp(v.timeWaiting / 20f)));
        reward += expWaitingTimePenalty;

        // increase reward for high throughput
        float vehiclesCleared = trafficController.junction.VehiclesExitedSinceLastStep();
        float clearanceReward = vehiclesCleared; // Positive reward for clearing vehicles
        reward += clearanceReward;

        SetReward(reward);

        previousCongestion = congestion;
        previousReward = reward;
    }

    public float timeElapsedLights = 0f;

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;

        discreteActions[0] = 1 - trafficController.currentPhase;
    }
}
