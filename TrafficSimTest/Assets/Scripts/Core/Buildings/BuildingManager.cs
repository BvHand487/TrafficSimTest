using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class BuildingManager
{
    public Simulation simulation;
    public List<Building> buildings;
    public Dictionary<Building.Type, List<Building>> buildingsByType;

    public BuildingManager(Simulation simulation, List<Building> buildings)
    {
        this.simulation = simulation;
        this.buildings = buildings;

        buildingsByType = new Dictionary<Building.Type, List<Building>>();

        foreach (Building b in buildings)
        {
            if (!buildingsByType.ContainsKey(b.type))
                buildingsByType.Add(b.type, new List<Building>());
            else
                buildingsByType[b.type].Add(b);
        }
    }

    public Building GetRandomBuildingByType(Building.Type type)
    {
        return Utils.Random.Select(buildingsByType[type]);
    }

    public Building GetRandomBuilding()
    {
        return Utils.Random.Select(buildings);
    }

    public void Destroy()
    {
        foreach (Building b in buildings)
        {
            GameObject.Destroy(b.obj);
        }
    }
}
