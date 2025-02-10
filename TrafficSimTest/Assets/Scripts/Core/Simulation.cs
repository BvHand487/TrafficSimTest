using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Simulation : MonoBehaviour
{
    public VehicleManager vehicleManager;
    public BuildingManager buildingManager;

    public List<Junction> junctions;
    public List<Road> roads;

    public void Awake()
    {
        vehicleManager = GetComponent<VehicleManager>();
        buildingManager = GetComponent<BuildingManager>();
    }

    public void Start()
    {
        junctions = GetComponentsInChildren<Junction>().ToList();

        roads = junctions
            .SelectMany(junction => junction.roads)
            .Distinct()
            .ToList();
    }

    public void OnDestroy()
    {
        Destroy(vehicleManager);
        Destroy(buildingManager);

        foreach (var junction in junctions)
            Destroy(junction);

        for (int i = 0; i < transform.childCount; ++i)
            Destroy(transform.GetChild(i).gameObject);
    }
}
