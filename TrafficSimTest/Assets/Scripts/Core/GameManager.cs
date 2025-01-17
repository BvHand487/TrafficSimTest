using Generation;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new GameManager();
            return _instance;
        }
    }

    [Header("Prefabs")]
    public GameObject simulationPrefab;
    public GameObject roadStraightPrefab;
    public GameObject roadTurnPrefab;
    public GameObject roadJoinPrefab;
    public GameObject roadCrossPrefab;
    public GameObject roadEndPrefab;
    public GameObject buildingPrefab;
    public GameObject groundPrefab;

    [Header("Generation Settings")]
    [SerializeField] public float tileSize = 15f;
    [SerializeField] public int gridSize = 50;
    [SerializeField] public int junctionGap = 5;
    
    [SerializeField] public int minBuildingHeight;
    [SerializeField] public int maxBuildingHeight;
    [SerializeField] public float buildingHeightStep = 2.0f;
    [SerializeField] public float buildingHeightDecay;
    [SerializeField] public float buildingHeightRandomness = 2.0f;

    [Header("Simulation Settings")]
    [SerializeField] public List<VehiclePreset> vehicleTypes;
    [SerializeField] public int vehicleCount;

    [Header("Training Settings")]
    [SerializeField] public int simulationCopies;

    public SimulationsManager simulations;
    public TrainingManager trainingManager;

    public Clock clock;

    void Awake()
    {
        _instance = this;

        clock = Clock.Instance;
        simulations = new SimulationsManager();

        if (PlayerPrefs.HasKey("Grid Size"))
            gridSize = PlayerPrefs.GetInt("Grid Size");
        if (PlayerPrefs.HasKey("Junction Gap"))
            junctionGap = PlayerPrefs.GetInt("Junction Gap");
        PlayerPrefs.DeleteAll();

        Generator.Generate();


        trainingManager = new TrainingManager();
    }

    void Update()
    {
        clock.Update();

        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("duplicate creation");

            simulations.MakeCopies(1);


        }
    }
}
