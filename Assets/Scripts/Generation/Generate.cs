using System.Collections;
using System.Collections.Generic;
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

        [SerializeField] public GameObject roadStraightPrefab, roadTurnPrefab, roadJoinPrefab, roadCrossPrefab, roadEndPrefab, buildingPrefab, groundPrefab;

        private Grid grid;
        private WFC wfc;

        private List<GridTile> tiles;

        public Simulation simulation;
        private List<Road> roads = new List<Road>();
        private HashSet<Junction> junctions = new HashSet<Junction>();
        private HashSet<Building> buildings = new HashSet<Building>();

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

            grid = new Grid(gridSize);

            wfc = new WFC(tiles, grid, this);
            wfc.Run();

            // Remove unconnected grid tiles
            Generation.Optimization.KeepLargestRoadComponent(grid);

            // Spawn prefabs
            GeneratePrefabs();

            simulation.Initialize(junctions.ToList(), roads.ToList(), buildings.ToList());
            simulation.gameObject.SetActive(true);

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

            // Expands the grid by the junctionGap
            Grid expandedGrid = new Grid((gridSize - 1) * junctionGap + gridSize + 2);

            for (int i = 0; i < gridSize; i++)
                for (int j = 0; j < gridSize; j++)
                    expandedGrid.tiles[i * (junctionGap + 1) + 1, j * (junctionGap + 1) + 1].SetTile(grid.tiles[i, j]);

            // Spawn ground
            {
                var ground = Instantiate(groundPrefab, Vector3.zero, Quaternion.identity, simulation.transform);
                ground.name = groundPrefab.name;
            
                var scale = ground.transform.localScale;
                var groundSize = expandedGrid.size + 2;
                scale.Scale(new Vector3(groundSize, 1f, groundSize));
                ground.transform.localScale = scale;
            }

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
                    GridTile tile = expandedGrid.GetTile(i, j);
                    if (!tile.IsValidTile())
                    {
                        if (tile.GetNeighbours().Any(n => n.IsValidTile() && n.validConnections.Count <= 2))
                            expandedGrid.GetTile(i, j).SetTile(buildingTile);
                    }
                }
            }

            // Instantiate all prefabs and create Junction/Building objects
            for (int i = 0; i < expandedGrid.size; ++i)
            {
                for (int j = 0; j < expandedGrid.size; ++j)
                {
                    GridTile tile = expandedGrid.GetTile(i, j);
                    if (!tile.IsValidTile()) continue;

                    Vector3 pos = tile.physicalPos;
                    var obj = Instantiate(tile.prefab, pos, Quaternion.Euler(0, tile.rotY, 0), simulation.transform);
                    obj.SetActive(true);
                    obj.name = $"{tile.prefab.name}";

                    if (tile.type == GridTile.Type.Junction)
                        juncsMap.Add(tile, new Junction(obj));

                    else if (tile.type == GridTile.Type.Building)
                    {
                        var factor = tile.DistanceToCenter() / expandedGrid.MaxDistanceFromCenter();
                        var buildingHeight = Utils.Modeling.BuildingHeightFromDistance(factor, minBuildingHeight, maxBuildingHeight, buildingHeightDecay);
                        buildingHeight += buildingHeightRandomness * (Random.value - 0.5f);
                        buildingHeight = Mathf.Clamp(Mathf.Ceil((buildingHeight) / buildingHeightStep) * buildingHeightStep, minBuildingHeight, maxBuildingHeight);

                        var scale = obj.transform.localScale;
                        scale.y = buildingHeight;
                        obj.transform.localScale = scale;

                        buildsMap.Add(tile, new Building(obj));
                    }
                }
            }

            // Creates all Road objects 
            bool[,] visited = new bool[expandedGrid.size, expandedGrid.size];
            for (int i = 0; i < expandedGrid.size; ++i)
            {
                for (int j = 0; j < expandedGrid.size; ++j)
                {
                    GridTile tile = expandedGrid.GetTile(i, j);
                    if (!tile.IsValidTile()) continue;

                    if (tile.type == GridTile.Type.Road && !visited[i, j] && expandedGrid.tiles[i, j] != null)
                    {
                        List<GridTile> roadTiles = new List<GridTile>();
                        List<GridTile> juncs = new List<GridTile>();
                        List<GridTile> builds = new List<GridTile>();

                        GetFullRoad(tile, roadTiles, juncs, builds, visited);

                        var roadPath = Utils.Math.OrderVectorPath(roadTiles.Select(r => r.physicalPos).ToList());
                        Road roadToAdd = new Road(roadPath, juncsMap[juncs.First()], juncsMap[juncs.Last()]);
                        roads.Add(roadToAdd);

                        if (roadToAdd.junctionStart == roadToAdd.junctionEnd)
                        {
                            Road sameRoadCopy = new Road(new List<Vector3>(roadToAdd.path), roadToAdd.junctionEnd, roadToAdd.junctionStart);
                            sameRoadCopy.path.Reverse();
                            roads.Add(sameRoadCopy);
                        }

                        foreach (Building building in builds.FindAll(tile => roadTiles.Any(rt => GridTile.IsNeighbours(rt, tile))).Select(tile => buildsMap[tile]))
                        {
                            building.spawnPoints.TryAdd(roadToAdd, Utils.Math.GetClosestVector(building.obj.transform.position, roadToAdd.path));   
                            if (!buildings.Contains(building))
                                buildings.Add(building);
                        }
                    }
                }
            }

            for (int i = 0; i < expandedGrid.size; ++i)
            {
                for (int j = 0; j < expandedGrid.size; ++j)
                {
                    GridTile tile = expandedGrid.GetTile(i, j);
                    if (!tile.IsValidTile()) continue;

                    if (tile.type == GridTile.Type.Junction && tile.validConnections.Count <= 2)
                    {
                        foreach (Building building in tile.GetNeighbours().FindAll(n => n.type == GridTile.Type.Building).Select(tile => buildsMap[tile]))
                        {
                            Road road = roads.Find(r => r.junctionStart == juncsMap[tile] || r.junctionEnd == juncsMap[tile]);

                            Vector3 closestPoint = Utils.Math.GetClosestVector(building.obj.transform.position, road.path);
                            int closestPointIndex = road.path.IndexOf(closestPoint);
                            int nextPointIndex = closestPointIndex == 0 ? 1 : road.path.Count - 2;

                            Vector3 continuationDir = closestPoint - road.path[nextPointIndex];
                            building.spawnPoints.TryAdd(road, closestPoint + continuationDir);
                            if (!buildings.Contains(building))
                                buildings.Add(building);
                        }
                    }
                }
            }

            // Sets the references in the Junction and Road objects
            junctions = juncsMap.Values.ToHashSet();

            foreach (var j in junctions)
            {
                var maxDistFromCenter = expandedGrid.MaxDistanceFromCenter();
                var distFromCenter = j.obj.transform.position.magnitude;
                j.Initialize(roads.FindAll((r) => r.junctionStart == j || r.junctionEnd == j), Utils.Modeling.ChooseRandomJunctionType(distFromCenter / maxDistFromCenter));
            }

            foreach (var b in buildings)
            {
                var maxDistFromCenter = expandedGrid.MaxDistanceFromCenter();
                var distFromCenter = b.obj.transform.position.magnitude;
                b.Initialize(Utils.Modeling.ChooseRandomBuildingType(distFromCenter / maxDistFromCenter));
            }
        }
    }

}