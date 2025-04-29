using System;
using System.Collections.Generic;
using System.Linq;
using Core.Buildings;
using Core.Vehicles;
using Persistence;
using UnityEngine;

namespace Core
{
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
        public static float TileSize = 15f;
        [SerializeField] public int gridSize = 3;
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

            if (string.IsNullOrEmpty(loadMethod))
                loadMethod = "generate";

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
                    simulationObject.SetActive(true);

                    simulation = simulationObject.GetComponent<Simulation>();
                    Generation.Generation.Generate(simulation.transform);
                }
                    break;

                // generates a simulation from a save file
                case "file":
                {
                    // TODO: refactor into separate file/method

                    SimulationData data = PersistenceManager.Instance.lastData;

                    simulationPrefab = Resources.Load(data.prefabPath) as GameObject;
                    
                    GameObject simulationObject = Instantiate(simulationPrefab, Vector3.zero, Quaternion.identity);
                    simulationObject.name = $"{simulationPrefab.name}";
                    simulationObject.transform.position = new Vector3(data.pos[0], data.pos[1], data.pos[2]);
                    simulation = simulationObject.GetComponent<Simulation>();

                    // get ground and set its values
                    Transform ground = simulationObject.transform.GetChild(0);
                    ground.localScale = new Vector3(data.size, 1f, data.size);
                    ground.localPosition += 0.025f * Vector3.down;


                    List<Road> roads = new List<Road>();
                    List<Junction> junctions = new List<Junction>();
                    List<Building> buildings = new List<Building>();


                    // instantiate roads
                    foreach (var r in data.roadsData)
                    {
                        var tempPath2DArray = Utils.Arrays.To2DArray(r.path, r.path.Length / 3, 3);

                        for (int i = 0; i < r.prefabPaths.Length; ++i)
                        {
                            GameObject prefab = Resources.Load(r.prefabPaths[i]) as GameObject;

                            GameObject roadObject = Generation.Instantiation.InstantiatePrefab(
                                prefab,
                                new Vector3(tempPath2DArray[i, 0], tempPath2DArray[i, 1], tempPath2DArray[i, 2]),
                                Quaternion.Euler(0f, r.rotYs[i], 0f),
                                simulationObject.transform
                            );

                        }

                        var road = new Road(
                            simulation,
                            Enumerable.Range(0, tempPath2DArray.GetLength(0)).Select(i => new Vector3(tempPath2DArray[i, 0], tempPath2DArray[i, 1], tempPath2DArray[i, 2])).ToList(),
                            null,
                            null
                        );

                        roads.Add(road);
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
                        junctions.Add(junction);
                    }

                    // instantiate buildings
                    foreach (var b in data.buildingsData)
                    {
                        GameObject prefab = Resources.Load(b.prefabPath) as GameObject;
                        GameObject buildingObject = Generation.Instantiation.InstantiatePrefab(
                            prefab,
                            new Vector3(b.pos[0], b.pos[1], b.pos[2]),
                            Quaternion.identity,
                            simulationObject.transform
                        );

                        // set building height
                        var scale = buildingObject.transform.localScale;
                        scale.y = b.height;
                        buildingObject.transform.localScale = scale;

                        Building building = buildingObject.GetComponent<Building>();
                        Enum.TryParse(b.typeName, out Building.Type type);
                        building.SetType(type);

                        buildings.Add(building);
                    }


                    // set references between junctions and roads
                    for (int i = 0; i < data.junctionsData.Length; ++i)
                    {
                        Junction start = junctions[i];

                        for (int j = i; j < data.junctionsData.Length; ++j)
                        {
                            List<Road> connectedRoads = data.roadConnections[i, j].AsEnumerable()
                                .Select(index => roads[index]).ToList(); 

                            Junction end = junctions[j];

                            foreach (var r in connectedRoads)
                            {
                                r.junctionStart = start;
                                r.junctionEnd = end;
                            }
                        }
                    }

                    for (int i = 0; i < data.junctionsData.Length; ++i)
                    {
                        Junction junction = junctions[i];

                        List<Road> connectedRoads = data.roadConnections[i]
                            .Where(list => list.indeces.Count > 0)
                            .SelectMany(list => list.indeces)
                            .Select(index => roads[index]).ToList();

                        junction.Initialize(connectedRoads);
                    }

                    // set references between buildings and roads
                    for (int i = 0; i < data.buildingsData.Length; ++i)
                    {
                        Building building = buildings[i];

                        var roadIndeces = data.buildingConnections[i];
                        List<Road> adjacentRoads = roadIndeces.AsEnumerable().Select(index => roads[index]).ToList();

                        building.roads = adjacentRoads;
                    }

                    // sets time information
                    Clock.Instance.datetime = new DateTime(data.clockData.timeTicks);
                    Clock.Instance.timeScale = data.clockData.timeScale;
                    Clock.Instance.SetPaused(data.clockData.isPaused);

                    // sets camera information
                    Camera.main.transform.position = new Vector3(data.viewData.pos[0], data.viewData.pos[1], data.viewData.pos[2]);
                }
                    break;
            }
               
            PlayerPrefs.DeleteAll();
        }
    }
}
