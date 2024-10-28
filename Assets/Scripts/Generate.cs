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
            new RoadTile(roadTurnPrefab, 0, new[] { 'N', 'E' }, new[] { 'W', 'S' }), // leave
            new RoadTile(roadTurnPrefab, 90, new[] { 'E', 'S' }, new[] { 'W', 'N' }),  // ?
            new RoadTile(roadTurnPrefab, 180, new[] { 'S', 'W' }, new[] { 'N', 'E' }),  // rotate
            new RoadTile(roadTurnPrefab, 270, new[] { 'W', 'N' }, new[] { 'E', 'S' }),  // ?
            new RoadTile(roadJoinPrefab, 0, new[] { 'W', 'N', 'E' }, new[] { 'S' }),  // ?
            new RoadTile(roadJoinPrefab, 90, new[] { 'N', 'E', 'S' }, new[] { 'W' }), // ?
            new RoadTile(roadJoinPrefab, 180, new[] { 'E', 'S', 'W' }, new[] { 'N' }),  // ?
            new RoadTile(roadJoinPrefab, 270, new[] { 'S', 'W', 'N' }, new[] { 'E' }), // ?
            new RoadTile(roadCrossPrefab, 0, new[] { 'N', 'S', 'E', 'W' }, new char[] {}),
            new RoadTile(roadEndPrefab, 0, new[] { 'N' }, new[] { 'S', 'E', 'W' }),
            new RoadTile(roadEndPrefab, 90, new[] { 'E' }, new[] { 'S', 'N', 'W' }),
            new RoadTile(roadEndPrefab, 180, new[] { 'S' }, new[] { 'N', 'E', 'W' }),
            new RoadTile(roadEndPrefab, 270, new[] { 'W' }, new[] { 'S', 'E', 'N' }),
        };

        possibilities = new List<RoadTile>[gridSize, gridSize];
        for (int i = 0; i < gridSize; ++i)
            for (int j = 0; j < gridSize; ++j)
                possibilities[i, j] = new List<RoadTile>(tiles);

        // Initialize center tile
        possibilities[gridSize / 2, gridSize / 2] = null;
        grid[gridSize / 2, gridSize / 2] = tiles.Find(t => t.prefab == roadCrossPrefab);
        spawnGrid[gridSize / 2, gridSize / 2] = spawnCount;
        spawnCount++;
        PropagateConstraints(gridSize / 2, gridSize / 2);

        // WFC
        GenerateGrid();

        // Optimize
        Optimize();

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
            null;

        possibilities[x, y] = null;
        grid[x, y] = chosen;
    }

    void PropagateConstraints(int x, int y)
    {
        if (x + 1 < gridSize && possibilities[x + 1, y] is not null)
            possibilities[x + 1, y].RemoveAll((rt) => !rt.CanConnect('W', grid[x, y]));

        if (x - 1 >= 0 && possibilities[x - 1, y] is not null)
            possibilities[x - 1, y].RemoveAll((rt) => !rt.CanConnect('E', grid[x, y]));

        if (y + 1 < gridSize && possibilities[x, y + 1] is not null)
            possibilities[x, y + 1].RemoveAll((rt) => !rt.CanConnect('S', grid[x, y]));

        if (y - 1 >= 0 && possibilities[x, y - 1] is not null)
            possibilities[x, y - 1].RemoveAll((rt) => !rt.CanConnect('N', grid[x, y]));
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

                var obj = Instantiate(tile.prefab, spawnPos, Quaternion.Euler(0, tile.rotY, 0));
                obj.SetActive(true);
                obj.name += $"_{tile.rotY}_{new string(tile.validConnections.ToArray())}";

                for (int k = 1; k <= blockCells; ++k)
                {
                    Vector3 roadPosHorizontal = spawnPos + new Vector3(k * cellSize, 0, 0);
                    Vector3 roadPosHorizontal2 = spawnPos + new Vector3(-k * cellSize, 0, 0);
                    Vector3 roadPosVertical = spawnPos + new Vector3(0, 0, k * cellSize);
                    Vector3 roadPosVertical2 = spawnPos + new Vector3(0, 0, -k * cellSize);

                    Quaternion horRot = Quaternion.Euler(0, 90, 0);
                    Quaternion vertRot = Quaternion.Euler(0, 0, 0);

                    GameObject road;
                    if (i + 1 < gridSize && tile.CanConnectThroughRoad('E', grid[i + 1, j]))
                    {
                        road = Instantiate(roadStraightPrefab, roadPosHorizontal, horRot);
                        road.SetActive(true);
                    }
                    if (j + 1 < gridSize && tile.CanConnectThroughRoad('N', grid[i, j + 1])) // NS
                    {
                        road = Instantiate(roadStraightPrefab, roadPosVertical, vertRot);
                        road.SetActive(true);
                    }
                    if (i - 1 >= 0 && tile.CanConnectThroughRoad('W', grid[i - 1, j]))
                    {
                        road = Instantiate(roadStraightPrefab, roadPosHorizontal2, horRot);
                        road.SetActive(true);
                    }
                    if (j - 1 >= 0 && tile.CanConnectThroughRoad('S', grid[i, j - 1])) // NS
                    {
                        road = Instantiate(roadStraightPrefab, roadPosVertical2, vertRot);
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

    public (int[], int[]) GetGridNeighbours(int i, int j)
    {
        List<int> Is = new List<int>();
        List<int> Js = new List<int>();

        if (i + 1 < gridSize && grid[i, j].CanConnectThroughRoad('E', grid[i + 1, j]))
        {
            Is.Add(i + 1);
            Js.Add(j);
        }
        if (j + 1 < gridSize && grid[i, j].CanConnectThroughRoad('N', grid[i, j + 1]))
        {
            Is.Add(i);
            Js.Add(j + 1);
        }
        if (i - 1 >= 0 && grid[i, j].CanConnectThroughRoad('W', grid[i - 1, j]))
        {
            Is.Add(i - 1);
            Js.Add(j);
        }
        if (j - 1 >= 0 && grid[i, j].CanConnectThroughRoad('S', grid[i, j - 1]))
        {
            Is.Add(i);
            Js.Add(j - 1);
        }

        return (Is.ToArray(), Js.ToArray());
    }

    void Optimize()
    {
        bool[,] visited = new bool[gridSize, gridSize];
        List<List<(int, int)>> allComponents = new List<List<(int, int)>>();
        int largestComponent = int.MinValue;
        List<(int, int)> largestComponentObj = null;

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (!visited[i, j])
                {
                    List<(int, int)> component = new List<(int, int)>();

                    DFS(ref component, ref visited, i, j);
                    allComponents.Add(component);

                    if (largestComponent < component.Count)
                    {
                        largestComponent = component.Count;
                        largestComponentObj = component;
                    }

                    component.Clear();
                }
            }
        }

        foreach (var component in allComponents)
            if (component != largestComponentObj)
                for (int i = 0; i < component.Count; ++i)
                    grid[component[i].Item1, component[i].Item2] = null;
    }

    public void DFS(ref List<(int, int)> indeces, ref bool[,] visited, int i, int j)
    {
        visited[i, j] = true;
        indeces.Add((i, j));

        var (neighboursI, neighboursJ) = GetGridNeighbours(i, j);

        for (int k = 0; k < neighboursI.Length; k++)
        {
            if (!visited[neighboursI[k], neighboursJ[k]])
                    DFS(ref indeces, ref visited, neighboursI[k], neighboursJ[k]);
        }
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

        public bool CanConnectThroughRoad(char directionToOther, RoadTile other)
        {
            if (other is null)
                return false;

            char oppositeDirection = GetOppositeDirection(directionToOther);

            return validConnections.Contains(directionToOther) && other.validConnections.Contains(oppositeDirection);
        }
    }
}
