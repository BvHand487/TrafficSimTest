using System.Collections.Generic;
using Core.Buildings;
using ML;
using UnityEngine;
using Utils;

namespace Core.Vehicles
{
    public class VehicleManager : MonoBehaviour
    {
        private Simulation simulation;

        public int maxVehicleCount;
        [System.NonSerialized] public int currentVehicleCount;
        [System.NonSerialized] public int simulatedMaxVehicleCount;

        private float minVehicleTravelDistance = 75.0f;
        private float minVehicleSpawnDistance = 30.0f;  // for a vehicle to spawn - there has to be no other vehicles within 30 units
        private float turnRadius=7.5f;
        private int turnResolution=10;

        private List<VehiclePreset> types = new List<VehiclePreset>();

        public List<Vehicle> spawnQueue = new List<Vehicle>();

        public float homeToWorkTrafficChance = 1.0f;
        public float workToHomeTrafficChance = 1.0f;

        public bool vehicleCollisions = false;
        public float vehicleMultiplier;

        public void Awake()
        {
            simulation = GetComponent<Simulation>();

            types = GameManager.Instance.vehicleTypes;
            vehicleMultiplier = GameManager.Instance.vehicleMultiplier;
        }

        public void Start()
        {
            UpdateMaxVehicleCount();
        }
   
        public void Update()
        {
            if (TrainingManager.Instance.timeDependentTraffic == true)
            {
                simulatedMaxVehicleCount = Modeling.CalculateTrafficFlowFromTime(24f * Clock.Instance.GetFractionOfDay(), ref homeToWorkTrafficChance, ref workToHomeTrafficChance, maxVehicleCount);
            }    
            else
            {
                simulatedMaxVehicleCount = maxVehicleCount;
                homeToWorkTrafficChance = 0.4f;
                workToHomeTrafficChance = 0.4f;
            }

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
                path = CreateDirectedVehiclePath(reversed: true);
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

        private VehiclePath CreateDirectedVehiclePath(bool reversed = false)
        {
            Building start, end;

            do
            {
                start = simulation.buildingManager.GetRandomBuildingByType(Building.Type.Home);
                end = simulation.buildingManager.GetRandomBuildingByType(Building.Type.Work);
            }
            while (Vector3.Distance(start.transform.localPosition, end.transform.localPosition) < minVehicleTravelDistance);

            if (reversed == false)
                return new VehiclePath(start, end, turnRadius, turnResolution);
            else
                return new VehiclePath(end, start, turnRadius, turnResolution);

        }

        public Vehicle InstantiateVehicle(Vector3 pos, Quaternion rot, VehiclePreset preset)
        {
            var obj = GameObject.Instantiate(preset.prefab, pos, rot, transform);
            obj.SetActive(false);
            obj.name = preset.prefab.name;
            return obj.GetComponent<Vehicle>();
        }

        public void ClearVehicles()
        {
            Vehicle[] vehicles = FindObjectsByType<Vehicle>(FindObjectsSortMode.None);
            foreach (var vehicle in vehicles)
                Destroy(vehicle.gameObject);

            foreach (var vehicle in spawnQueue)
                Destroy(vehicle.gameObject);
        
            spawnQueue.Clear();

            currentVehicleCount = 0;
        }

        public void UpdateMaxVehicleCount()
        {
            maxVehicleCount = (int)(vehicleMultiplier * simulation.buildingManager.buildings.Count);
        }

        public void OnDestroy()
        {
            foreach (Vehicle vehicle in FindObjectsByType<Vehicle>(FindObjectsSortMode.None))
                Destroy(vehicle);
        }
    }
}