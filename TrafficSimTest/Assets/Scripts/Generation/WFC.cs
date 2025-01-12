using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Generation
{
    public class WFC
    {
        public List<GridTile> tiles;

        public Grid grid { get; private set; }
        public List<GridTile>[,] possibilities;
        private Generate parentScript;

        private float straightChance = 0, turnChance = 0, joinChance = 0, crossChance = 0, endChance = 0;

        public WFC(List<GridTile> tiles, Grid grid, Generate parentScript)
        {
            this.tiles = tiles;
            this.grid = grid;

            possibilities = new List<GridTile>[grid.size, grid.size];
            for (int i = 0; i < grid.size; ++i)
                for (int j = 0; j < grid.size; ++j)
                    possibilities[i, j] = new List<GridTile>(tiles);

            this.parentScript = parentScript;

            possibilities[grid.size / 2, grid.size / 2] = tiles.FindAll(t => t.prefab == parentScript.roadCrossPrefab);
            GridTile centerTile = CollapseTile(grid.size / 2, grid.size / 2);
            PropagateConstraints(centerTile);
        }

        public void Run()
        {
            if (tiles == null)
                return;

            while (true)
            {
                GridTile tile = FindLowestEntropyTile();

                if (tile.grid == null || tile.coords == -Vector2Int.one)
                    break;

                tile = CollapseTile(tile.coords.x, tile.coords.y);
                PropagateConstraints(tile);
            }
        }

        private GridTile FindLowestEntropyTile()
        {
            GridTile tile = GridTile.empty;
            int minEntropy = int.MaxValue;

            for (int i = 0; i < grid.size; ++i)
            {
                for (int j = 0; j < grid.size; ++j)
                {
                    if (possibilities[i, j] is null)
                        continue;

                    int entropy = possibilities[i, j].Count;

                    if (entropy < minEntropy)
                    {
                        minEntropy = entropy;
                        tile = grid.tiles[i, j];
                    }
                }
            }

            return tile;
        }

        GridTile CollapseTile(int x, int y)
        {
            GridTile tile = ChooseRandomTile(x, y);
            if (tile == null)
                tile = GridTile.empty;

            possibilities[x, y] = null;
            grid.tiles[x, y].SetTile(tile);
            return grid.tiles[x, y];
        }

        void PropagateConstraints(GridTile tile)
        {
            tile.ForNeighbours(neighbour =>
            {
                if (possibilities[neighbour.coords.x, neighbour.coords.y] != null)
                    possibilities[neighbour.coords.x, neighbour.coords.y].RemoveAll(p => !tile.CanConnect(tile.GetDirectionToTile(neighbour), p));
            });
        }

        GridTile ChooseRandomTile(int x, int y)
        {
            List<char> invalidEdgeConnections = new List<char>();
            if (x == 0)
                invalidEdgeConnections.Add('W');
            if (y == 0)
                invalidEdgeConnections.Add('S');
            if (x == grid.size - 1)
                invalidEdgeConnections.Add('E');
            if (y == grid.size - 1)
                invalidEdgeConnections.Add('N');

            // Select a tile which doesnt exit the grid when on the edge
            if (invalidEdgeConnections.Count > 0)
            {
                var possibleEdgeTiles = possibilities[x, y].FindAll(p => invalidEdgeConnections.All(c => !p.validConnections.Contains(c)));
                return Utils.Random.Select(possibleEdgeTiles);
            }

            float distanceFromCenter = grid.DistanceOfTileToCenter(x, y);
            float maxDistanceFromCenter = grid.MaxDistanceFromCenter();
            float factor = distanceFromCenter / maxDistanceFromCenter;

            // normalize
            var t = Mathf.InverseLerp(0, maxDistanceFromCenter, factor);
            factor = Mathf.Lerp(0, 1, t);

            SetProbabilityWeights(factor);
            float rand = UnityEngine.Random.value;

            List<GridTile> wantedTiles;

            // Cross road
            wantedTiles = possibilities[x, y].FindAll(p => p.prefab == parentScript.roadCrossPrefab);
            if (wantedTiles.Count > 0 && rand < crossChance)
                return Utils.Random.Select(wantedTiles);

            // Join road
            wantedTiles = possibilities[x, y].FindAll(p => p.prefab == parentScript.roadJoinPrefab);
            if (wantedTiles.Count > 0 && rand < (joinChance + crossChance))
                return Utils.Random.Select(wantedTiles);

            // Straight road
            wantedTiles = possibilities[x, y].FindAll(p => p.prefab == parentScript.roadStraightPrefab);
            if (wantedTiles.Count > 0 && rand < (joinChance + crossChance + straightChance))
                return Utils.Random.Select(wantedTiles);

            // Turn road
            wantedTiles = possibilities[x, y].FindAll(p => p.prefab == parentScript.roadTurnPrefab);
            if (wantedTiles.Count > 0 && rand < (joinChance + crossChance + straightChance + turnChance))
                return Utils.Random.Select(wantedTiles);

            // End road
            wantedTiles = possibilities[x, y].FindAll(p => p.prefab == parentScript.roadEndPrefab);
            if (wantedTiles.Count > 0)
                return Utils.Random.Select(wantedTiles);

            // Select randomly if we get here
            return Utils.Random.Select(possibilities[x, y]);
        }

        void SetProbabilityWeights(float factor)
        {
            //crossChance = Mathf.Sin(factor * Mathf.PI + Mathf.PI / 2 - 0 * Mathf.PI / 5) / 2 + 0.5f;
            //joinChance = Mathf.Sin(factor * Mathf.PI + Mathf.PI / 2 - 1 * Mathf.PI / 5) / 2 + 0.5f;
            //straightChance = Mathf.Sin(factor * Mathf.PI + Mathf.PI / 2 - 0 * Mathf.PI / 5) / 2 + 0.5f;
            //turnChance = Mathf.Sin(factor * Mathf.PI + Mathf.PI / 2 - 0 * Mathf.PI / 5) / 2 + 0.5f;
            //endChance = Mathf.Sin(factor * Mathf.PI + Mathf.PI / 2 - 0 * Mathf.PI / 5) / 2 + 0.5f;

            //return -1.53884176851f * Mathf.Pow(factor - 1, 2) + 4.5388417686f;

            var crossFunc = Utils.Math.NormalDistribution(factor, 0.15f, 0f) + Utils.Math.NormalDistribution(factor, 0.15f, 0.8f);
            var joinFunc = Utils.Math.NormalDistribution(factor, 0.15f, 0.15f) + Utils.Math.NormalDistribution(factor, 0.15f, 0.65f);
            var straightFunc = 0.8f - 0.5f * factor + 0.5f * Utils.Math.NormalDistribution(factor, 0.3f, 0.65f);
            var turnFunc = Utils.Math.NormalDistribution(factor, 0.2f, 0.25f) + Utils.Math.NormalDistribution(factor, 0.2f, 0.75f);
            var endFunc = Utils.Math.NormalDistribution(factor, 0.15f, 1f) + 0.5f * Utils.Math.NormalDistribution(factor, 0.5f, 0.2f);

            var norm = crossFunc + joinFunc + straightFunc + turnFunc + endFunc;

            crossChance = crossFunc / norm;
            joinChance = joinFunc / norm;
            straightChance = straightFunc / norm;
            turnChance = turnFunc / norm;
            endChance = endFunc / norm;
        }
    }
}
