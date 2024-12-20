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
    public List<Road> adjacentRoads = new List<Road>();

    public Building(GameObject obj, Type type)
    {
        this.obj = obj;
        this.type = type;

        var mat = obj.GetComponentInChildren<Renderer>().material;
        switch (type)
        {
             case Type.Work:
                mat.color = Color.HSVToRGB(0.5f, 0.5f, 0.8f);
                break;
             case Type.Home:
                mat.color = Color.HSVToRGB(0.3f, 0.5f, 0.7f);
                break;

            default:
            mat.color = Color.black;
            break;
        }
    }

    public static Junction GetClosestJunction(Building b)
    {
        if (b.adjacentRoads.Count == 2)
            return Road.GetCommonJunction(b.adjacentRoads.First(), b.adjacentRoads.Last());
        else
        {
            Road road = b.adjacentRoads.First();
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