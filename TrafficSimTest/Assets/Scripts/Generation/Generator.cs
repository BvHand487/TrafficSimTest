using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Generation
{
    public static class Generator
    {
        public static void Generate()
        {
            GameManager game = GameManager.Instance;

            List<Road> roads;
            List<Junction> junctions;
            List<Building> buildings;
            float physicalSize = 0f;

            List<GridTile> tiles = new List<GridTile>()
            {
                new GridTile(GridTile.Type.Junction, GameManager.Instance.roadCrossPrefab, 0, "NESW", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, GameManager.Instance.roadJoinPrefab, 0, "WNE", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, GameManager.Instance.roadJoinPrefab, 90, "NES", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, GameManager.Instance.roadJoinPrefab, 180, "ESW", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, GameManager.Instance.roadJoinPrefab, 270, "SWN", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, GameManager.Instance.roadEndPrefab, 0, "N", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, GameManager.Instance.roadEndPrefab, 90, "E", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, GameManager.Instance.roadEndPrefab, 180, "S", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Junction, GameManager.Instance.roadEndPrefab, 270, "W", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Road, GameManager.Instance.roadStraightPrefab, 0, "NS", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Road, GameManager.Instance.roadStraightPrefab, 90, "WE", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Road, GameManager.Instance.roadTurnPrefab, 0, "NE", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Road, GameManager.Instance.roadTurnPrefab, 90, "ES", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Road, GameManager.Instance.roadTurnPrefab, 180, "SW", null, -Vector2Int.one),
                new GridTile(GridTile.Type.Road, GameManager.Instance.roadTurnPrefab, 270, "WN", null, -Vector2Int.one),
            };

            Grid grid = new Grid(GameManager.Instance.gridSize);

            WFC wfc = new WFC(tiles, grid);
            wfc.Run();

            // Remove unconnected grid tiles
            Generation.Optimization.KeepLargestRoadComponent(grid);

            // Spawn prefabs
            (roads, junctions, buildings) = GeneratePrefabs(tiles, grid, ref physicalSize);

            GameManager.Instance.simulations.Initialize(junctions.ToList(), roads.ToList(), buildings.ToList(), physicalSize);
        }

        // DFS algorithm that finds all neighbouring road tiles so that it can create a Road object
        public static void GetFullRoad(GridTile current, List<GridTile> road, List<GridTile> juncs, List<GridTile> builds, bool[,] visited)
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
        public static (List<Road>, List<Junction>, List<Building>) GeneratePrefabs(List<GridTile> tiles, Grid grid, ref float physicalSize)
        {
            List<Road> roads = new List<Road>();
            List<Junction> junctions = new List<Junction>();
            List<Building> buildings = new List<Building>();

            Dictionary<GridTile, Junction> juncsMap = new Dictionary<GridTile, Junction>();
            Dictionary<GridTile, Building> buildsMap = new Dictionary<GridTile, Building>();

            GridTile buildingTile = new GridTile(GridTile.Type.Building, GameManager.Instance.buildingPrefab, 0, "NESW", null, -Vector2Int.one);
            GridTile HorizontalRoadTile = tiles.Find(t => t.prefab == GameManager.Instance.roadStraightPrefab && t.rotY == 90);
            GridTile VerticalStraightRoadTile = tiles.Find(t => t.prefab == GameManager.Instance.roadStraightPrefab && t.rotY == 0);

            // Expands the grid by the junctionGap
            Grid expandedGrid = new Grid((GameManager.Instance.gridSize - 1) * GameManager.Instance.junctionGap + GameManager.Instance.gridSize + 2);

            for (int i = 0; i < GameManager.Instance.gridSize; i++)
                for (int j = 0; j < GameManager.Instance.gridSize; j++)
                    expandedGrid.tiles[i * (GameManager.Instance.junctionGap + 1) + 1, j * (GameManager.Instance.junctionGap + 1) + 1].SetTile(grid.tiles[i, j]);

            physicalSize = (expandedGrid.size + 2) * GameManager.Instance.tileSize;

            // Fill gap between spaced out tiles with straight roads
            List<(int, int, GridTile)> toAdd = new List<(int, int, GridTile)>();
            for (int i = 0; i < expandedGrid.size; ++i)
                for (int j = 0; j < expandedGrid.size; ++j)
                {
                    GridTile tile = expandedGrid.tiles[i, j];
                    if (!tile.IsValidTile()) continue;

                    for (int k = 1; k <= GameManager.Instance.junctionGap; ++k)
                    {
                        if (i + GameManager.Instance.junctionGap + 1 < expandedGrid.size && tile.CanConnectThroughRoad('E', expandedGrid.tiles[i + GameManager.Instance.junctionGap + 1, j]))
                            toAdd.Add((i + k, j, HorizontalRoadTile));

                        if (i - GameManager.Instance.junctionGap - 1 >= 0 && tile.CanConnectThroughRoad('W', expandedGrid.tiles[i - GameManager.Instance.junctionGap - 1, j]))
                            toAdd.Add((i - k, j, HorizontalRoadTile));

                        if (j + GameManager.Instance.junctionGap + 1 < expandedGrid.size && tile.CanConnectThroughRoad('N', expandedGrid.tiles[i, j + GameManager.Instance.junctionGap + 1]))
                            toAdd.Add((i, j + k, VerticalStraightRoadTile));

                        if (j - GameManager.Instance.junctionGap - 1 >= 0 && tile.CanConnectThroughRoad('S', expandedGrid.tiles[i, j - GameManager.Instance.junctionGap - 1]))
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
                    var obj = GameObject.Instantiate(tile.prefab, pos, Quaternion.Euler(0, tile.rotY, 0), GameManager.Instance.simulations.simulation.transform);
                    obj.SetActive(true);
                    obj.name = $"{tile.prefab.name}";

                    if (tile.type == GridTile.Type.Junction)
                        juncsMap.Add(tile, new Junction(GameManager.Instance.simulations.simulation, obj));

                    else if (tile.type == GridTile.Type.Building)
                    {
                        var factor = tile.DistanceToCenter() / expandedGrid.MaxDistanceFromCenter();
                        var buildingHeight = Utils.Modeling.BuildingHeightFromDistance(factor, GameManager.Instance.minBuildingHeight, GameManager.Instance.maxBuildingHeight, GameManager.Instance.buildingHeightDecay);
                        buildingHeight += GameManager.Instance.buildingHeightRandomness * (Random.value - 0.5f);
                        buildingHeight = Mathf.Clamp(Mathf.Ceil((buildingHeight) / GameManager.Instance.buildingHeightStep) * GameManager.Instance.buildingHeightStep, GameManager.Instance.minBuildingHeight, GameManager.Instance.maxBuildingHeight);

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
                        Road roadToAdd = new Road(GameManager.Instance.simulations.simulation, roadPath, juncsMap[juncs.First()], juncsMap[juncs.Last()]);
                        roads.Add(roadToAdd);

                        if (roadToAdd.junctionStart == roadToAdd.junctionEnd)
                        {
                            Road sameRoadCopy = new Road(GameManager.Instance.simulations.simulation,new List<Vector3>(roadToAdd.path), roadToAdd.junctionEnd, roadToAdd.junctionStart);
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
            junctions = juncsMap.Values.ToList();

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

            return (roads, junctions, buildings);
        }
    }
}