using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Junction
{
    public Road[] roads;
    public TrafficLight[] lights;
    public GameObject obj;

    public Junction(GameObject obj)
    {
        this.obj = obj;
    }

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


    public void Update()
    {
        if (lights == null)
            return;

        foreach (var light in lights)
            light.Update();
    }
}