using System.Collections.Generic;
using System.Linq;
using Core;
using Core.Buildings;
using UnityEngine;

namespace Persistence
{
    [System.Serializable]
    public class SimulationData
    {
        [System.Serializable]
        public class AdjacentIndecesMatrix
        {
            public int nodes;
            public IndexList[] data;

            public AdjacentIndecesMatrix(int nodes)
            {
                this.nodes = nodes;
                data = new IndexList[nodes * nodes];

                for (int i = 0; i < data.Length; i++)
                    data[i] = new IndexList();
            }

            public void AddDoubleEdge(int fromIndex, int toIndex, int index)
            {
                if (fromIndex < 0 || fromIndex > nodes ||
                    toIndex < 0 || toIndex > nodes)
                    return;

                if (!data[fromIndex * nodes + toIndex].Contains(index))
                    data[fromIndex * nodes + toIndex].Add(index);

                if (!data[toIndex * nodes + fromIndex].Contains(index))
                    data[toIndex * nodes + fromIndex].Add(index);
            }

            public IEnumerable<IndexList> AsEnumerable() => data.AsEnumerable();

            public IndexList this[int i, int j] => data[i * nodes + j];
            public List<IndexList> this[int i] => Enumerable.Range(0, nodes).Select(index => this[i, index]).ToList();
        }

        [System.Serializable]
        public class IndexList
        {
            public List<int> indeces;

            public IndexList()
            {
                this.indeces = new List<int>();
            }

            public bool Contains(int index)
            {
                return indeces.Contains(index);
            }

            public void Add(int index)
            {
                indeces.Add(index);
            }

            public IEnumerable<int> AsEnumerable() => indeces.AsEnumerable();

            public int this[int i] => indeces[i];
        }

        public ViewData viewData;

        public ClockData clockData;

        public RoadData[] roadsData;
        public JunctionData[] junctionsData;
        public BuildingData[] buildingsData;
        public AdjacentIndecesMatrix roadConnections;  // for junction-road connections
        public List<IndexList> buildingConnections;  // for building-road connections

        public float[] pos;
        public float size;
        public string prefabPath;
    
        public SimulationData(Simulation simulation)
        {
            this.viewData = new ViewData(Camera.main.GetComponent<View>());

            this.clockData = new ClockData(Clock.Instance);

            this.pos = new float[3] {
                simulation.transform.position.x,
                simulation.transform.position.y,
                simulation.transform.position.z
            };

            this.size = simulation.transform.GetChild(0).localScale.x;

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


            roadConnections = new AdjacentIndecesMatrix(junctionsData.Length);

            for (int i = 0; i < junctionsData.Length; ++i)
            {
                Junction start = simulation.junctions[i];

                for (int j = 0; j < junctionsData.Length; ++j)
                {
                    Junction end = simulation.junctions[j];

                    List<Road> commonRoads = new List<Road>();
                    // if start and end junction are the same only get cyclic roads
                    if (start == end)
                        commonRoads = start.roads.Where(r => r.IsCyclic()).ToList();
                    else
                        commonRoads = start.roads.Intersect(end.roads).ToList();

                    for (int k = 0; k < commonRoads.Count; ++k)
                    {
                        RoadData data = roadSerializationMap[commonRoads[k]];
                        int indexOfThisRoad = System.Array.IndexOf(roadsData, data);

                        roadConnections.AddDoubleEdge(i, j, indexOfThisRoad);
                    }
                }
            }


            buildingConnections = new List<IndexList>();

            for (int i = 0; i < buildingsData.Length; ++i)
            {
                Building building = simulation.buildingManager.buildings[i];
                buildingConnections.Add(new IndexList());

                // what happens if a building is on a turn -> 2 road connections for 1 road?
                for (int k = 0; k < building.roads.Count; ++k)
                {
                    RoadData data = roadSerializationMap[building.roads[k]];
                    int indexOfThisRoad = System.Array.IndexOf(roadsData, data);

                    buildingConnections[i].Add(indexOfThisRoad);
                }
            }

            this.prefabPath = $"Prefabs/{GameManager.Instance.simulationPrefab.name}";
        }
    }
}