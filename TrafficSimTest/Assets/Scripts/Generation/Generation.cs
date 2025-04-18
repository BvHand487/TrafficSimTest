using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Generation
{
    public static class Generation
    {
        public static readonly List<GridTile> tiles = new List<GridTile>() {
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

        public static void Generate(Transform parent)
        {
            GameManager gm = GameManager.Instance;

            Grid grid = new Grid(gm.gridSize);

            WFC wfc = new WFC(tiles, grid);
            wfc.Run();

            // Remove unconnected grid tiles
            Optimization.KeepLargestRoadComponent(grid);


            // Space out grid by junctionGap
            var expandedGrid = ExpandGrid(grid, gm.junctionGap);
            var buildingTile = new GridTile(GridTile.Type.Building, gm.buildingPrefab, 0, "NESW", null, -Vector2Int.one);

            // Fill empty spaces with straight roads && buildings
            FillStraightRoads(expandedGrid, gm.junctionGap);
            FillBuildings(expandedGrid, buildingTile);

            // TODO: refactor
            Transform ground = parent.transform.GetChild(0);

            var size = (expandedGrid.size + 2) * GameManager.TileSize;
            var scale = ground.localScale;
            scale.Scale(new Vector3(size, 1f, size));
            ground.localScale = scale;

            ground.localPosition += 0.025f * Vector3.down;

            Instantiation.InstantiateGrid(expandedGrid, new List<GridTile>(tiles) { buildingTile }, parent);
        }

        public static Grid ExpandGrid(Grid grid, int spacing)
        {
            Grid expandedGrid = new Grid((grid.size - 1) * spacing + grid.size + 2);

            for (int i = 0; i < grid.size; i++)
                for (int j = 0; j < grid.size; j++)
                {
                    var expandedI = i * (spacing + 1) + 1;
                    var expandedJ = j * (spacing + 1) + 1;

                    expandedGrid.tiles[expandedI, expandedJ].SetTile(grid.tiles[i, j]);
                }

            return expandedGrid;
        }

        public static void FillStraightRoads(Grid grid, int spacing)
        {
            var gm = GameManager.Instance;

            GridTile VerticalStraightRoadTile = tiles.Find(t => t.validConnections.Count == 2 && t.validConnections.Contains('N'));
            GridTile HorizontalRoadTile = tiles.Find(t => t.validConnections.Count == 2 && t.validConnections.Contains('E'));

            List<(int, int, GridTile)> toAdd = new List<(int, int, GridTile)>();

            for (int i = 0; i < grid.size; ++i)
                for (int j = 0; j < grid.size; ++j)
                {
                    GridTile tile = grid.tiles[i, j];
                    if (!tile.IsValidTile()) continue;

                    for (int k = 1; k <= spacing; ++k)
                    {
                        if (i + spacing + 1 < grid.size && tile.CanConnectThroughRoad('E', grid.tiles[i + spacing + 1, j]))
                            toAdd.Add((i + k, j, HorizontalRoadTile));

                        if (i - spacing - 1 >= 0 && tile.CanConnectThroughRoad('W', grid.tiles[i - spacing - 1, j]))
                            toAdd.Add((i - k, j, HorizontalRoadTile));

                        if (j + spacing + 1 < grid.size && tile.CanConnectThroughRoad('N', grid.tiles[i, j + spacing + 1]))
                            toAdd.Add((i, j + k, VerticalStraightRoadTile));

                        if (j - spacing - 1 >= 0 && tile.CanConnectThroughRoad('S', grid.tiles[i, j - spacing - 1]))
                            toAdd.Add((i, j - k, VerticalStraightRoadTile));
                    }
                }

            foreach (var a in toAdd)
                grid.tiles[a.Item1, a.Item2].SetTile(a.Item3);
        }

        public static void FillBuildings(Grid grid, GridTile building)
        {
            for (int i = 0; i < grid.size; ++i)
            {
                for (int j = 0; j < grid.size; ++j)
                {
                    GridTile tile = grid.GetTile(i, j);
                    if (!tile.IsValidTile())
                    {
                        if (tile.GetNeighbours().Any(n => n.IsValidTile() && n.validConnections.Count <= 2))
                            grid.GetTile(i, j).SetTile(building);
                    }
                }
            }
        }
    }
}