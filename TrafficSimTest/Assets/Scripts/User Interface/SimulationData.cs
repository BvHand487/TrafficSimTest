using TMPro;
using UnityEngine;

public class SimulationData : MonoBehaviour
{
    private Simulation simulation;
    private VehicleManager vehicleManager;

    private TextMeshProUGUI simulationData;

    void Start()
    {
        simulationData = GetComponent<TextMeshProUGUI>();

        simulation = GameObject.FindGameObjectWithTag("Simulation").GetComponent<Simulation>();
        vehicleManager = simulation.vehicleManager;
    }


    void Update()
    {
        simulationData.text = $"Time-dependent vehicle amount: {TrainingManager.Instance.timeDependentTraffic}\n" +
            $"- Max vehicles: {vehicleManager.maxVehicleCount}\n" +
            $"- Simulated max vehicles: {vehicleManager.simulatedMaxVehicleCount}\n" +
            $"- Current vehicles: {vehicleManager.currentVehicleCount}\n" +
            $"- Vehicles spawn queue: {vehicleManager.spawnQueue.Count}\n" +
            $"- HTW / WTH / RND: {(100f * vehicleManager.homeToWorkTrafficChance).ToString("0")}% / " +
            $"{(100f * vehicleManager.workToHomeTrafficChance).ToString("0")}% / " +
            $"{(100f * (1f - vehicleManager.homeToWorkTrafficChance - vehicleManager.workToHomeTrafficChance)).ToString("0")}%";
    }
}
