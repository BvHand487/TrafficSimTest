using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils;

public class VehicleManager : MonoBehaviour
{
    private Simulation simulation;

    private int maxVehicleCount;
    [System.NonSerialized] public int currentVehicleCount;
    [System.NonSerialized] public int simulatedMaxVehicleCount;

    private float minVehicleTravelDistance = 75.0f;
    private float minVehicleSpawnDistance = 30.0f;  // for a vehicle to spawn - there has to be no other vehicles within 30 units
    private float turnRadius=7.5f;
    private int turnResolution=10;

    private List<VehiclePreset> types = new List<VehiclePreset>();

    private List<Vehicle> spawnQueue = new List<Vehicle>();

    private float homeToWorkTrafficChance = 1.0f;
    private float workToHomeTrafficChance = 1.0f;

    public void Awake()
    {
        simulation = GetComponent<Simulation>();

        types = GameManager.Instance.vehicleTypes;
    }

    public void Start()
    {
        maxVehicleCount = (int) (GameManager.Instance.vehicleMultiplier * simulation.buildingManager.buildings.Count);
    }

    public void Update()
    {
        simulatedMaxVehicleCount = Modeling.CalculateTrafficFlowFromTime(24f * Clock.Instance.GetFractionOfDay(), ref homeToWorkTrafficChance, ref workToHomeTrafficChance, maxVehicleCount);

        if (currentVehicleCount + spawnQueue.Count < simulatedMaxVehicleCount)
        {
            SpawnVehicle();
        }

        List<Vehicle> toRemove = new List<Vehicle>();
        foreach (var vehicle in spawnQueue)
            if (CanActivateVehicle(vehicle))
            {
                currentVehicleCount++;
                vehicle.gameObject.SetActive(true);
                toRemove.Add(vehicle);
            }

        foreach (var carToRemove in toRemove)
            spawnQueue.Remove(carToRemove);
    }

    public void SpawnVehicle()
    {
        VehiclePreset preset = GetRandomVehicle();
        VehiclePath path = CreatePath();
        Vehicle vehicle = InstantiateVehicle(simulation.transform.position + path.Next(), Quaternion.identity, preset);
        vehicle.Initialize(preset, path);
        spawnQueue.Add(vehicle);
    }

    public VehiclePreset GetRandomVehicle()
    {
        return Utils.Random.Select(types);
    }

    private bool CanActivateVehicle(Vehicle vehicle)
    {
        // TODO: change 1 << 6, which is the vehicle layer
        Collider[] hits = Physics.OverlapSphere(vehicle.transform.position, minVehicleSpawnDistance, 1 << 6);

        if (hits.Length == 0)
            return true;
        else
            return false;
    }

    private VehiclePath CreatePath()
    {
        VehiclePath path;
        float rand = UnityEngine.Random.value;

        if (rand < homeToWorkTrafficChance)
            path = CreateDirectedVehiclePath();
        else if (rand < workToHomeTrafficChance)
        {
            path = CreateDirectedVehiclePath();
            path.Reverse();
        }
        else
            path = CreateRandomVehiclePath();

        return path;
    }

    private VehiclePath CreateRandomVehiclePath()
    {
        Building start, end;

        do
        {
            start = simulation.buildingManager.GetRandomBuilding();
            end = simulation.buildingManager.GetRandomBuilding();
        }
        while (Vector3.Distance(start.transform.localPosition, end.transform.localPosition) < minVehicleTravelDistance);

        return new VehiclePath(start, end, turnRadius, turnResolution);
    }

    private VehiclePath CreateDirectedVehiclePath()
    {
        Building start, end;

        do
        {
            start = simulation.buildingManager.GetRandomBuildingByType(Building.Type.Home);
            end = simulation.buildingManager.GetRandomBuildingByType(Building.Type.Work);
        }
        while (Vector3.Distance(start.transform.localPosition, end.transform.localPosition) < minVehicleTravelDistance);

        return new VehiclePath(start, end, turnRadius, turnResolution);
    }

    public Vehicle InstantiateVehicle(Vector3 pos, Quaternion rot, VehiclePreset preset)
    {
        var obj = GameObject.Instantiate(preset.prefab, pos, rot, transform);
        obj.SetActive(false);
        obj.name = preset.prefab.name;
        return obj.GetComponent<Vehicle>();
    }

    public void OnDestroy()
    {
        foreach (Vehicle vehicle in FindObjectsByType<Vehicle>(FindObjectsSortMode.None))
            Destroy(vehicle);
    }
}