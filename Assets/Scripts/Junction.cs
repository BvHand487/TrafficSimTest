using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// A junction is either a crossroad, joinroad (t-junction) or an end road (for pathfinding)
public class Junction
{
    public enum Type
    {
        Stops,
        Lights,
    }

    // Which roads are connected to the junction
    public Type type;
    public List<Road> roads;
    public GameObject obj;


    // If the junction is controlled with lights
    public enum LightMode
    {
        Single,
        Double,
    }
    public List<TrafficLight> lights;
    public LightMode mode;

    // If the junctions is controlled with stop signs
    /* Add priority road + car queues */

    public Junction(GameObject obj)
    {
        this.obj = obj;
    }

    // Set junction's roads and create traffic lights
    public void Initialize(List<Road> roads, Type type)
    {
        this.type = type;
        this.roads = Road.OrderRoadsAntiClockwise(roads);

        switch (type)
        {
            case Type.Stops:
                return;
            
            case Type.Lights:
                if (this.roads.Count > 1)
                {
                    lights = new List<TrafficLight>();

                    for (int i = 0; i < this.roads.Count; ++i)
                        lights.Add(new TrafficLight(this, this.roads[i]));

                    if (lights.Count == 3)
                        ConfigureLights(
                            new List<float>() { 20, 15, 15 },
                            new List<float>() { 30, 35, 35 },
                            LightMode.Single);

                    else if (lights.Count == 4)
                        ConfigureLights(
                            new List<float>() { 20, 15, 20, 15 },
                            new List<float>() { 15, 20, 15, 20 },
                            LightMode.Double);
                }
                
                return;
        }
    }

    // Updates the junction's traffic lights
    public void Update()
    {
        switch (type)
        {
            case Type.Stops:
                return;
            case Type.Lights:
                if (this.lights != null)
                    foreach (var light in lights)
                        light.Update();
                return;
        }
    }

    public static Road GetCommonRoad(Junction a, Junction b)
    {
        return a.roads.Intersect(b.roads).First();
    }

    /*
     * Acts as an interface to the future ML agents - verifies the configuration is valid and then sets the traffic lights intervals
     */
    public bool ConfigureLights(List<float> greenIntervals, List<float> redIntervals, LightMode mode)
    {
        if ((greenIntervals.Count < 3 || greenIntervals.Count > 4) ||
            (redIntervals.Count < 3 || redIntervals.Count > 4) ||
            (greenIntervals.Count != redIntervals.Count) ||
            greenIntervals.Count != roads.Count ||
            greenIntervals.Count != lights.Count)
            return false;

        // The sum of green intervals = the period of the traffic lights
        float cycleTime = greenIntervals.Sum();
        float redSum = redIntervals.Sum();

        switch (mode)
        {
            case LightMode.Single:
                {
                    if (redSum != (lights.Count - 1) * cycleTime)
                        return false;

                    for (int i = 0; i < lights.Count; ++i)
                    {
                        if (redIntervals[i] < TrafficLight.minRedInterval ||
                            greenIntervals[i] < TrafficLight.minGreenInterval)
                            return false;

                        // A roads red signal duration has to be equal to the green signals of the other roads
                        if (redIntervals[i] != cycleTime - greenIntervals[i])
                            return false;
                    }

                    for (int i = 0; i < lights.Count; ++i)
                    {
                        lights[i].ConfigureInterval(greenIntervals[i], redIntervals[i] + TrafficLight.redBufferTime);
                        lights[i].SetStatus(TrafficLight.Status.Red);

                        if (i != 0)
                            lights[i].elapsedTime +=
                                TrafficLight.yellowInterval +
                                TrafficLight.redBufferTime +
                                (i - 1) * 2 * TrafficLight.yellowInterval +
                                greenIntervals.GetRange(0, i - 1).Sum() +
                                (i - 1) * TrafficLight.redBufferTime;
                    }

                    lights.First().SetStatus(TrafficLight.Status.Green);

                } break;
            case LightMode.Double:
                {
                    if (lights.Count == 3)
                    {

                    }

                    else if (lights.Count == 4)
                    {
                    }

                    for (int i = 0; i < lights.Count; ++i)
                    {
                        lights[i].ConfigureInterval(greenIntervals[i], redIntervals[i] + TrafficLight.redBufferTime);
                        lights[i].SetStatus(TrafficLight.Status.Red);
                    }
                    
                    /* ???????????????? */
                    
                    float lightToActivateRed = lights.First().redInterval;
                    float lightToActivateGreen = lights.First().greenInterval;
                    foreach (var light in lights)
                        if (light.greenInterval == lightToActivateGreen &&
                            light.redInterval == lightToActivateRed)
                            light.SetStatus(TrafficLight.Status.Green);

                } break;
            default:
                return false;
        }

        this.mode = mode;
        return true;
    }
}