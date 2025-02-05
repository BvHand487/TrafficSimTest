using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    private Simulation simulation;

    public List<Building> buildings;
    public Dictionary<Building.Type, List<Building>> buildingsByType;

    public void Awake()
    {
        simulation = GetComponent<Simulation>();
    }

    public void Start()
    {
        buildings = GetComponentsInChildren<Building>().ToList();

        buildingsByType = buildings
            .GroupBy(building => building.type)
            .ToDictionary(group => group.Key, group => group.ToList());
    }

    public Building GetRandomBuildingByType(Building.Type type)
    {
        return Utils.Random.Select(buildingsByType[type]);
    }

    public Building GetRandomBuilding()
    {
        return Utils.Random.Select(buildings);
    }

    public void OnDestroy()
    {
        foreach (Building b in buildings)
            Destroy(b);
    }
}
