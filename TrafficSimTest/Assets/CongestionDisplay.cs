using JetBrains.Annotations;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class CongestionDisplay : MonoBehaviour
{
    private Graph graph;
    private List<CongestionTracker> trackers;

    private Queue<float> history = new Queue<float>();
    private int maxHistoryLength = 50;
    private float timeWindow = 60f;
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
            var value = CalculateAggregatedCongestion();
            history.Enqueue(value);

            graph.ShowGraph(
                history.ToList(),
                maxHistoryLength,
                (idx) => idx % 5 == 0 ? $"{idx}" : string.Empty,
                (value) => value.ToString("0.00")
            );
        }

        timeElapsed = 0f;
    }

    public float CalculateAggregatedCongestion()
    {
        float totalCongestion = 0f;

        foreach (var t in trackers)
        {
            var trackerCongestion = t.GetCumulativeCongestion();
            totalCongestion += trackerCongestion;

            Debug.Log(trackerCongestion);
        }

        // normalize
        return totalCongestion / trackers.Count;
    }
}
