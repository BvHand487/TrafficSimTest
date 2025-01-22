using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : SingletonMonobehaviour<GameManager>
{
    [System.NonSerialized] public Simulation simulation;

    [Header("Prefabs")]
    public GameObject simulationPrefab;
    public GameObject roadStraightPrefab;
    public GameObject roadTurnPrefab;
    public GameObject roadJoinPrefab;
    public GameObject roadCrossPrefab;
    public GameObject roadEndPrefab;
    public GameObject buildingPrefab;

    [Header("Generation Settings")]
    [SerializeField] public float tileSize = 15f;
    [SerializeField] public int gridSize = 50;
    [SerializeField] public int junctionGap = 5;
    
    [SerializeField] public int minBuildingHeight;
    [SerializeField] public int maxBuildingHeight;
    [SerializeField] public float buildingHeightStep = 2.0f;
    [SerializeField] public float buildingHeightDecay;
    [SerializeField] public float buildingHeightRandomness = 2.0f;

    [Header("Vehicle Settings")]
    [SerializeField] public List<VehiclePreset> vehicleTypes;
    [SerializeField] public float vehicleMultiplier = 0.5f;

    public override void Awake()
    {
        base.Awake();

        if (PlayerPrefs.HasKey("Grid Size"))
            gridSize = PlayerPrefs.GetInt("Grid Size");
        if (PlayerPrefs.HasKey("Junction Gap"))
            junctionGap = PlayerPrefs.GetInt("Junction Gap");
        PlayerPrefs.DeleteAll();

        GameObject obj = Instantiate(simulationPrefab, Vector3.zero, Quaternion.identity);
        obj.name = $"{simulationPrefab.name}";
        obj.SetActive(false);

        simulation = obj.GetComponent<Simulation>();
        Generation.Generation.Generate(simulation.transform);
    }

    private void Start()
    {
        simulation.gameObject.SetActive(true);
    }
}
