using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// A junction is either a crossroad, joinroad (t-junction) or an end road (for pathfinding)
public class Junction
{
    // Which roads are connected to the junction
    public List<Road> roads;
    public List<TrafficLight> lights;
    public GameObject obj;

    public Junction(GameObject obj)
    {
        this.obj = obj;
    }

    // Set junction's roads and create traffic lights
    public void SetRoads(List<Road> roads)
    {
        this.roads = roads;

        if (roads.Count > 1)
        {
            lights = new List<TrafficLight>();
            
            for (int i = 0; i < roads.Count; ++i)
                lights.Add(new TrafficLight(this, roads[i]));
        }
        else
            lights = null;   
    }

    // Updates the junction's traffic lights
    public void Update()
    {
        if (lights == null)
            return;

        foreach (var light in lights)
            light.Update();
    }

    public static Road GetCommonRoad(Junction a, Junction b)
    {
        return a.roads.Intersect(b.roads).First();
    }
}