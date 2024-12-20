﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Generation
{
    public class Generate : MonoBehaviour
    {
        [SerializeField] private int gridSize = 50;
        [SerializeField] private int junctionGap = 5;
        public static readonly float tileSize = 15;

        [SerializeField] private int minBuildingHeight;
        [SerializeField] private int maxBuildingHeight;
        [SerializeField] private float buildingHeightStep = 2.0f;
        [SerializeField] private float buildingHeightDecay;
        [SerializeField] private float buildingHeightRandomness = 2.0f;

        [SerializeField] public Vector2 center = Vector2.zero;

        [SerializeField] public GameObject roadStraightPrefab, roadTurnPrefab, roadJoinPrefab, roadCrossPrefab, roadEndPrefab, buildingPrefab, simulation;

        private Grid grid;
        private WFC wfc;

        private List<GridTile> tiles;

        private List<Road> roads = new List<Road>();
        private List<Junction> junctions = new List<Junction>();
        private List<Building> buildings = new List<Building>();

        // Load data from main menu using the PlayerRrefs API
        private void Awake()
        {
            if (PlayerPrefs.HasKey("Grid Size"))
                gridSize = PlayerPrefs.GetInt("Grid Size");

            if (PlayerPrefs.HasKey("Junction Gap"))
                junctionGap = PlayerPrefs.GetInt("Junction Gap");

            PlayerPrefs.DeleteAll();
        }

        void Start()
        {
            tiles = new List<GridTile>()
            {
                new GridTile(GridTile.Type.Junction, roadCrossPrefab, 0, "NESW", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, roadJoinPrefab, 0, "WNE", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, roadJoinPrefab, 90, "NES", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, roadJoinPrefab, 180, "ESW", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, roadJoinPrefab, 270, "SWN", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, roadEndPrefab, 0, "N", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, roadEndPrefab, 90, "E", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, roadEndPrefab, 180, "S", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, roadEndPrefab, 270, "W", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Road, roadStraightPrefab, 0, "NS", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Road, roadStraightPrefab, 90, "WE", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Road, roadTurnPrefab, 0, "NE", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Road, roadTurnPrefab, 90, "ES", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Road, roadTurnPrefab, 180, "SW", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Road, roadTurnPrefab, 270, "WN", null, -Vector2Int.one),
            };

            grid = new Grid(gridSize, center);

            wfc = new WFC(tiles, grid, this);
            wfc.Run();

            // Remove unconnected grid tiles
            Generation.Optimization.KeepLargestRoadComponent(grid);

            // Spawn prefabs
            GeneratePrefabs();

            simulation.GetComponent<Simulation>().Initialize(junctions, roads, buildings);
            simulation.SetActive(true);

            Destroy(this.gameObject);
        }

        // DFS algorithm that finds all neighbouring road tiles so that it can create a Road object
        void GetFullRoad(GridTile current, List<GridTile> road, List<GridTile> juncs, List<GridTile> builds, bool[,] visited)
        {
            if (current == null || !current.IsValidTile())
                return;

            if (current.type == GridTile.Type.Junction)
            {
                juncs.Add(current);
                return;
            }

            if (current.type == GridTile.Type.Building)
            {
                builds.Add(current);
                return;
            }

            if (visited[current.coords.x, current.coords.y]) return;
            visited[current.coords.x, current.coords.y] = true;

            road.Add(current);

            current.ForNeighbours(neighbour =>
            {
                if (neighbour.IsValidTile() && current.CanConnectThroughRoad(current.GetDirectionToTile(neighbour), neighbour) || neighbour.type == GridTile.Type.Building)
                    GetFullRoad(neighbour, road, juncs, builds, visited);
            });
        }

        // Genereates the prefabs from the grid and initializes Junction and Road classes
        void GeneratePrefabs()
        {
            Dictionary<GridTile, Junction> juncsMap = new Dictionary<GridTile, Junction>();
            Dictionary<GridTile, Building> buildsMap = new Dictionary<GridTile, Building>();

            GridTile buildingTile = new GridTile(GridTile.Type.Building, buildingPrefab, 0, "NESW", null, -Vector2Int.one);
            GridTile HorizontalRoadTile = tiles.Find(t => t.prefab == roadStraightPrefab && t.rotY == 90);
            GridTile VerticalStraightRoadTile = tiles.Find(t => t.prefab == roadStraightPrefab && t.rotY == 0);

            // Expands the grid by the number of blockCells
            // gridSize = (gridSize - 1) * blockCells + gridSize + 2;
            Grid expandedGrid = new Grid((gridSize - 1) * junctionGap + gridSize + 2, center);

            for (int i = 0; i < gridSize; i++)
                for (int j = 0; j < gridSize; j++)
                    expandedGrid.tiles[i * (junctionGap + 1) + 1, j * (junctionGap + 1) + 1].SetTile(grid.tiles[i, j]);

            // Fill gap between spaced out tiles with straight roads
            List<(int, int, GridTile)> toAdd = new List<(int, int, GridTile)>();
            for (int i = 0; i < expandedGrid.size; ++i)
                for (int j = 0; j < expandedGrid.size; ++j)
                {
                    GridTile tile = expandedGrid.tiles[i, j];
                    if (!tile.IsValidTile()) continue;

                    for (int k = 1; k <= junctionGap; ++k)
                    {
                        if (i + junctionGap + 1 < expandedGrid.size && tile.CanConnectThroughRoad('E', expandedGrid.tiles[i + junctionGap + 1, j]))
                            toAdd.Add((i + k, j, HorizontalRoadTile));

                        if (i - junctionGap - 1 >= 0 && tile.CanConnectThroughRoad('W', expandedGrid.tiles[i - junctionGap - 1, j]))
                            toAdd.Add((i - k, j, HorizontalRoadTile));

                        if (j + junctionGap + 1 < expandedGrid.size && tile.CanConnectThroughRoad('N', expandedGrid.tiles[i, j + junctionGap + 1]))
                            toAdd.Add((i, j + k, VerticalStraightRoadTile));

                        if (j - junctionGap - 1 >= 0 && tile.CanConnectThroughRoad('S', expandedGrid.tiles[i, j - junctionGap - 1]))
                            toAdd.Add((i, j - k, VerticalStraightRoadTile));
                    }
                }
            foreach (var a in toAdd)
                expandedGrid.tiles[a.Item1, a.Item2].SetTile(a.Item3);


            // Add buildings to grid and instantiate
            for (int i = 0; i < expandedGrid.size; ++i)
            {
                for (int j = 0; j < expandedGrid.size; ++j)
                {
                    GridTile tile = expandedGrid.tiles[i, j];
                    if (!tile.IsValidTile())
                    {
                        if (tile.GetNeighbours().Any(n => n.IsValidTile() && n.validConnections.Count <= 2))
                            expandedGrid.tiles[i, j].SetTile(buildingTile);
                    }
                }
            }

            // Instantiate all prefabs and create Junction/Building objects
            for (int i = 0; i < expandedGrid.size; ++i)
            {
                for (int j = 0; j < expandedGrid.size; ++j)
                {
                    GridTile tile = expandedGrid.tiles[i, j];
                    if (!tile.IsValidTile()) continue;

                    Vector3 pos = tile.physicalPos;
                    var obj = Instantiate(tile.prefab, pos, Quaternion.Euler(0, tile.rotY, 0));
                    obj.SetActive(true);
                    obj.name = $"{tile.prefab.name}";

                    if (tile.type == GridTile.Type.Junction)
                        juncsMap.Add(expandedGrid.tiles[i, j], new Junction(obj));

                    else if (tile.type == GridTile.Type.Building)
                    {
                        var factor = tile.DistanceToCenter() / expandedGrid.MaxDistanceFromCenter();
                        var buildingHeight = Utils.Modeling.BuildingHeightFromDistance(factor, minBuildingHeight, maxBuildingHeight, buildingHeightDecay);
                        buildingHeight += buildingHeightRandomness * (Random.value - 0.5f);
                        buildingHeight = Mathf.Clamp(Mathf.Ceil((buildingHeight) / buildingHeightStep) * buildingHeightStep, minBuildingHeight, maxBuildingHeight);

                        var scale = obj.transform.localScale;
                        scale.y = buildingHeight;
                        obj.transform.localScale = scale;

                        buildsMap.Add(expandedGrid.tiles[i, j], new Building(obj, ChooseRandomBuildingType(obj.transform, expandedGrid)));
                    }
                }
            }

            // Creates all Road objects 
            bool[,] visited = new bool[expandedGrid.size, expandedGrid.size];
            for (int i = 0; i < expandedGrid.size; ++i)
            {
                for (int j = 0; j < expandedGrid.size; ++j)
                {
                    GridTile tile = expandedGrid.tiles[i, j];
                    if (!tile.IsValidTile()) continue;

                    if (tile.type == GridTile.Type.Road && !visited[i, j] && expandedGrid.tiles[i, j] != null)
                    {
                        List<GridTile> roadTiles = new List<GridTile>();
                        List<GridTile> juncs = new List<GridTile>();
                        List<GridTile> builds = new List<GridTile>();

                        GetFullRoad(tile, roadTiles, juncs, builds, visited);

                        var roadPath = Utils.Math.OrderVectorPath(roadTiles.Select(r => r.physicalPos).ToList());
                        var roadToAdd = new Road(roadPath, juncsMap[juncs[0]], juncsMap[juncs[1]]);
                        roads.Add(roadToAdd);

                        foreach (Building building in builds.Select(tile => buildsMap[tile]))
                        {
                            building.adjacentRoads.Add(roadToAdd);

                            if (!buildings.Contains(building))
                                buildings.Add(building);
                        }
                    }
                }
            }

            foreach (var b in buildings)
                Debug.Log(b.adjacentRoads.Count);

            // Sets the references in the Junction and Road objects
            junctions = juncsMap.Values.ToList();

            foreach (var j in junctions)
                j.Initialize(roads.FindAll((r) => r.junctionStart == j || r.junctionEnd == j), ChooseRandomJunctionType(j.obj.transform, expandedGrid));
        }

        Building.Type ChooseRandomBuildingType(Transform b, Grid g)
        {
            var maxDistFromCenter = g.MaxDistanceFromCenter();
            var distFromCenter = Vector3.Distance(b.position, new Vector3(center.x, 0, center.y));

            if (Utils.Math.NormalDistribution(distFromCenter / maxDistFromCenter, 0.32f) > UnityEngine.Random.value)
                return Building.Type.Work;
            else
                return Building.Type.Home;
        }

        Junction.Type ChooseRandomJunctionType(Transform j, Grid g)
        {
            var maxDistFromCenter = g.MaxDistanceFromCenter();
            var distFromCenter = Vector3.Distance(j.position, new Vector3(center.x, 0, center.y));

            if (Utils.Math.NormalDistribution(distFromCenter / maxDistFromCenter, 0.65f) > UnityEngine.Random.value)
                return Junction.Type.Lights;
            else
                return Junction.Type.Stops;
        }
    }

}