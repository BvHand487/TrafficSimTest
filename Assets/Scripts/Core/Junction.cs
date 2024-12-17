using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// A junction is either a crossroad, joinroad (t-junction) or an end road (for pathfinding)
public class Junction
{
    public enum Type
    {
        None,
        Stops,
        Lights,
    }

    // Which roads are connected to the junction
    public Type type;
    public List<Road> roads;
    public GameObject obj;


    // If the junction is controlled with lights
    public TrafficController trafficController;
    public List<TrafficLight> trafficLights => trafficController?.Lights;


    // If the junctions is controlled with stop signs
    /* Add priority road + car queues */


    public Junction(GameObject obj)
    {
        this.obj = obj;
    }

    // Set junction's roads and create traffic lights
    public void Initialize(List<Road> roads, Type type = Type.None)
    {
        this.roads = Road.OrderRoads(roads);

        if (this.roads.Count == 1)
        {
            type = Type.None;
            return;
        }
        else if (this.roads.Count == 3 || this.roads.Count == 4)
        {
            this.type = type;

            switch (type)
            {
                case Type.Stops:
                    return;

                case Type.Lights:
                    trafficController = new TrafficController(this);
                    return;
            }
        }
    }

    // Updates the junction's traffic lights
    public void Update()
    {
        if (type == Type.Lights)
            trafficController.Update();
    }

    public static Road GetCommonRoad(Junction a, Junction b)
    {
        return a.roads.Intersect(b.roads).First();
    }
}