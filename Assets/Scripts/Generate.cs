using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using static Generate;
using static UnityEditor.PlayerSettings;
using static UnityEditor.UIElements.ToolbarMenu;
using static UnityEngine.Rendering.DebugUI.Table;

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
    public GameObject roadStraightPrefab, roadTurnPrefab, roadJoinPrefab, roadCrossPrefab, roadEndPrefab, simulation;

    private float straightChance = 0, turnChance = 0, joinChance = 0, crossChance = 0, endChance = 0;
    

    private RoadTile[,] grid;
    private List<RoadTile> tiles;
    private List<RoadTile>[,] possibilities;

    private List<Road> roads = new List<Road>();
    private List<Junction> junctions = new List<Junction>();

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

        simulation.GetComponent<Simulation>().Initialize(junctions, roads);
        simulation.SetActive(true);

        Destroy(this.gameObject);
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

    void GetFullRoad(ref RoadTile[,] grid, ref List<(int, int)> road, ref List<Vector2Int> juncs, ref bool[,] visited, int i, int j, int gridSize)
    {
        if (i < 0 || j < 0 || i >= gridSize || j >= gridSize || grid[i, j] == null)
            return;

        if (!grid[i, j].IsRoad())
        {
            juncs.Add(new Vector2Int(i, j));
            return;
        }

        if (visited[i, j]) return;
        visited[i, j] = true;

        if (grid[i, j].IsRoad())
            road.Add((i, j));

        if (i < gridSize - 1 && grid[i + 1, j]?.CanConnectThroughRoad('W', grid[i, j]) == true)
            GetFullRoad(ref grid, ref road, ref juncs, ref visited, i + 1, j, gridSize);

        if (j < gridSize - 1 && grid[i, j + 1]?.CanConnectThroughRoad('S', grid[i, j]) == true)
            GetFullRoad(ref grid, ref road, ref juncs, ref visited, i, j + 1, gridSize);

        if (i > 0 && grid[i - 1, j]?.CanConnectThroughRoad('E', grid[i, j]) == true)
            GetFullRoad(ref grid, ref road, ref juncs, ref visited, i - 1, j, gridSize);

        if (j > 0 && grid[i, j - 1]?.CanConnectThroughRoad('N', grid[i, j]) == true)
            GetFullRoad(ref grid, ref road, ref juncs, ref visited, i, j - 1, gridSize);
    }

    void GeneratePrefabs()
    {
        Dictionary<Vector2Int, Junction> juncsMap = new Dictionary<Vector2Int, Junction>();

        RoadTile[,] grid2 = new RoadTile[gridSize * (blockCells + 1), gridSize * (blockCells + 1)];
        int grid2Size = gridSize * (blockCells + 1);

        for (int i = 0; i < gridSize; i++)
            for (int j = 0; j < gridSize; j++)
                grid2[i * (blockCells + 1), j * (blockCells + 1)] = grid[i, j];

        List<(int, int, RoadTile)> toAdd = new List<(int, int, RoadTile)>();
        for (int i = 0; i < grid2Size; ++i)
            for (int j = 0; j < grid2Size; ++j)
            {
                RoadTile tile = grid2[i, j];
                if (tile == null) continue;

                if (i + blockCells + 1 < grid2Size && tile.CanConnectThroughRoad('E', grid2[i + blockCells + 1, j]))
                    for (int k = 1; k <= blockCells; ++k)
                        toAdd.Add((i + k, j, tiles[1]));

                if (i - blockCells - 1 >= 0 && tile.CanConnectThroughRoad('W', grid2[i - blockCells - 1, j]))
                    for (int k = 1; k <= blockCells; ++k)
                        toAdd.Add((i - k, j, tiles[1]));

                if (j + blockCells + 1 < grid2Size && tile.CanConnectThroughRoad('N', grid2[i, j + blockCells + 1]))
                    for (int k = 1; k <= blockCells; ++k)
                        toAdd.Add((i, j + k, tiles[0]));

                if (j - blockCells - 1 >= 0 && tile.CanConnectThroughRoad('S', grid2[i, j - blockCells - 1]))
                    for (int k = 1; k <= blockCells; ++k)
                        toAdd.Add((i, j - k, tiles[0]));
            }

        foreach (var a in toAdd)
            grid2[a.Item1, a.Item2] = a.Item3;

        for (int i = 0; i < grid2Size; ++i)
        {
            for (int j = 0; j < grid2Size; ++j)
            {
                RoadTile tile = grid2[i, j];
                if (tile == null) continue;

                Vector3 pos = new Vector3((i * cellSize - centerOffset.x) - blockCells * cellSize, 0, (j * cellSize - centerOffset.y) - blockCells * cellSize);

                var obj = Instantiate(tile.prefab, pos, Quaternion.Euler(0, tile.rotY, 0));
                obj.SetActive(true);
                obj.name = $"{tile.prefab.name}_({i}, {j})";

                if (!tile.IsRoad())
                    juncsMap.Add(new Vector2Int(i, j), new Junction(obj));
            }
        }

        bool[,] visited = new bool[grid2Size, grid2Size];
        for (int i = 0; i < grid2Size; ++i)
        {
            for (int j = 0; j < grid2Size; ++j)
            {
                RoadTile tile = grid2[i, j];
                if (tile == null) continue;

                if (tile.IsRoad() && !visited[i, j] && grid2[i, j] != null)
                {
                    List<(int, int)> roadTiles = new List<(int, int)>();
                    List<Vector2Int> juncs = new List<Vector2Int>();

                    GetFullRoad(ref grid2, ref roadTiles, ref juncs, ref visited, i, j, grid2Size);

                    roads.Add(new Road(roadTiles.Select((r) => new Vector3(
                            (r.Item1 * cellSize - centerOffset.x) - blockCells * cellSize,
                            0,
                            (r.Item2 * cellSize - centerOffset.x) - blockCells * cellSize
                        )).ToArray(),
                        juncsMap[juncs[0]],
                        juncsMap[juncs[1]])
                    );
                }
            }
        }

        Dictionary<Junction, List<Road>> junctionToRoadConnections = new Dictionary<Junction, List<Road>>();
        foreach (var j in juncsMap.Values)
            junctionToRoadConnections.Add(j, new List<Road>());

        foreach (var r in roads)
        {
            junctionToRoadConnections[r.j1].Add(r);
            junctionToRoadConnections[r.j2].Add(r);
        }

        foreach ((Junction junc, List<Road> roadsList) in junctionToRoadConnections)
        {
            junctions.Add(junc);
            junc.SetRoads(roadsList);
        }

        foreach (var j in junctionToRoadConnections.Keys)
        {
            Debug.Log($"Junction: {j.obj.transform.position}, {j.roads.Length}");
        }
        
        foreach (var r in roads)
        {
            Debug.Log($"Road: {r.j1.obj.transform.position} -> {string.Join(',', r.path)} -> {r.j2.obj.transform.position}");
        }

        //for (int i = 0; i < gridSize; ++i)
        //{
        //    for (int j = 0; j < gridSize; ++j)
        //    {
        //        RoadTile tile = grid[i, j];
        //        if (tile == null) continue;

        //        Vector3 pos = new Vector3((i * cellSize - centerOffset.x) * (blockCells + 1), 0, (j * cellSize - centerOffset.y) * (blockCells + 1));

        //        var obj = Instantiate(tile.prefab, pos, Quaternion.Euler(0, tile.rotY, 0));
        //        obj.SetActive(true);
        //        obj.name = $"{tile.prefab.name}_({i}, {j})";

        //        if (!tile.IsRoad())
        //            junctions.Add(new Vector2Int(i, j), new Junction(obj));
        //    }
        //}

        //for (int i = 0; i < gridSize; ++i)
        //{
        //    for (int j = 0; j < gridSize; ++j)
        //    {
        //        RoadTile tile = grid[i, j];
        //        if (tile == null) continue;

        //        for (int k = 1; k <= blockCells; ++k)
        //        {
        //            if (i + 1 < gridSize && tile.CanConnectThroughRoad('E', grid[i + 1, j]))
        //            {
        //                var obj = Instantiate(
        //                    tiles[0].prefab,
        //                    new Vector3((i * cellSize - centerOffset.x) * (blockCells + 1) + k * cellSize, 0, (j * cellSize - centerOffset.y) * (blockCells + 1)),
        //                    Quaternion.Euler(0, 90, 0)
        //                );
        //                obj.SetActive(true);
        //                obj.name = $"{tile.prefab.name}_({i}, {j})";
        //            }
        //            if (i - 1 >= 0 && tile.CanConnectThroughRoad('W', grid[i - 1, j]))
        //            {
        //                var obj = Instantiate(
        //                    tiles[0].prefab,
        //                    new Vector3((i * cellSize - centerOffset.x) * (blockCells + 1) - k * cellSize, 0, (j * cellSize - centerOffset.y) * (blockCells + 1)),
        //                    Quaternion.Euler(0, 90, 0)
        //                );
        //                obj.SetActive(true);
        //                obj.name = $"{tile.prefab.name}_({i}, {j})";
        //            }

        //            if (j + 1 < gridSize && tile.CanConnectThroughRoad('N', grid[i, j + 1]))
        //            {
        //                var obj = Instantiate(
        //                    tiles[1].prefab,
        //                    new Vector3((i * cellSize - centerOffset.x) * (blockCells + 1), 0, (j * cellSize - centerOffset.y) * (blockCells + 1) + k * cellSize),
        //                    Quaternion.Euler(0, 0, 0)
        //                );
        //                obj.SetActive(true);
        //                obj.name = $"{tile.prefab.name}_({i}, {j})";
        //            }
        //            if (j - 1 >= 0 && tile.CanConnectThroughRoad('S', grid[i, j - 1]))
        //            {
        //                var obj = Instantiate(
        //                    tiles[1].prefab,
        //                    new Vector3((i * cellSize - centerOffset.x) * (blockCells + 1), 0, (j * cellSize - centerOffset.y) * (blockCells + 1) - k * cellSize),
        //                    Quaternion.Euler(0, 0, 0)
        //                );
        //                obj.SetActive(true);
        //                obj.name = $"{tile.prefab.name}_({i}, {j})";
        //            }
        //        }
        //    }
        //}

        //bool[,] visited = new bool[gridSize, gridSize];
        //for (int i = 0; i < gridSize; ++i)
        //{
        //    for (int j = 0; j < gridSize; ++j)
        //    {
        //        RoadTile tile = grid[i, j];
        //        if (tile == null) continue;

        //        if (tile.IsRoad() && !visited[i, j] && grid[i, j] != null)
        //        {
        //            List<(int, int)> roadTiles = new List<(int, int)>();
        //            List<Vector2Int> juncs = new List<Vector2Int>();

        //            GetFullRoad(ref roadTiles, ref juncs, ref visited, i, j);

        //            roads.Add(new Road(roadTiles.Select((r) => new Vector3(
        //                    (r.Item1 * cellSize - centerOffset.x) * (blockCells + 1),
        //                    0,
        //                    (r.Item2 * cellSize - centerOffset.y) * (blockCells + 1)
        //                )).ToArray(),
        //                junctions[juncs[0]],
        //                junctions[juncs[1]])
        //            );
        //        }
        //    }
        //}

        //Dictionary<Junction, List<Road>> junctionToRoadConnections = new Dictionary<Junction, List<Road>>();
        //foreach (var j in junctions.Values)
        //    junctionToRoadConnections.Add(j, new List<Road>());

        //foreach (var r in roads)
        //{
        //    junctionToRoadConnections[r.j1].Add(r);
        //    junctionToRoadConnections[r.j2].Add(r);
        //}

        //foreach ((Junction junc, List<Road> roadsList) in junctionToRoadConnections)
        //    junc.SetRoads(roadsList);

        //foreach (var j in junctionToRoadConnections.Keys)
        //{
        //    Debug.Log($"Junction: {j.obj.transform.position}, {j.roads.Length}");
        //}
    }

    RoadTile ChooseRandomTile(int x, int y)
    {
        // End road when on the edge of the grid
        if (x == 0)
            return tiles[12];
        else if (y == 0)
            return tiles[11];
        else if (x == gridSize - 1)
            return tiles[14];
        else if (y == gridSize - 1)
            return tiles[13];

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
                    List<(int, int)> tempComponent = new List<(int, int)>();
                    DFS(ref tempComponent, ref visited, i, j);

                    var component = new List<(int, int)>(tempComponent);
                    allComponents.Add(component);

                    if (largestComponent < component.Count)
                    {
                        largestComponent = component.Count;
                        largestComponentObj = component;
                    }

                    tempComponent.Clear();
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
    
        public bool IsRoad()
        {
            return validConnections.Count == 2 && invalidConnections.Count == 2;
        }
    }
}
