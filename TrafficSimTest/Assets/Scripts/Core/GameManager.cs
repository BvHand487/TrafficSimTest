using System;
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

        string loadMethod = PlayerPrefs.GetString("Load method");

        switch (loadMethod)
        {
            // generate a random simulation
            case "generate":
                {
                    if (PlayerPrefs.HasKey("Grid Size"))
                        gridSize = PlayerPrefs.GetInt("Grid Size");

                    if (PlayerPrefs.HasKey("Junction Gap"))
                        junctionGap = PlayerPrefs.GetInt("Junction Gap");

                    GameObject simulationObject = Instantiate(simulationPrefab, Vector3.zero, Quaternion.identity);
                    simulationObject.name = $"{simulationPrefab.name}";
                    simulationObject.SetActive(false);

                    simulation = simulationObject.GetComponent<Simulation>();
                    Generation.Generation.Generate(simulation.transform);
                }
                break;

            // generates a simulation from a save file
            case "file":
                {
                    SimulationData data = PersistenceManager.Instance.lastData;
                    simulationPrefab = Resources.Load(data.prefabPath) as GameObject;
                    GameObject simulationObject = Instantiate(simulationPrefab, Vector3.zero, Quaternion.identity);
                    simulationObject.name = $"{simulationPrefab.name}";
                    simulationObject.SetActive(false);

                    // instantiate roads
                    foreach (var r in data.roadsData)
                    {
                        for (int i = 0; i < r.prefabPaths.Length; ++i)
                        {
                            GameObject prefab = Resources.Load(r.prefabPaths[i]) as GameObject;

                            GameObject buildingObject = Generation.Instantiation.InstantiatePrefab(
                                prefab,
                                new Vector3(r.path[i][0], r.path[i][1], r.path[i][2]),
                                Quaternion.Euler(0f, r.rotYs[i], 0f),
                                simulationObject.transform
                            );
                        }
                    }

                    // instantiate junctions
                    foreach (var j in data.junctionsData)
                    {
                        GameObject prefab = Resources.Load(j.prefabPath) as GameObject;
                        GameObject junctionObject = Generation.Instantiation.InstantiatePrefab(
                            prefab,
                            new Vector3(j.pos[0], j.pos[1], j.pos[2]),
                            Quaternion.Euler(0f, j.rotY, 0f),
                            simulationObject.transform
                        );

                        Junction junction = junctionObject.GetComponent<Junction>();
                    }

                    // instantiate buildings
                    foreach (var b in data.buildingsData)
                    {
                        GameObject prefab = Resources.Load(b.prefabPath) as GameObject;
                        GameObject buildingObject = Generation.Instantiation.InstantiatePrefab(
                            prefab,
                            new Vector3(b.pos[0], b.pos[1], b.pos[2]),
                            Quaternion.identity,
                            simulationObject.transform);

                        Building building = buildingObject.GetComponent<Building>();
                        Enum.TryParse(b.typeName, out Building.Type type);
                        building.SetType(type);

                    }

                    // set references between junctions and roads
                    // ...

                    simulation = simulationObject.GetComponent<Simulation>();
                }
                break;
        }
               
        PlayerPrefs.DeleteAll();
    }

    private void Start()
    {
        simulation.gameObject.SetActive(true);
    }
}
