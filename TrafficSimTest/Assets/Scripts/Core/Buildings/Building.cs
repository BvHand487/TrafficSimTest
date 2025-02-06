using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Building : MonoBehaviour
{
    public enum Type
    {
        None,
        Home,
        Work
    }
    public Type type;

    public List<Road> roads = new List<Road>();
    public Dictionary<Road, Vector3> spawnPoints = new Dictionary<Road, Vector3>();
    
    public Junction closestJunction;

    private Simulation simulation;
    private BuildingManager buildingManager;

    private MeshRenderer meshRenderer;

    public void Awake()
    {
        simulation = GetComponentInParent<Simulation>();
        buildingManager = GetComponentInParent<BuildingManager>();

        meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    public void Start()
    {
        this.spawnPoints = Building.GetSpawnPoints(this);
        this.closestJunction = Building.GetClosestJunction(this);
    }

    public void SetType(Type type)
    {
        this.type = type;

        switch (type)
        {
            case Type.Home:
                meshRenderer.material.color = Color.green;
                break;

            case Type.Work:
                meshRenderer.material.color = Color.cyan;
                break;

            default:
                meshRenderer.material.color = Color.black;
                break;
        }
    }

    public static Junction GetClosestJunction(Building b)
    {
        if (b.roads.Count == 2)
            return Road.GetCommonJunction(b.roads);
        else
        {
            Road road = b.roads.First();

            // If one of the junctions is an end road return the other one
            if (road.junctionStart.roads.Count == 1)
                return road.junctionEnd;

            if (road.junctionEnd.roads.Count == 1)
                return road.junctionStart;


            float distanceToJunctionStart = Vector3.Distance(b.transform.position, road.junctionStart.transform.position);
            float distanceToJunctionEnd = Vector3.Distance(b.transform.position, road.junctionEnd.transform.position);

            // If junctionStart is closer to the building than junctionEnd
            if (distanceToJunctionStart < distanceToJunctionEnd)
                return road.junctionStart;
            else if (distanceToJunctionStart > distanceToJunctionEnd)
                return road.junctionEnd;
            else
            {
                // If distance is equal get the junction closer to the center of the city
                if (road.junctionStart.transform.position.sqrMagnitude < road.junctionEnd.transform.position.sqrMagnitude)
                    return road.junctionStart;
                else
                    return road.junctionEnd;
            }
        }
    }

    public static Dictionary<Road, Vector3> GetSpawnPoints(Building b)
    {
        var spawnPointsMap = new Dictionary<Road, Vector3>();

        foreach (var r in b.roads)
        {
            Vector3 closestPoint = Utils.Math.GetClosestVector(b.transform.position, r.path);
            Vector3 closestPointDir = (closestPoint - b.transform.position).normalized;

            // exit point is closestPoint
            if (Mathf.Abs(Vector3.Dot(Vector3.right, closestPointDir)) > 0.99f ||
                Mathf.Abs(Vector3.Dot(Vector3.forward, closestPointDir)) > 0.99f)
            {
                spawnPointsMap[r] = closestPoint;
            }
            // exit point is a continuation of the closest point
            else
            {
                int closestPointIndex = r.path.IndexOf(closestPoint);
                int nextPointIndex = closestPointIndex == 0 ? 1 : r.path.Count - 2;

                Vector3 continuationPoint = 2 * closestPoint - r.path[nextPointIndex];
                spawnPointsMap[r] = continuationPoint;
            }
        }

        return spawnPointsMap;
    }
}