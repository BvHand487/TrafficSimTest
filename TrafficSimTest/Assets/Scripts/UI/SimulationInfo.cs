using Core;
using Core.Buildings;
using Core.Vehicles;
using ML;
using TMPro;
using UnityEngine;

namespace UI
{
    public class SimulationInfo : MonoBehaviour
    {
        private Simulation simulation;
        private VehicleManager vehicleManager;
        private BuildingManager buildingManager;

        private TextMeshProUGUI simulationData;

        void Awake()
        {
            simulationData = GetComponent<TextMeshProUGUI>();
        }

        void Start()
        {
            simulation = GameObject.FindGameObjectWithTag("Simulation").GetComponent<Simulation>();
            vehicleManager = simulation.vehicleManager;
            buildingManager = simulation.buildingManager;
        }

        void Update()
        {
            simulationData.text = $"Vehicle Info:\n" +
                                  $"- Time-dependent vehicle amount: {TrainingManager.Instance.timeDependentTraffic}\n" +
                                  $"- Vehicle collision: {vehicleManager.vehicleCollisions}\n" +
                                  $"- Vehicle mulitplier: {(100f * vehicleManager.vehicleMultiplier).ToString("0")}%\n" +
                                  $"- Max vehicles: {vehicleManager.maxVehicleCount}\n" +
                                  $"- Simulated max vehicles: {vehicleManager.simulatedMaxVehicleCount}\n" +
                                  $"- Current vehicles: {vehicleManager.currentVehicleCount}\n" +
                                  $"- Vehicles spawn queue: {vehicleManager.spawnQueue.Count}\n" +
                                  $"- HTW / WTH / RND: {(100f * vehicleManager.homeToWorkTrafficChance).ToString("0")}% / " +
                                  $"{(100f * vehicleManager.workToHomeTrafficChance).ToString("0")}% / " +
                                  $"{(100f * (1f - vehicleManager.homeToWorkTrafficChance - vehicleManager.workToHomeTrafficChance)).ToString("0")}%\n\n" +
                                  $"Building Info:\n" +
                                  $"- Building count: {buildingManager.buildings.Count}\n" +
                                  $"- H / W: {buildingManager.buildingsByType[Building.Type.Home].Count} / {buildingManager.buildingsByType[Building.Type.Work].Count}\n\n" +
                                  $"Road Info:\n" +
                                  $"- Two mode junction: {TrainingManager.Instance.twoModeJunctions}\n" +
                                  $"- Junction count: {simulation.junctions.Count}\n" +
                                  $"- Road count: {simulation.roads.Count}";
        }
    }
}
