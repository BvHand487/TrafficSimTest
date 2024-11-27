using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;


public class Generate : MonoBehaviour
{
    [SerializeField]
    private int gridSize = 50;

    [SerializeField]
    private float cellSize = 15;

    // Spacing of tiles on generating
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

    void Start()
    {
        grid = new RoadTile[gridSize, gridSize];

        float cellsToCenter = (gridSize * (blockCells + 1) - blockCells) / 2.0f * cellSize;

        centerOffset = new Vector2(cellsToCenter - center.x, cellsToCenter - center.y);
        maxDistanceFromCenter = Mathf.Sqrt(2) * (cellSize * (gridSize / 2));


        // All possible road tiles for the algorithm
        tiles = new List<RoadTile>()
        {
            new RoadTile(roadStraightPrefab, 0, new[] { 'N', 'S' }, new[] { 'E', 'W' }),
            new RoadTile(roadStraightPrefab, 90, new[] { 'W', 'E' }, new[] { 'N', 'S' }),
            new RoadTile(roadTurnPrefab, 0, new[] { 'N', 'E' }, new[] { 'W', 'S' }),
            new RoadTile(roadTurnPrefab, 90, new[] { 'E', 'S' }, new[] { 'W', 'N' }),
            new RoadTile(roadTurnPrefab, 180, new[] { 'S', 'W' }, new[] { 'N', 'E' }),
            new RoadTile(roadTurnPrefab, 270, new[] { 'W', 'N' }, new[] { 'E', 'S' }),
            new RoadTile(roadJoinPrefab, 0, new[] { 'W', 'N', 'E' }, new[] { 'S' }),
            new RoadTile(roadJoinPrefab, 90, new[] { 'N', 'E', 'S' }, new[] { 'W' }),
            new RoadTile(roadJoinPrefab, 180, new[] { 'E', 'S', 'W' }, new[] { 'N' }),
            new RoadTile(roadJoinPrefab, 270, new[] { 'S', 'W', 'N' }, new[] { 'E' }),
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

        // Initialize a crossroad at the center
        possibilities[gridSize / 2, gridSize / 2] = null;
        grid[gridSize / 2, gridSize / 2] = tiles.Find(t => t.prefab == roadCrossPrefab);
        PropagateConstraints(gridSize / 2, gridSize / 2);

        // Run WFC algorithm
        GenerateGrid();

        // Remove unconnected road tiles
        Optimize();

        // Spawn prefabs
        GeneratePrefabs();

        simulation.GetComponent<Simulation>().Initialize(junctions, roads);
        simulation.SetActive(true);

        Destroy(this.gameObject);
    }

    // WFC algorithm
    void GenerateGrid()
    {
        while (true)
        {
            Vector2Int cell = FindLowestEntropyCell();

            if (cell == -Vector2Int.one)
                break;

            CollapseCell(cell.x, cell.y);

            PropagateConstraints(cell.x, cell.y);
        }
    }

    // Finds the cell with the lowest number of possibilities
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

    // Randomly selects one of the possible tiles
    void CollapseCell(int x, int y)
    {
        RoadTile chosen = possibilities[x, y].Count != 0 ?
            ChooseRandomTile(x, y) :
            null;

        possibilities[x, y] = null;
        grid[x, y] = chosen;
    }

    // Change all neighbours' possibilities when collapsing a cell
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

    // DFS algorithm that finds all neighbouring road tiles so that it can create a Road object
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

    // Genereates the prefabs from the grid and initializes Junction and Road classes
    void GeneratePrefabs()
    {
        Dictionary<Vector2Int, Junction> juncsMap = new Dictionary<Vector2Int, Junction>();

        // Expands the grid by the number of blockCells
        RoadTile[,] grid2 = new RoadTile[gridSize * (blockCells + 1) - 1, gridSize * (blockCells + 1) - 1];
        int grid2Size = gridSize * (blockCells + 1) - 1;

        for (int i = 0; i < gridSize; i++)
            for (int j = 0; j < gridSize; j++)
                grid2[i * (blockCells + 1), j * (blockCells + 1)] = grid[i, j];


        // Fill gap between spaced out tiles with straight roads
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


        // Instantiate all prefabs and create Junction objects
        for (int i = 0; i < grid2Size; ++i)
        {
            for (int j = 0; j < grid2Size; ++j)
            {
                RoadTile tile = grid2[i, j];
                if (tile == null) continue;

                Vector3 pos = new Vector3((i + 0.5f) * cellSize - centerOffset.x, 0, (j + 0.5f) * cellSize - centerOffset.y);

                var obj = Instantiate(tile.prefab, pos, Quaternion.Euler(0, tile.rotY, 0));
                obj.SetActive(true);
                obj.name = $"{tile.prefab.name}_({i}, {j})";

                if (!tile.IsRoad())
                    juncsMap.Add(new Vector2Int(i, j), new Junction(obj));
            }
        }

        // Creates all Road objects 
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

                    var roadPath = Utils.Math.OrderVectorPath(
                        roadTiles.Select((r) => new Vector3(
                            (r.Item1 + 0.5f) * cellSize - centerOffset.x,
                            0,
                            (r.Item2 + 0.5f) * cellSize - centerOffset.y
                        )).ToList()
                    );

                    roads.Add(
                        new Road(
                            roadPath,
                            juncsMap[juncs[0]],
                            juncsMap[juncs[1]]
                        )
                    );
                }
            }
        }

        // Sets the references in the Junction and Road objects
        junctions = juncsMap.Values.ToList();

        foreach (var j in junctions)
            j.SetRoads(roads.FindAll((r) => r.junctionStart == j || r.junctionEnd == j));
    }

    // Chooses a random tile depending on the distance from the center of the grid.
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

        float distanceFromCenter = Vector2.Distance(new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize), Vector2.one * gridSize / 2.0f * cellSize);
        float factor = distanceFromCenter / maxDistanceFromCenter;

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

        // Select randomly if we get here
        return possibilities[x, y][UnityEngine.Random.Range(0, possibilities[x, y].Count)];
    }

    // The chances for a type of tile is based on sine functions
    void SetProbabilityWeights(float factor)
    {
        crossChance = Mathf.Sin(factor * Mathf.PI + Mathf.PI / 2 - 0 * Mathf.PI / 5) / 2 + 0.5f;
        joinChance = Mathf.Sin(factor * Mathf.PI + Mathf.PI / 2 - 1 * Mathf.PI / 5) / 2 + 0.5f;
        straightChance = Mathf.Sin(factor * Mathf.PI + Mathf.PI / 2 - 0 * Mathf.PI / 5) / 2 + 0.5f;
        turnChance = Mathf.Sin(factor * Mathf.PI + Mathf.PI / 2 - 0 * Mathf.PI / 5) / 2 + 0.5f;
        endChance = Mathf.Sin(factor * Mathf.PI + Mathf.PI / 2 - 0 * Mathf.PI / 5) / 2 + 0.5f;
    }

    // Since the probabilities of all tiles don't add up to 1, it's necessary to normalize them with this function
    float ApproxNormalizingConstant(float factor)
    {
        return -1.53884176851f * Mathf.Pow(factor - 1, 2) + 4.5388417686f;
    }

    // Gets indeces of the neighbours of a cell in the grid
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

    // Finds the largest section of connected roads and removes all other ones. Uses DFS 
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


    // A class for representing the tiles of a given prefab
    public class RoadTile
    {
        // The tile's prefab
        public GameObject prefab;
        
        // The rotation of the tile
        public int rotY;

        // Where the tile can connect with a road to
        public List<char> validConnections;

        // Where the tile cannot connect with a road to
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


        // Returns if the tile can connect to another one either by road or by pavement
        public bool CanConnect(char directionToOther, RoadTile other)
        {
            if (other is null)
                return false;

            char oppositeDirection = GetOppositeDirection(directionToOther);

            return (validConnections.Contains(directionToOther) && other.validConnections.Contains(oppositeDirection)) ||
                (invalidConnections.Contains(directionToOther) && other.invalidConnections.Contains(oppositeDirection));
        }

        // Returns if the tile can connect to another one only by road
        public bool CanConnectThroughRoad(char directionToOther, RoadTile other)
        {
            if (other is null)
                return false;

            char oppositeDirection = GetOppositeDirection(directionToOther);

            return validConnections.Contains(directionToOther) && other.validConnections.Contains(oppositeDirection);
        }
    
        // Returns if the tile represents a road - if it has 2 'valid' connections
        public bool IsRoad()
        {
            return validConnections.Count == 2 && invalidConnections.Count == 2;
        }
    }
}
