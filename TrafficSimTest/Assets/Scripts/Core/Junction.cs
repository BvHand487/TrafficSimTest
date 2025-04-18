using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// A junction is either a crossroad, joinroad (t-junction) or an end road (for pathfinding)
public class Junction : MonoBehaviour
{
    public enum Type
    {
        None,
        Stops,
        Lights,
    }

    public Type type;
    public List<Road> roads;
    public List<Vector3> exitPoints;
    
    public Simulation simulation;
    public TrafficController trafficController;

    public BoxCollider boxCollider;

    public void Awake()
    {
        simulation = GetComponentInParent<Simulation>();
        trafficController = GetComponentInChildren<TrafficController>();

        boxCollider = GetComponent<BoxCollider>();
    }

    // after Awake, before Update
    public void Initialize(List<Road> roads)
    {
        this.roads = roads;
        this.OrderRoads();

        var cyclicRoads = roads.FindAll(r => r.IsCyclic());
        if (cyclicRoads.Count != 0 && cyclicRoads.First().path.First() == cyclicRoads.Last().path.First()) 
            cyclicRoads.Last().path.Reverse();
    }

    public void Start()
    {
        if (simulation != null)
        {
            float halfSize = 0.5f * (simulation.transform.GetChild(0).localScale.x - 2f * GameManager.TileSize);
            float maxDistance = Mathf.Sqrt(2f) * halfSize;

            type = Utils.Modeling.ChooseRandomJunctionType(transform.position.magnitude / maxDistance);
        }
        else
        {
            type = Junction.Type.None;
        }
    }

    public static Road GetCommonRoad(Junction a, Junction b)
    {
        return a?.roads?.Intersect(b?.roads ?? Enumerable.Empty<Road>())?.FirstOrDefault();
    }
    
    public static List<Road> GetCommonRoads(Junction a, Junction b)
    {
        return a?.roads?.Intersect(b?.roads ?? Enumerable.Empty<Road>()).ToList();
    }


    // Orders roads around the intersection sequentially
    // If it's a 4-way intersection it orders them anticlockwise
    private void OrderRoads()
    {
        Vector3 junctionPos = transform.localPosition;
        Dictionary<float, Road> anglesInWorld = new Dictionary<float, Road>();

        if (roads.Count == 3)
        {
            Vector3 dirToIndex0 = roads[0].GetClosestEndPoint(junctionPos) - junctionPos;
            Vector3 dirToIndex1 = roads[1].GetClosestEndPoint(junctionPos) - junctionPos;
            Vector3 dirToIndex2 = roads[2].GetClosestEndPoint(junctionPos) - junctionPos;

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
        return boxCollider.bounds.Contains(point);
    }


    public int vehiclesExitedSinceLastStep = 0;

    public int VehiclesExitedSinceLastStep()
    {
        int exited = vehiclesExitedSinceLastStep;
        vehiclesExitedSinceLastStep = 0;
        return exited;
    }
}