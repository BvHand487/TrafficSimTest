using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Generation
{
    public static class Instantiation
    {
        public static GameObject InstantiatePrefab(GameObject prefab, Vector3 position, Quaternion? rotation, Transform parent)
        {
            GameObject obj = GameObject.Instantiate(prefab, position, rotation ?? Quaternion.identity, parent);
            obj.name = $"{prefab.name}";
            obj.SetActive(true);
            return obj;
        }

        public static void InstantiateGrid(Grid grid, List<GridTile> tiles, Transform parent)
        {
            var gm = GameManager.Instance;

            List<Road> roads = new List<Road>();
            List<Junction> junctions = new List<Junction>();
            List<Building> buildings = new List<Building>();

            Dictionary<GridTile, Junction> junctionsMap = new Dictionary<GridTile, Junction>();
            Dictionary<GridTile, Building> buildingsMap = new Dictionary<GridTile, Building>();

            for (int i = 0; i < grid.size; ++i)
            {
                for (int j = 0; j < grid.size; ++j)
                {
                    GridTile tile = grid.GetTile(i, j);
                    if (!tile.IsValidTile()) continue;

                    Vector3 pos = tile.physicalPos;
                    Quaternion rot = Quaternion.Euler(0, tile.rotY, 0);

                    if (tile.type == GridTile.Type.Junction)
                    {
                        Junction junction = InstantiatePrefab(tile.prefab, pos, rot, parent).GetComponent<Junction>();
                        junctionsMap.Add(tile, junction);
                    }
                    else if (tile.type == GridTile.Type.Road)
                    {
                        InstantiatePrefab(tile.prefab, pos, rot, parent);
                    }
                    else if (tile.type == GridTile.Type.Building)
                    {
                        Building building = InstantiatePrefab(tile.prefab, pos, rot, parent).GetComponent<Building>();
                        buildingsMap.Add(tile, building);

                        var factor = tile.DistanceToCenter() / grid.MaxDistanceFromCenter();
                        var buildingHeight = Utils.Modeling.BuildingHeightFromDistance(factor, gm.minBuildingHeight, gm.maxBuildingHeight, gm.buildingHeightDecay);
                        buildingHeight += gm.buildingHeightRandomness * (Random.value - 0.5f);
                        buildingHeight = Mathf.Clamp(Mathf.Ceil((buildingHeight) / gm.buildingHeightStep) * gm.buildingHeightStep, gm.minBuildingHeight, gm.maxBuildingHeight);

                        var scale = building.transform.localScale;
                        scale.y = buildingHeight;
                        building.transform.localScale = scale;
                    }
                }
            }

            // Creates all Road objects 
            bool[,] visited = new bool[grid.size, grid.size];
            for (int i = 0; i < grid.size; ++i)
            {
                for (int j = 0; j < grid.size; ++j)
                {
                    GridTile tile = grid.GetTile(i, j);
                    if (!tile.IsValidTile()) continue;

                    if (tile.type == GridTile.Type.Road && !visited[i, j] && grid.tiles[i, j] != null)
                    {
                        List<GridTile> roadTiles = new List<GridTile>();
                        List<GridTile> junctionTiles = new List<GridTile>();
                        List<GridTile> buildingTiles = new List<GridTile>();

                        Optimization.GetFullRoad(tile, roadTiles, junctionTiles, buildingTiles, visited);

                        var roadPath = Utils.Math.OrderVectorPath(roadTiles.Select(r => r.physicalPos).ToList());
                        Road roadToAdd = new Road(
                            gm.simulation,
                            roadPath,
                            junctionsMap[junctionTiles.First()],
                            junctionsMap[junctionTiles.Last()]
                        );

                        roads.Add(roadToAdd);

                        if (roadToAdd.junctionStart == roadToAdd.junctionEnd)
                        {
                            Road sameRoadCopy = new Road(gm.simulation, new List<Vector3>(roadToAdd.path), roadToAdd.junctionEnd, roadToAdd.junctionStart);
                            sameRoadCopy.path.Reverse();
                            roads.Add(sameRoadCopy);
                        }

                        foreach (Building building in buildingTiles.FindAll(tile => roadTiles.Any(rt => GridTile.IsNeighbours(rt, tile))).Select(tile => buildingsMap[tile]))
                        {
                            building.spawnPoints.TryAdd(roadToAdd, Utils.Math.GetClosestVector(building.transform.position, roadToAdd.path));
                            if (!buildings.Contains(building))
                                buildings.Add(building);
                        }
                    }
                }
            }

            // End roads
            for (int i = 0; i < grid.size; ++i)
            {
                for (int j = 0; j < grid.size; ++j)
                {
                    GridTile tile = grid.GetTile(i, j);
                    if (!tile.IsValidTile()) continue;

                    if (tile.type == GridTile.Type.Junction && tile.validConnections.Count <= 2)
                    {
                        foreach (Building building in tile.GetNeighbours().FindAll(n => n.type == GridTile.Type.Building).Select(tile => buildingsMap[tile]))
                        {
                            Road road = roads.Find(r => r.junctionStart == junctionsMap[tile] || r.junctionEnd == junctionsMap[tile]);

                            Vector3 closestPoint = Utils.Math.GetClosestVector(building.transform.position, road.path);
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
            junctions = junctionsMap.Values.ToList();

            foreach (var j in junctions)
                j.Initialize(roads.FindAll((r) => r.IsConnectedTo(j)));
        }
    }
}
