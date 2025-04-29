using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using UI;
using UnityEngine;

namespace ML
{
    public class CongestionDisplay : MonoBehaviour
    {
        private Graph graph;
        private List<CongestionTracker> trackers;

        private DateTime earliestTime;
        private Queue<float> history = new Queue<float>();
        private int maxHistoryLength = 60;  // graph shows 1 hour
        private float timeWindow = 60f;  // plots every minute
        private float timeElapsed;

        private void Awake()
        {
            graph = GetComponentInChildren<Graph>();
        }

        private void Start()
        {
            trackers = new List<CongestionTracker>();

            foreach (var junction in GameObject.FindGameObjectsWithTag("Junction"))
            {
                var congestionTracker = junction.GetComponentInChildren<CongestionTracker>();
                trackers.Add(congestionTracker);
            }
        }

        void Update()
        {
            timeElapsed += Time.deltaTime;

            if (timeElapsed < timeWindow)
                return;

            if (trackers.All(t => t.ReadyToReport()))
            {
                if (earliestTime == default(DateTime))
                    earliestTime = Clock.Instance.datetime;

                DateTime latestTime = earliestTime.AddSeconds(timeWindow * history.Count);
            
                var value = CalculateAggregatedCongestion();
                history.Enqueue(value);

                graph.ShowGraph(
                    history.ToList(),
                    maxHistoryLength,
                    (idx) => idx % 10 == 0 ? $"{(earliestTime.AddSeconds(timeWindow * idx)).ToString("HH:mm:ss")}" : string.Empty,
                    (value) => value.ToString("0.00"),
                    isCoroutine: true
                );
            }

            while (history.Count > maxHistoryLength)
            {
                history.Dequeue();
                earliestTime = earliestTime.AddSeconds(timeWindow);
            }

            timeElapsed -= timeWindow;
        }

        public float CalculateAggregatedCongestion()
        {
            float totalCongestion = 0f;

            foreach (var t in trackers)
            {
                var trackerCongestion = t.GetAverageCongestion();
                totalCongestion += trackerCongestion;
            }

            // normalize
            return totalCongestion / trackers.Count;
        }
    }
}
