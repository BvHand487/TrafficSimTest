using System.Collections.Generic;
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
    List<Road> adjacentRoads;

    public Building(List<Road> roads)
    {
        adjacentRoads = roads;
    }
}