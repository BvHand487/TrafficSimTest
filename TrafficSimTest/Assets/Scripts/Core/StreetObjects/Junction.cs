using Generation;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
    public Collider junctionCollider;

    // If the junction is controlled with lights
    public TrafficController trafficController;
    public List<TrafficLight> trafficLights => trafficController?.lights;

    public Simulation simulation;

    // If the junctions is controlled with stop signs
    /* Add priority road + car queues */


    public Junction(Simulation simulation, GameObject obj)
    {
        this.simulation = simulation;
        this.obj = obj;
        this.junctionCollider = obj.GetComponent<Collider>();
    }

    // Set junction's roads and create traffic lights
    public void Initialize(List<Road> roads, Type type = Type.None)
    {
        this.roads = roads;
        this.OrderRoads();

        var cyclicRoads = roads.FindAll(r => r.IsCyclic());
        if (cyclicRoads.Count != 0 && cyclicRoads.First().path.First() == cyclicRoads.Last().path.First()) 
            cyclicRoads.Last().path.Reverse();

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
        return a?.roads?.Intersect(b?.roads ?? Enumerable.Empty<Road>())?.FirstOrDefault();
    }


    // Orders roads around the intersection sequentially
    // If it's a 4-way intersection it orders them anticlockwise
    private void OrderRoads()
    {
        Vector3 junctionPos = this.obj.transform.localPosition;
        Dictionary<float, Road> anglesInWorld = new Dictionary<float, Road>();

        if (roads.Count == 3)
        {
            Vector3 dirToIndex0 = roads[0].GetClosestPathPoint(junctionPos) - junctionPos;
            Vector3 dirToIndex1 = roads[1].GetClosestPathPoint(junctionPos) - junctionPos;
            Vector3 dirToIndex2 = roads[2].GetClosestPathPoint(junctionPos) - junctionPos;

            if (Vector3.Dot(dirToIndex0, dirToIndex1) <= -0.95)
            {
                roads = new List<Road> { roads[0], roads[2], roads[1] };
            }

            else if (Vector3.Dot(dirToIndex1, dirToIndex2) <= -0.95)
            {
                roads = new List<Road> { roads[1], roads[0], roads[2] };
                return;
            }
        }
        else
        {
            for (int i = 0; i < roads.Count; ++i)
            {
                var roadPos = roads[i].IsCyclic() ?
                    roads[i].path.First() :
                    Utils.Math.GetClosestVector(junctionPos, roads[i].path);

                Vector3 roadDir = (roadPos - junctionPos).normalized;

                var angle = Vector3.Angle(roadDir, Vector3.right);
                if (Vector3.Dot(roadDir, Vector3.forward) < 0.1f)
                    angle = 360f - angle;
                angle = Unity.Mathematics.math.fmod(angle, 360f);

                anglesInWorld.Add(angle, roads[i]);
            }

            roads = anglesInWorld.OrderBy(e => e.Key).Select(pair => pair.Value).ToList();
        }
    }

    public bool IsPointInside(Vector3 point)
    {
        return junctionCollider.bounds.Contains(point);
    }

    public override string ToString()
    {
        return $"({obj.name}, at: {obj.transform.position}, type: {type})";
    }
}