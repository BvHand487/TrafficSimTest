using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class CongestionTracker : MonoBehaviour
{
    [SerializeField] private float timeWindow = 180f;  // Total congestion is an average over 3 minutes
    [SerializeField] private float updatePeriod = 10f;  // Add values every 10 seconds

    private VehicleManager vehicleManager;
    private TrafficController trafficController;
    
    private float cumulativeCongestion = 0f;
    private Queue<float> congestionHistory;
    private float timeElapsed;

    void Awake()
    {
        congestionHistory = new Queue<float>();
    }

    public void Start()
    {
        trafficController = GetComponent<TrafficController>();
        vehicleManager = trafficController.junction.simulation.vehicleManager;
    }

    public void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed < updatePeriod)
            return;

        float currentCongestion = CalculateCurrentCongestion();
        UpdateCongestion(currentCongestion);

        timeElapsed = 0f;
    }

    // Returns queue length of vehicles per road
    public List<int> GetQueueLengths()
    {
        var queues = new List<int>();
    
        for (int i = 0; i < trafficController.lights.Count; ++i)
            queues.Add(trafficController.lights[i].vehicleQueue.Count);

        return queues;
    }

    // Returns total waiting time of vehicles per road
    public List<float> GetTotalWaitingTimes()
    {
        var waitingTimes = new List<float>();

        for (int i = 0; i < trafficController.lights.Count; ++i)
            waitingTimes.Add(trafficController.lights[i].vehicleQueue.Sum(v => v.timeWaiting));

        return waitingTimes;
    }

    public void UpdateCongestion(float currentCongestion)
    {
        congestionHistory.Enqueue(currentCongestion);

        cumulativeCongestion += currentCongestion * Time.timeScale;

        while (congestionHistory.Count * Time.timeScale > timeWindow)
        {
            float oldestCongestion = congestionHistory.Dequeue();
            cumulativeCongestion -= oldestCongestion * Time.timeScale;
        }
    }

    public float CalculateCurrentCongestion()
    {
        List<int> queueLengths = GetQueueLengths();
        List<float> waitingTimes = GetTotalWaitingTimes();

        float totalQueueLength = queueLengths.Sum();  // sum of all queue lengths
        float totalWaitingTime = waitingTimes.Sum();  // sum of all waiting times

        float normalizedQueueLength = totalQueueLength / (trafficController.lights.Count * 20f);  // assume max queue length is 20 for now
        float normalizedWaitingTime = totalWaitingTime / (trafficController.lights.Count * 40f);  // assume max waiting time is 40 for now

        float queueWeight = 0.5f;
        float waitingTimeWeight = 0.5f;

        return (queueWeight * normalizedQueueLength) + (waitingTimeWeight * normalizedWaitingTime);
    }

    public float GetCumulativeCongestion()
    {
        // normalize
        return cumulativeCongestion / congestionHistory.Count;
    }

    public bool ReadyToReport()
    {
        if (congestionHistory.Count >= 1 && cumulativeCongestion != float.NaN && cumulativeCongestion != 0f)
            return true;
        else
            return false;
    }

    public void ResetValues()
    {
        congestionHistory.Clear();
        cumulativeCongestion = 0f;
        timeElapsed = 0f;
    }
}
