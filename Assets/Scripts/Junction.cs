using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// A junction is either a crossroad, joinroad (t-junction) or an end road (for pathfinding)
public class Junction
{
    // Which roads are connected to the junction
    public Road[] roads;
    public TrafficLight[] lights;
    public GameObject obj;

    public Junction(GameObject obj)
    {
        this.obj = obj;
    }

    // Set junction's roads and create traffic lights
    public void SetRoads(List<Road> roads)
    {
        this.roads = roads.ToArray();

        if (roads.Count > 1)
        {
            lights = new TrafficLight[roads.Count];
            for (int i = 0; i < roads.Count; i++)
            {
                lights[i] = new TrafficLight(this, roads[i]);
            }
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
}