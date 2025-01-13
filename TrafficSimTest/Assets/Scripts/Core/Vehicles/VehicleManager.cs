using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils;

public class VehicleManager
{
    public Simulation simulation;
    public List<Vehicle> vehicles;
    public int simulatedMaxVehicles;
    public int maxVehicles;

    public float minVehicleTravelDistance = 75.0f;
    public float turnRadius=7.5f;
    public int turnResolution=5;

    public List<VehiclePreset> types;
    public List<Vehicle> spawnQueue;

    private float homeToWorkTrafficChance = 1.0f;
    private float workToHomeTrafficChance = 1.0f;

    private Clock clock;

    public VehicleManager(Simulation simulation, int maxVehicles=20)
    {
        this.simulation = simulation;
        this.maxVehicles = maxVehicles;

        vehicles = new List<Vehicle>();
        types = GameManager.Instance.vehicleTypes;
        spawnQueue = new List<Vehicle>();

        clock = Clock.Instance;
    }

    public void Update()
    {
        simulatedMaxVehicles = Modeling.CalculateTrafficFlowFromTime(24f * clock.GetFractionOfDay(), ref homeToWorkTrafficChance, ref workToHomeTrafficChance, maxVehicles);

        if (vehicles.Count + spawnQueue.Count < maxVehicles)
        {
            SpawnVehicle();
        }

        List<Vehicle> toRemove = new List<Vehicle>();
        foreach (var vehicle in spawnQueue)
            if (CanActivateVehicle(vehicle))
            {
                vehicles.Add(vehicle);
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
        Vehicle vehicle = InstantiateVehicle(simulation.transform.position + path.Next(), Quaternion.identity, simulation.transform, preset);
        vehicle.Initialize(this, preset, path);
        spawnQueue.Add(vehicle);
    }

    public VehiclePreset GetRandomVehicle()
    {
        return Utils.Random.Select(types);
    }

    private bool CanActivateVehicle(Vehicle vehicle)
    {
        var vehicles = GameObject.FindObjectsByType(typeof(Vehicle), FindObjectsSortMode.None);
        foreach (Vehicle v in vehicles)
            if (v != null && v.gameObject.activeInHierarchy && Vector3.Distance(v.transform.localPosition, vehicle.transform.localPosition) < 30.0f)
                return false;

        return true;
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
        while (Vector3.Distance(start.obj.transform.localPosition, end.obj.transform.localPosition) < minVehicleTravelDistance);

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
        while (Vector3.Distance(start.obj.transform.localPosition, end.obj.transform.localPosition) < minVehicleTravelDistance);

        return new VehiclePath(start, end, turnRadius, turnResolution);
    }

    public Vehicle InstantiateVehicle(Vector3 pos, Quaternion rot, Transform parent, VehiclePreset preset)
    {
        var obj = GameObject.Instantiate(preset.prefab, pos, rot, parent);
        obj.SetActive(false);
        obj.name = preset.prefab.name;
        return obj.GetComponent<Vehicle>();
    }

    public TrafficLight GetTrafficLight(Vehicle vehicle, Junction junction)
    {
        var map = junction.trafficController.trafficLightDict;
        return map[Utils.Math.GetClosestVector(vehicle.transform.localPosition, map.Keys.ToList())];
    }

    public void DestroyVehicle(Vehicle vehicle)
    {
        vehicles.Remove(vehicle);
    }

    public void Destroy()
    {
        foreach (Vehicle vehicle in vehicles)
        {
            GameObject.Destroy(vehicle.gameObject);
        }
    }
}