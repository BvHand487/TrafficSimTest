﻿using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using System;

public class TrafficController
{
    public enum Mode
    {
        Single,
        Double,
    }

    public Mode mode;
    public float elapsedTime = 0.0f;

    public int activeLight = 0;

    public Junction junction;
    public List<Road> roads => junction.roads;

    private List<TrafficLight> lights;
    public List<TrafficLight> Lights => lights.FindAll(l => l.trafficController != null);

    public TrafficController(Junction junction, List<float> greenIntervals = null, Mode mode = Mode.Double)
    {
        this.junction = junction;

        lights = new List<TrafficLight>();
        for (int i = 0; i < junction.roads.Count; ++i)
            lights.Add(new TrafficLight(this, junction.roads[i]));

        if (greenIntervals == null)
            greenIntervals = new List<float>() { 20f, 20f, 20f, 20f };

        ConfigureLights(greenIntervals, mode);
    }

    public void Update()
    {
        elapsedTime += Time.deltaTime;

        switch (mode)
        {
            case Mode.Single:
                {
                    TrafficLight current = lights[activeLight];
                    UpdateSingleMode(current);
                    break;
                }

            case Mode.Double:
                {
                    TrafficLight current = lights[activeLight];
                    TrafficLight opposite = lights[(activeLight + 2) % lights.Count];

                    if (lights.Count == 3 && activeLight == 1)
                        UpdateSingleMode(current);
                    else
                        UpdateDoubleMode(current, opposite);

                    break;
                }
        }
    }

    public bool IsGreenOver(TrafficLight light) => (light.status == TrafficLight.Status.Green && elapsedTime > light.greenInterval);
    public bool IsYellowOver(TrafficLight light) => (light.status == TrafficLight.Status.Yellow && elapsedTime > TrafficLight.yellowInterval);
    public bool IsRedOver(TrafficLight light) => (light.status == TrafficLight.Status.Red && elapsedTime > TrafficLight.redIntervalBuffer);

    public void UpdateSingleMode(TrafficLight current)
    {
        if (IsGreenOver(current))
        {
            current.status = TrafficLight.Status.Yellow;
            elapsedTime = 0.0f;
        }
        else if (IsYellowOver(current))
        {
            current.status = TrafficLight.Status.Red;
            elapsedTime = 0.0f;
        }
        else if (IsRedOver(current))
        {
            if (lights.Count == 3)
            {
                activeLight = 0;
                lights[activeLight].status = TrafficLight.Status.Green;
                lights[(activeLight + 2) % lights.Count].status = TrafficLight.Status.Green;
            }
            else
            {
                activeLight = (activeLight + 1) % lights.Count;
                lights[activeLight].status = TrafficLight.Status.Green;
            }

            elapsedTime = 0.0f;
        }
    }

    public void UpdateDoubleMode(TrafficLight current, TrafficLight opposite)
    {
        if (IsGreenOver(current))
        {
            current.status = opposite.status = TrafficLight.Status.Yellow;
            elapsedTime = 0.0f;
        }
        else if (IsYellowOver(current))
        {
            current.status = opposite.status = TrafficLight.Status.Red;
            elapsedTime = 0.0f;
        }
        else if (IsRedOver(current))
        {
            if (lights.Count == 3)
                activeLight = 1;
            else
            {
                activeLight = (activeLight + 1) % lights.Count;
                lights[(activeLight + 2) % lights.Count].status = TrafficLight.Status.Green;
            }

            lights[activeLight].status = TrafficLight.Status.Green;
            elapsedTime = 0.0f;
        }
    }


    /*
     * Acts as an interface to the future ML agents - verifies the configuration is valid and then sets the traffic lights intervals
     */
    public bool ConfigureLights(List<float> greenIntervals, Mode mode)
    {
        if (lights.Count == 3 && greenIntervals.Count == 4)
            greenIntervals.RemoveAt(greenIntervals.Count - 1);

        for (int i = 0; i < (int) (lights.Count / 2f); ++i)
            if (!Utils.Math.CompareFloat(greenIntervals[i], greenIntervals[i + 2]))
                return false;

        for (int i = 0; i < lights.Count; ++i)
        {
            if (greenIntervals[i] < TrafficLight.minGreenInterval &&
                greenIntervals[i] > TrafficLight.maxGreenInterval)
                return false;

            lights[i].ConfigureInterval(greenIntervals[i]);
        }

        this.mode = mode;
        return true;
    }
}
