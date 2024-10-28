using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Generate : MonoBehaviour
{
    [SerializeField]
    private int gridSize = 50;

    [SerializeField]
    private float cellSize = 15;

    [SerializeField]
    private int blockCells = 5;

    [SerializeField]
    private Vector2 center = Vector2.zero;
    private Vector2 centerOffset = Vector2.zero;
    private float maxDistanceFromCenter = 1.0f;

    [SerializeField]
    public GameObject roadStraightPrefab, roadTurnPrefab, roadJoinPrefab, roadCrossPrefab, roadEndPrefab;
    private float straightChance = 0, turnChance = 0, joinChance = 0, crossChance = 0, endChance = 0;

    private RoadTile[,] grid;
    private List<RoadTile> tiles;
    private List<RoadTile>[,] possibilities;

    private int[,] spawnGrid;
    private int spawnCount = 1;

    void Start()
    {
        spawnGrid = new int[gridSize, gridSize];

        grid = new RoadTile[gridSize, gridSize];

        centerOffset = new Vector2((gridSize - 1) * cellSize / 2 - center.x, (gridSize - 1) * cellSize / 2 - center.y);
        maxDistanceFromCenter = Vector2.Distance(-centerOffset, center) / (gridSize * cellSize * Mathf.Sqrt(2) / 2);

        tiles = new List<RoadTile>()
        {
            new RoadTile(roadStraightPrefab, 0, new[] { 'N', 'S' }, new[] { 'E', 'W' }),
            new RoadTile(roadStraightPrefab, 90, new[] { 'W', 'E' }, new[] { 'N', 'S' }),
            new RoadTile(roadTurnPrefab, 90, new[] { 'S', 'E' }, new[] { 'W', 'N' }), // leave
            new RoadTile(roadTurnPrefab, 180, new[] { 'S', 'W' }, new[] { 'N', 'E' }),  // ?
            new RoadTile(roadTurnPrefab, 270, new[] { 'N', 'W' }, new[] { 'S', 'E' }),  // rotate
            new RoadTile(roadTurnPrefab, 0, new[] { 'N', 'E' }, new[] { 'S', 'W' }),  // ?
            new RoadTile(roadJoinPrefab, 0, new[] { 'N', 'E', 'S' }, new[] { 'W' }),  // ?
            new RoadTile(roadJoinPrefab, 180, new[] { 'N', 'W', 'S' }, new[] { 'E' }), // ?
            new RoadTile(roadJoinPrefab, 90, new[] { 'W', 'S', 'E' }, new[] { 'N' }),  // ?
            new RoadTile(roadJoinPrefab, 270, new[] { 'W', 'N', 'E' }, new[] { 'S' }), // ?
            new RoadTile(roadCrossPrefab, 0, new[] { 'N', 'S', 'E', 'W' }, new char[] {}),
            new RoadTile(roadEndPrefab, 0, new[] { 'S' }, new[] { 'N', 'E', 'W' }),
            new RoadTile(roadEndPrefab, 90, new[] { 'W' }, new[] { 'S', 'N', 'E' }),
            new RoadTile(roadEndPrefab, 180, new[] { 'N' }, new[] { 'S', 'E', 'W' }),
            new RoadTile(roadEndPrefab, 270, new[] { 'E' }, new[] { 'S', 'W', 'N' }),
        };


        //new RoadTile(roadStraightPrefab, 90, new[] { 'W', 'E' }, new[] { 'N', 'S' }), // ok
        //    new RoadTile(roadStraightPrefab, 0, new[] { 'N', 'S' }, new[] { 'E', 'W' }), // ok
        //    new RoadTile(roadTurnPrefab, 90, new[] { 'W', 'N' }, new[] { 'E', 'S' }), // leave
        //    new RoadTile(roadTurnPrefab, 180, new[] { 'N', 'E' }, new[] { 'W', 'S' }),  // ?
        //    new RoadTile(roadTurnPrefab, 270, new[] { 'S', 'E' }, new[] { 'N', 'W' }),  // rotate
        //    new RoadTile(roadTurnPrefab, 0, new[] { 'S', 'W' }, new[] { 'E', 'N' }),  // ?
        //    new RoadTile(roadJoinPrefab, 0, new[] { 'N', 'S', 'W' }, new[] { 'E' }),  // ?
        //    new RoadTile(roadJoinPrefab, 180, new[] { 'N', 'S', 'E' }, new[] { 'W' }), // ?
        //    new RoadTile(roadJoinPrefab, 90, new[] { 'W', 'N', 'E' }, new[] { 'S' }),  // ?
        //    new RoadTile(roadJoinPrefab, 270, new[] { 'W', 'S', 'E' }, new[] { 'N' }), // ?
        //    new RoadTile(roadCrossPrefab, 0, new[] { 'N', 'S', 'E', 'W' }, new char[] { }),
        //    new RoadTile(roadEndPrefab, 0, new[] { 'N' }, new[] { 'S', 'E', 'W' }),
        //    new RoadTile(roadEndPrefab, 90, new[] { 'E' }, new[] { 'S', 'N', 'W' }),
        //    new RoadTile(roadEndPrefab, 180, new[] { 'S' }, new[] { 'N', 'E', 'W' }),
        //    new RoadTile(roadEndPrefab, 270, new[] { 'W' }, new[] { 'S', 'E', 'N' }),


        possibilities = new List<RoadTile>[gridSize, gridSize];
        for (int i = 0; i < gridSize; ++i)
            for (int j = 0; j < gridSize; ++j)
            {
                possibilities[i, j] = new List<RoadTile>(tiles);
            }

        possibilities[gridSize / 2, gridSize / 2] = null;
        grid[gridSize / 2, gridSize / 2] = tiles.Find(t => t.prefab == roadCrossPrefab);
        spawnGrid[gridSize / 2, gridSize / 2] = spawnCount;
        spawnCount++;
        PropagateConstraints(gridSize / 2, gridSize / 2);

        // WFC
        GenerateGrid();

        // Spawn prefabs
        GeneratePrefabs();
    }
    
    void GenerateGrid()
    {
        while (true)
        {
            Vector2Int cell = FindLowestEntropyCell();

            if (cell == -Vector2Int.one)
                break;

            spawnGrid[cell.x, cell.y] = spawnCount;
            spawnCount++;

            CollapseCell(cell.x, cell.y);

            PropagateConstraints(cell.x, cell.y);
        }
    }

    Vector2Int FindLowestEntropyCell()
    {
        Vector2Int cell = -Vector2Int.one;
        int minEntropy = int.MaxValue;

        for (int i = 0; i < gridSize; ++i)
        {
            for (int j = 0; j < gridSize; ++j)
            {
                if (possibilities[i, j] is null)
                    continue;

                int entropy = possibilities[i, j].Count;

                if (j + 1 < gridSize && grid[i, j + 1] is not null && RoadTile.OnRoadPath('N', grid[i, j + 1])) entropy -= 4;
                if (i + 1 < gridSize && grid[i + 1, j] is not null && RoadTile.OnRoadPath('E', grid[i + 1, j])) entropy -= 4;
                if (j - 1 >= 0 && grid[i, j - 1] is not null && RoadTile.OnRoadPath('S', grid[i, j - 1])) entropy -= 4;
                if (i - 1 >= 0 && grid[i - 1, j] is not null && RoadTile.OnRoadPath('W', grid[i - 1, j])) entropy -= 4;

                if (entropy < minEntropy)
                {
                    minEntropy = entropy;
                    cell.x = i;
                    cell.y = j;
                }
            }
        }

        return cell;
    }

    void CollapseCell(int x, int y)
    {
        RoadTile chosen = possibilities[x, y].Count != 0 ?
            ChooseRandomTile(x, y) :
            //possibilities[x, y][UnityEngine.Random.Range(0, possibilities[x, y].Count)] :
            null;

        possibilities[x, y] = null;
        grid[x, y] = chosen;
    }

    void PropagateConstraints(int x, int y)
    {
        if (x + 1 < gridSize && possibilities[x + 1, y] is not null)
            possibilities[x + 1, y].RemoveAll((rt) => !rt.CanConnect('E', grid[x, y]));

        if (x - 1 >= 0 && possibilities[x - 1, y] is not null)
            possibilities[x - 1, y].RemoveAll((rt) => !rt.CanConnect('W', grid[x, y]));

        if (y + 1 < gridSize && possibilities[x, y + 1] is not null)
            possibilities[x, y + 1].RemoveAll((rt) => !rt.CanConnect('N', grid[x, y]));

        if (y - 1 >= 0 && possibilities[x, y - 1] is not null)
            possibilities[x, y - 1].RemoveAll((rt) => !rt.CanConnect('S', grid[x, y]));
    }

    void GeneratePrefabs()
    {
        for (int i = 0; i < gridSize; ++i)
        {
            for (int j = 0; j < gridSize; ++j)
            {
                RoadTile tile = grid[i, j];
                if (tile == null)
                    continue;

                Vector3 spawnPos = new Vector3((i * cellSize - centerOffset.x) * (blockCells + 1), 0, (j * cellSize - centerOffset.y) * (blockCells + 1));

                var obj = Instantiate(tile.prefab, spawnPos, Quaternion.identity);
                obj.SetActive(true);
                obj.transform.GetChild(0).gameObject.SetActive(true);
                obj.transform.Rotate(Vector3.up, tile.rotY);
                obj.name += $"_{tile.rotY}_{spawnGrid[i, j]}";

                for (int k = 1; k <= blockCells; ++k)
                {
                    Vector3 roadPosHorizontal = spawnPos + new Vector3(k * cellSize, 0, 0);
                    Vector3 roadPosHorizontal2 = spawnPos + new Vector3(-k * cellSize, 0, 0);
                    Vector3 roadPosVertical = spawnPos + new Vector3(0, 0, k * cellSize);
                    Vector3 roadPosVertical2 = spawnPos + new Vector3(0, 0,-k * cellSize);

                    GameObject road;
                    if (i + 1 < gridSize && tile.CanConnectWithRoad('E', grid[i + 1, j]))
                    {
                        road = Instantiate(roadStraightPrefab, roadPosHorizontal, Quaternion.Euler(90, 90, 0));
                        road.SetActive(true);
                    }
                    if (j + 1 < gridSize && tile.CanConnectWithRoad('N', grid[i, j + 1]))
                    {
                        road = Instantiate(roadStraightPrefab, roadPosVertical, Quaternion.Euler(90, 0, 0));
                        road.SetActive(true);
                    }
                    if (i - 1 >= 0 && tile.CanConnectWithRoad('W', grid[i - 1, j]))
                    {
                        road = Instantiate(roadStraightPrefab, roadPosHorizontal2, Quaternion.Euler(90, 90, 0));
                        road.SetActive(true);
                    }
                    if (j - 1 >= 0 && tile.CanConnectWithRoad('N', grid[i, j - 1]))
                    {
                        road = Instantiate(roadStraightPrefab, roadPosVertical2, Quaternion.Euler(90, 0, 0));
                        road.SetActive(true);
                    }
                }
            }
        }
    }

    RoadTile ChooseRandomTile(int x, int y)
    {
        float distanceFromCenter = Vector2.Distance(new Vector2(x * cellSize, y * cellSize) - centerOffset, center);
        float factor = distanceFromCenter / (gridSize * cellSize * Mathf.Sqrt(2) / 2);

        // normalize
        var t = Mathf.InverseLerp(0, maxDistanceFromCenter, factor);
        factor = Mathf.Lerp(0, 1, t);

        SetProbabilityWeights(factor);
        float norm = ApproxNormalizingConstant(factor);
        float rand = UnityEngine.Random.value;

        // Cross road
        if (possibilities[x, y].Contains(tiles[10]) && rand < crossChance / norm)
            return tiles[10];

        // Join road
        for (int i = 6; i < 10; ++i)
            if (possibilities[x, y].Contains(tiles[i]) && rand < (joinChance + crossChance) / norm)
                return tiles[i];

        // Straight road
        for (int i = 0; i < 2; ++i)
            if (possibilities[x, y].Contains(tiles[i]) && rand < (joinChance + crossChance + straightChance) / norm)
                return tiles[i];

        // Turn road
        for (int i = 2; i < 6; ++i)
            if (possibilities[x, y].Contains(tiles[i]) && rand < (joinChance + crossChance + straightChance + turnChance) / norm)
                return tiles[i];

        // End road
        for (int i = 11; i < 15; ++i)
            if (possibilities[x, y].Contains(tiles[i]))
                return tiles[i];

        return possibilities[x, y][UnityEngine.Random.Range(0, possibilities[x, y].Count)];
    }

    void SetProbabilityWeights(float factor)
    {
        crossChance = Mathf.Sin(factor * Mathf.PI + Mathf.PI / 2 - 0 * Mathf.PI / 5) / 2 + 0.5f;
        joinChance = Mathf.Sin(factor * Mathf.PI + Mathf.PI / 2 - 1 * Mathf.PI / 5) / 2 + 0.5f;
        straightChance = Mathf.Sin(factor * Mathf.PI + Mathf.PI / 2 - 0 * Mathf.PI / 5) / 2 + 0.5f;
        turnChance = Mathf.Sin(factor * Mathf.PI + Mathf.PI / 2 - 0 * Mathf.PI / 5) / 2 + 0.5f;
        endChance = Mathf.Sin(factor * Mathf.PI + Mathf.PI / 2 - 0 * Mathf.PI / 5) / 2 + 0.5f;
    }

    float ApproxNormalizingConstant(float factor)
        {
            return -1.53884176851f * Mathf.Pow(factor - 1, 2) + 4.5388417686f;
    }

    public class RoadTile
    {
        public GameObject prefab;
        public int rotY;
        public List<char> validConnections;
        public List<char> invalidConnections;

        public RoadTile(GameObject prefab, int rotY, char[] validConnections, char[] invalidConnections)
        {
            this.prefab = prefab;
            this.rotY = rotY;
            this.validConnections = validConnections.ToList();
            this.invalidConnections = invalidConnections.ToList();
        }

        private static char GetOppositeDirection(char dir)
        {
            switch (dir)
            {
                case 'N': return 'S';
                case 'E': return 'W';
                case 'S': return 'N';
                case 'W': return 'E';
            }

            return 'N';
        }

        public bool CanConnect(char directionToOther, RoadTile other)
        {
            if (other is null)
                return false;

            char oppositeDirection = GetOppositeDirection(directionToOther);

            return (validConnections.Contains(directionToOther) && other.validConnections.Contains(oppositeDirection)) ||
                (invalidConnections.Contains(directionToOther) && other.invalidConnections.Contains(oppositeDirection));
        }

        public bool CanConnectWithRoad(char directionToOther, RoadTile other)
        {
            if (other is null)
                return false;

            char oppositeDirection = GetOppositeDirection(directionToOther);

            return (validConnections.Contains(directionToOther) && other.validConnections.Contains(oppositeDirection));
        }

        public static bool OnRoadPath(char directionToRoad, RoadTile other)
        {
            char oppositeDirection = GetOppositeDirection(directionToRoad);
            return other.validConnections.Contains(oppositeDirection);
        }
    }
}
