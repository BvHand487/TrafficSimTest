using System.Collections.Generic;
using System.Linq;
using Core;
using Core.Vehicles;
using UnityEngine;

namespace ML
{
    public class CongestionTracker : MonoBehaviour
    {
        public float timeWindow = 90f;  // Total congestion is an average over 5 minutes
        public float updatePeriod = 1.5f;  // Add values every 5 seconds

        private VehicleManager vehicleManager;
        private TrafficController trafficController;
    
        private float maxHistoryLength;
        private Queue<float> congestionHistory;
        private float cumulativeCongestion = 0f;
        private float timeElapsed;

        void Awake()
        {
            congestionHistory = new Queue<float>();

            maxHistoryLength = timeWindow / updatePeriod;
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

            timeElapsed -= updatePeriod;
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
            cumulativeCongestion += currentCongestion;

            while (congestionHistory.Count > maxHistoryLength)
            {
                float oldestCongestion = congestionHistory.Dequeue();
                cumulativeCongestion -= oldestCongestion;
            }
        }

        public float CalculateCurrentCongestion()
        {
            List<int> queueLengths = GetQueueLengths();
            List<float> waitingTimes = GetTotalWaitingTimes();

            float totalQueueLength = queueLengths.Sum();  // sum of all queue lengths
            float totalWaitingTime = waitingTimes.Sum();  // sum of all waiting times

            float normalizedQueueLength = totalQueueLength / (trafficController.lights.Count * 5f);  // assume max queue length is 5 for now
            float normalizedWaitingTime = totalWaitingTime / (trafficController.lights.Count * 10f);  // assume max waiting time is 10 for now

            float queueWeight = 1f;
            float waitingTimeWeight = 1f;

            return (queueWeight * normalizedQueueLength) + (waitingTimeWeight * normalizedWaitingTime);
        }

        public float GetAverageCongestion()
        {
            if (congestionHistory.Count == 0)
                return 0f;

            // normalize
            return cumulativeCongestion / congestionHistory.Count;
        }

        public bool ReadyToReport()
        {
            if (congestionHistory.Count == maxHistoryLength)
                return true;
            else
                return false;
        }

        public void ResetValues()
        {
            Debug.Log("reset");

            congestionHistory.Clear();
            cumulativeCongestion = 0f;
            timeElapsed = 0f;
        }
    }
}
