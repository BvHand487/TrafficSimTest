using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


[System.Serializable]
public class SimulationData
{
    public RoadData[] roadsData;
    public JunctionData[] junctionsData;
    public BuildingData[] buildingsData;
    public int[,][] adjacencyMatrix;
    public string prefabPath;

    public SimulationData(Simulation simulation)
    {
        var roadSerializationMap = new Dictionary<Road, RoadData>();
        var junctionSerializationMap = new Dictionary<Junction, JunctionData>();
        var buildingSerializationMap = new Dictionary<Building, BuildingData>();

        roadsData = new RoadData[simulation.roads.Count];
        junctionsData = new JunctionData[simulation.junctions.Count];
        buildingsData = new BuildingData[simulation.buildingManager.buildings.Count];

        // serialize all roads
        for (int i = 0; i < simulation.roads.Count; i++)
        {
            Road road = simulation.roads[i];
            roadsData[i] = new RoadData(road);
            roadSerializationMap.Add(road, roadsData[i]);
        }

        // serialize all junctions
        for (int i = 0; i < simulation.junctions.Count; i++)
        {
            Junction junction = simulation.junctions[i];
            junctionsData[i] = new JunctionData(junction);
            junctionSerializationMap.Add(junction, junctionsData[i]);
        }

        // serialize all buildings
        for (int i = 0; i < simulation.buildingManager.buildings.Count; i++)
        {
            Building building = simulation.buildingManager.buildings[i];
            buildingsData[i] = new BuildingData(building);
            buildingSerializationMap.Add(building, buildingsData[i]);
        }


        // save road connections
        adjacencyMatrix = new int[junctionsData.Length, junctionsData.Length][];

        for (int i = 0; i < junctionsData.Length; ++i)
        {
            Junction start = simulation.junctions[i];

            for (int j = i; j < junctionsData.Length; ++j)
            {
                Junction end = simulation.junctions[j];

                List<Road> commonRoads = start.roads.Intersect(end.roads).ToList();
                adjacencyMatrix[i, j] = new int[commonRoads.Count];

                for (int k = 0; k < commonRoads.Count; ++k)
                {
                    RoadData data = roadSerializationMap[commonRoads[k]];
                    int indexOfThisRoad = System.Array.IndexOf(roadsData, data);

                    adjacencyMatrix[i, j][k] = indexOfThisRoad;
                }
            }
        }


        this.prefabPath = $"Prefabs/{GameManager.Instance.simulationPrefab.name}";
    }
}