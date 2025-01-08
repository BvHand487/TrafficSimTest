using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Building
{
    public enum Type
    {
        None,
        Home,
        Work
    }

    public Type type { get; private set; }
    public GameObject obj;
    public Dictionary<Road, Vector3> spawnPoints = new Dictionary<Road, Vector3>();

    public Building(GameObject obj)
    {
        this.obj = obj;
    }

    public void Initialize(Type type = Type.None)
    {
        this.type = type;

        var mat = obj.GetComponentInChildren<Renderer>().material;
        switch (type)
        {
            case Type.Home:
                mat.color = Color.green;
                break;

            case Type.Work:
                mat.color = Color.cyan;
                break;

            default:
                mat.color = Color.black;
                break;
        }
    }

    public static Junction GetClosestJunction(Building b)
    {
        List<Road> adjacentRoads = b.spawnPoints.Keys.ToList();

        if (adjacentRoads.Count == 2)
            return Road.GetCommonJunction(adjacentRoads.First(), adjacentRoads.Last());
        else
        {
            Road road = adjacentRoads.First();
            Vector3 buildingPos = b.obj.transform.position;

            // If junctionStart is closer to the building than junctionEnd
            if (Vector3.Distance(buildingPos, road.junctionStart.obj.transform.position) <=
                Vector3.Distance(buildingPos, road.junctionEnd.obj.transform.position))
                return road.junctionStart;
            else
                return road.junctionEnd;
        }
    }
}