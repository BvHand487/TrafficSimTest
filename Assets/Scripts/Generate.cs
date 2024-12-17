using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Generate : MonoBehaviour
{
    [SerializeField] private int gridSize = 50;

    // Spacing of tiles on generating
    [SerializeField] private int blockCells = 5;
    private static readonly float cellSize = 15;

    [SerializeField]
    private Vector2 center = Vector2.zero;
    private Vector2 centerOffset = Vector2.zero;
    private float maxDistanceFromCenter = 1.0f;

    [SerializeField]
    public GameObject roadStraightPrefab, roadTurnPrefab, roadJoinPrefab, roadCrossPrefab, roadEndPrefab, buildingPrefab, simulation;

    private float straightChance = 0, turnChance = 0, joinChance = 0, crossChance = 0, endChance = 0;

    private List<GridTile> tiles;
    public GridTile[,] grid;
    private List<GridTile>[,] possibilities;

    private List<Road> roads = new List<Road>();
    private List<Junction> junctions = new List<Junction>();
    private List<Building> buildings = new List<Building>();

    // Load data from main menu using the PlayerRrefs API
    private void Awake()
    {
        if (PlayerPrefs.HasKey("Grid Size"))
            gridSize = PlayerPrefs.GetInt("Grid Size");

        if (PlayerPrefs.HasKey("Junction Gap"))
            blockCells = PlayerPrefs.GetInt("Junction Gap");

        PlayerPrefs.DeleteAll();
    }

    void Start()
    {
        grid = new GridTile[gridSize, gridSize];

        float cellsToCenter = (gridSize * (blockCells + 1) - blockCells) / 2.0f * cellSize;
        centerOffset = new Vector2(cellsToCenter - center.x, cellsToCenter - center.y);
        maxDistanceFromCenter = Mathf.Sqrt(2) * (cellSize * (gridSize / 2));

        tiles = new List<GridTile>()
        {
            new GridTile(GridTile.Type.Building, buildingPrefab, 0, "NESW", this),
            new GridTile(GridTile.Type.Junction, roadCrossPrefab, 0, "NESW", this),
            new GridTile(GridTile.Type.Junction, roadJoinPrefab, 0, "WNE", this),
            new GridTile(GridTile.Type.Junction, roadJoinPrefab, 90, "NES", this),
            new GridTile(GridTile.Type.Junction, roadJoinPrefab, 180, "ESW", this),
            new GridTile(GridTile.Type.Junction, roadJoinPrefab, 270, "SWN", this),
            new GridTile(GridTile.Type.Junction, roadEndPrefab, 0, "N", this),
            new GridTile(GridTile.Type.Junction, roadEndPrefab, 90, "E", this),
            new GridTile(GridTile.Type.Junction, roadEndPrefab, 180, "S", this),
            new GridTile(GridTile.Type.Junction, roadEndPrefab, 270, "W", this),
            new GridTile(GridTile.Type.Road, roadStraightPrefab, 0, "NS", this),
            new GridTile(GridTile.Type.Road, roadStraightPrefab, 90, "WE", this),
            new GridTile(GridTile.Type.Road, roadTurnPrefab, 0, "NE", this),
            new GridTile(GridTile.Type.Road, roadTurnPrefab, 90, "ES", this),
            new GridTile(GridTile.Type.Road, roadTurnPrefab, 180, "SW", this),
            new GridTile(GridTile.Type.Road, roadTurnPrefab, 270, "WN", this),
        };

        possibilities = new List<GridTile>[gridSize, gridSize];
        for (int i = 0; i < gridSize; ++i)
            for (int j = 0; j < gridSize; ++j)
                possibilities[i, j] = new List<GridTile>(tiles.FindAll(t => t.type != GridTile.Type.Building));

        // Initialize a crossroad at the center
        grid[gridSize / 2, gridSize / 2] = possibilities[gridSize / 2, gridSize / 2].Find(t => t.prefab == roadCrossPrefab);
        possibilities[gridSize / 2, gridSize / 2] = null;
        PropagateConstraints(gridSize / 2, gridSize / 2);

        // Run WFC algorithm
        GenerateGrid();

        // Remove unconnected grid tiles
        Optimize();

        // Spawn prefabs
        GeneratePrefabs();

        simulation.GetComponent<Simulation>().Initialize(junctions, roads, buildings);
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
        GridTile chosen = possibilities[x, y].Count != 0 ?
            ChooseRandomTile(x, y) :
            null;

        grid[x, y] = chosen;
        possibilities[x, y] = null;
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
    void GetFullRoad(ref GridTile[,] grid, ref List<(int, int)> road, ref List<Vector2Int> juncs, ref List<Vector2Int> builds, ref bool[,] visited, int i, int j, int gridSize)
    {
        if (i < 0 || j < 0 || i >= gridSize || j >= gridSize || grid[i, j] == null)
            return;

        if (grid[i, j].type == GridTile.Type.Junction)
        {
            juncs.Add(new Vector2Int(i, j));
            return;
        }

        if (grid[i, j].type == GridTile.Type.Building)
        {
            builds.Add(new Vector2Int(i, j));
            return;
        }

        if (visited[i, j]) return;
        visited[i, j] = true;

        road.Add((i, j));

        if (i < gridSize - 1 && grid[i + 1, j]?.CanConnectThroughRoad('W', grid[i, j]) == true)
            GetFullRoad(ref grid, ref road, ref juncs, ref builds, ref visited, i + 1, j, gridSize);

        if (j < gridSize - 1 && grid[i, j + 1]?.CanConnectThroughRoad('S', grid[i, j]) == true)
            GetFullRoad(ref grid, ref road, ref juncs, ref builds, ref visited, i, j + 1, gridSize);

        if (i > 0 && grid[i - 1, j]?.CanConnectThroughRoad('E', grid[i, j]) == true)
            GetFullRoad(ref grid, ref road, ref juncs, ref builds, ref visited, i - 1, j, gridSize);

        if (j > 0 && grid[i, j - 1]?.CanConnectThroughRoad('N', grid[i, j]) == true)
            GetFullRoad(ref grid, ref road, ref juncs, ref builds, ref visited, i, j - 1, gridSize);
    }

    // Genereates the prefabs from the grid and initializes Junction and Road classes
    void GeneratePrefabs()
    {
        Dictionary<Vector2Int, Junction> juncsMap = new Dictionary<Vector2Int, Junction>();
        Dictionary<Vector2Int, Building> buildsMap = new Dictionary<Vector2Int, Building>();

        // Expands the grid by the number of blockCells
        int grid2Size = (gridSize - 1) * blockCells + gridSize + 2;
        GridTile[,] grid2 = new GridTile[grid2Size, grid2Size];

        for (int i = 0; i < gridSize; i++)
            for (int j = 0; j < gridSize; j++)
                grid2[i * (blockCells + 1) + 1, j * (blockCells + 1) + 1] = grid[i, j];

        // Fill gap between spaced out tiles with straight roads
        List<(int, int, GridTile)> toAdd = new List<(int, int, GridTile)>();
        for (int i = 0; i < grid2Size; ++i)
            for (int j = 0; j < grid2Size; ++j)
            {
                GridTile tile = grid2[i, j];
                if (tile == null) continue;

                if (i + blockCells + 1 < grid2Size && tile.CanConnectThroughRoad('E', grid2[i + blockCells + 1, j]))
                    for (int k = 1; k <= blockCells; ++k)
                        toAdd.Add((i + k, j, tiles.Find(t => t.prefab == roadStraightPrefab && t.rotY == 90)));

                if (i - blockCells - 1 >= 0 && tile.CanConnectThroughRoad('W', grid2[i - blockCells - 1, j]))
                    for (int k = 1; k <= blockCells; ++k)
                        toAdd.Add((i - k, j, tiles.Find(t => t.prefab == roadStraightPrefab && t.rotY == 90)));

                if (j + blockCells + 1 < grid2Size && tile.CanConnectThroughRoad('N', grid2[i, j + blockCells + 1]))
                    for (int k = 1; k <= blockCells; ++k)
                        toAdd.Add((i, j + k, tiles.Find(t => t.prefab == roadStraightPrefab && t.rotY == 0)));

                if (j - blockCells - 1 >= 0 && tile.CanConnectThroughRoad('S', grid2[i, j - blockCells - 1]))
                    for (int k = 1; k <= blockCells; ++k)
                        toAdd.Add((i, j - k, tiles.Find(t => t.prefab == roadStraightPrefab && t.rotY == 0)));
            }
        foreach (var a in toAdd)
            grid2[a.Item1, a.Item2] = a.Item3;


        // Add buildings to grid and instantiate
        toAdd.Clear();
        for (int i = 0; i < grid2Size; ++i)
        {
            for (int j = 0; j < grid2Size; ++j)
            {
                GridTile tile = grid2[i, j];
                if (tile == null)
                {
                    if ((i + 1 < grid2Size && grid2[i + 1, j] != null && grid2[i + 1, j].validConnections.Count <= 2) ||
                        (i - 1 >= 0 && grid2[i - 1, j] != null && grid2[i - 1, j].validConnections.Count <= 2) ||
                        (j + 1 < grid2Size && grid2[i, j + 1] != null && grid2[i, j + 1].validConnections.Count <= 2) ||
                        (j - 1 >= 0 && grid2[i, j - 1] != null && grid2[i, j - 1].validConnections.Count <= 2))
                    {
                        toAdd.Add((i, j, tiles.Find(t => t.type == GridTile.Type.Building)));
                    }
                }
            }
        }
        foreach (var a in toAdd)
            grid2[a.Item1, a.Item2] = a.Item3;


        // Instantiate all prefabs and create Junction/Building objects
        for (int i = 0; i < grid2Size; ++i)
        {
            for (int j = 0; j < grid2Size; ++j)
            {
                GridTile tile = grid2[i, j];
                if (tile == null)
                    continue;

                Vector3 pos = new Vector3((i - 0.5f) * cellSize - centerOffset.x, 0, (j - 0.5f) * cellSize - centerOffset.y);
                var obj = Instantiate(tile.prefab, pos, Quaternion.Euler(0, tile.rotY, 0));
                obj.SetActive(true);
                obj.name = $"{tile.prefab.name}";

                if (tile.type == GridTile.Type.Junction)
                    juncsMap.Add(new Vector2Int(i, j), new Junction(obj));

                else if (tile.type == GridTile.Type.Building)
                {
                    var maxDistFromCenter = Mathf.Sqrt(2f) * 0.5f * (cellSize * grid2Size - cellSize);
                    var distFromCenter = Vector3.Distance(obj.transform.position, new Vector3(center.x, 0, center.y));
                    var heightFactor = 20 * Utils.Math.NormalDistribution(distFromCenter / maxDistanceFromCenter, 1f);
                    obj.transform.position += Vector3.up * 4f;
                    obj.transform.position += Vector3.up * heightFactor / 2; // Move the object in the direction of scaling, so that the corner on ther side stays in place
                    obj.transform.localScale += Vector3.up * heightFactor;

                    buildsMap.Add(new Vector2Int(i, j), new Building(obj, ChooseRandomBuildingType(obj.transform)));
                }
            }
        }

        // Creates all Road objects 
        bool[,] visited = new bool[grid2Size, grid2Size];
        for (int i = 0; i < grid2Size; ++i)
        {
            for (int j = 0; j < grid2Size; ++j)
            {
                GridTile tile = grid2[i, j];
                if (tile == null) continue;

                if (tile.type == GridTile.Type.Road && !visited[i, j] && grid2[i, j] != null)
                {
                    List<(int, int)> roadTiles = new List<(int, int)>();
                    List<Vector2Int> juncs = new List<Vector2Int>();
                    List<Vector2Int> builds = new List<Vector2Int>();

                    GetFullRoad(ref grid2, ref roadTiles, ref juncs, ref builds, ref visited, i, j, grid2Size);

                    var roadPath = Utils.Math.OrderVectorPath(
                        roadTiles.Select((r) => new Vector3(
                            (r.Item1 - 0.5f) * cellSize - centerOffset.x,
                            0,
                            (r.Item2 - 0.5f) * cellSize - centerOffset.y
                        )).ToList()
                    );

                    var roadToAdd = new Road(roadPath, juncsMap[juncs[0]], juncsMap[juncs[1]]);
                    roads.Add(roadToAdd);
                    
                    foreach (Building building in builds.Select(coords => buildsMap[coords]))
                    {
                        building.adjacentRoads.Add(roadToAdd);

                        if (!buildings.Contains(building))
                            buildings.Add(building);
                    }
                }
            }
        }

        // Sets the references in the Junction and Road objects
        junctions = juncsMap.Values.ToList();

        foreach (var j in junctions)
            j.Initialize(roads.FindAll((r) => r.junctionStart == j || r.junctionEnd == j), ChooseRandomJunctionType(j.obj.transform));
    }

    Building.Type ChooseRandomBuildingType(Transform b)
    {
        var maxDistFromCenter = Mathf.Sqrt(2f) * 0.5f * (cellSize * ((gridSize - 1) * blockCells + gridSize + 2) - cellSize);
        var distFromCenter = Vector3.Distance(b.position, new Vector3(center.x, 0, center.y));

        if (Utils.Math.NormalDistribution(distFromCenter / maxDistFromCenter, 0.32f) > UnityEngine.Random.value)
            return Building.Type.Work;
        else
            return Building.Type.Home;
    }

    Junction.Type ChooseRandomJunctionType(Transform j)
    {
        var maxDistFromCenter = Mathf.Sqrt(2f) * 0.5f * (cellSize * ((gridSize - 1) * blockCells + gridSize) - cellSize);
        var distFromCenter = Vector3.Distance(j.position, new Vector3(center.x, 0, center.y));

        Debug.Log(distFromCenter / maxDistFromCenter);

        if (Utils.Math.NormalDistribution(distFromCenter / maxDistFromCenter, 0.65f) > UnityEngine.Random.value)
            return Junction.Type.Lights;
        else
            return Junction.Type.Stops;
    }

    // Chooses a random tile depending on the distance from the center of the grid.
    GridTile ChooseRandomTile(int x, int y)
    {
        List<char> invalidEdgeConnections = new List<char>();
        if (x == 0)
            invalidEdgeConnections.Add('W');
        if (y == 0)
            invalidEdgeConnections.Add('S');
        if (x == gridSize - 1)
            invalidEdgeConnections.Add('E');
        if (y == gridSize - 1)
            invalidEdgeConnections.Add('N');

        // Select a tile which doesnt exit the grid when on the edge
        if (invalidEdgeConnections.Count > 0)
        {
            var possibleEdgeTiles = possibilities[x, y].FindAll(p => invalidEdgeConnections.All(c => !p.validConnections.Contains(c)));
            return possibleEdgeTiles.Count != 0 ? possibleEdgeTiles[UnityEngine.Random.Range(0, possibleEdgeTiles.Count - 1)] : null;
        }

        float distanceFromCenter = Vector2.Distance(new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize), Vector2.one * gridSize / 2.0f * cellSize);
        float factor = distanceFromCenter / maxDistanceFromCenter;

        // normalize
        var t = Mathf.InverseLerp(0, maxDistanceFromCenter, factor);
        factor = Mathf.Lerp(0, 1, t);

        SetProbabilityWeights(factor);
        float rand = UnityEngine.Random.value;

        List<GridTile> wantedTiles;

        // Cross road
        wantedTiles = possibilities[x, y].FindAll(p => p.prefab == roadCrossPrefab);
        if (wantedTiles.Count > 0 && rand < crossChance)
            return wantedTiles[UnityEngine.Random.Range(0, wantedTiles.Count - 1)];

        // Join road
        wantedTiles = possibilities[x, y].FindAll(p => p.prefab == roadJoinPrefab);
        if (wantedTiles.Count > 0 && rand < (joinChance + crossChance))
            return wantedTiles[UnityEngine.Random.Range(0, wantedTiles.Count - 1)];

        // Straight road
        wantedTiles = possibilities[x, y].FindAll(p => p.prefab == roadStraightPrefab);
        if (wantedTiles.Count > 0 && rand < (joinChance + crossChance + straightChance))
            return wantedTiles[UnityEngine.Random.Range(0, wantedTiles.Count - 1)];

        // Turn road
        wantedTiles = possibilities[x, y].FindAll(p => p.prefab == roadTurnPrefab);
        if (wantedTiles.Count > 0 && rand < (joinChance + crossChance + straightChance + turnChance))
            return wantedTiles[UnityEngine.Random.Range(0, wantedTiles.Count - 1)];

        // End road
        wantedTiles = possibilities[x, y].FindAll(p => p.prefab == roadEndPrefab);
        if (wantedTiles.Count > 0)
            return wantedTiles[UnityEngine.Random.Range(0, wantedTiles.Count - 1)];

        // Select randomly if we get here
        return possibilities[x, y][UnityEngine.Random.Range(0, possibilities[x, y].Count)];
    }

    // Set chances for tiles
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

    // Gets indeces of the neighbours of a cell in the grid
    public (int[], int[]) GetGridNeighbours(int i, int j)
    {
        List<int> Is = new List<int>();
        List<int> Js = new List<int>();

        if (i + 1 < gridSize && grid[i, j] != null && grid[i, j].CanConnectThroughRoad('E', grid[i + 1, j]))
        {
            Is.Add(i + 1);
            Js.Add(j);
        }
        if (j + 1 < gridSize && grid[i, j] != null && grid[i, j].CanConnectThroughRoad('N', grid[i, j + 1]))
        {
            Is.Add(i);
            Js.Add(j + 1);
        }
        if (i - 1 >= 0 && grid[i, j] != null && grid[i, j].CanConnectThroughRoad('W', grid[i - 1, j]))
        {
            Is.Add(i - 1);
            Js.Add(j);
        }
        if (j - 1 >= 0 && grid[i, j] != null && grid[i, j].CanConnectThroughRoad('S', grid[i, j - 1]))
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
    public class GridTile
    {
        public enum Type
        {
            Building,
            Junction,
            Road,
        }

        public Generate parentScript;

        // The tile's prefab
        public GameObject prefab;
        
        // The rotation of the tile
        public int rotY;

        // Where the tile can connect with a road to
        public List<char> validConnections;

        // Where the tile cannot connect with a road to
        public List<char> invalidConnections;

        public Type type { get; private set; }

        public GridTile(Type type, GameObject prefab, int rotY, string validConnections, Generate parentScript)
        {
            this.type = type;
            this.prefab = prefab;
            this.rotY = rotY;
            this.validConnections = validConnections.ToCharArray().ToList();

            this.invalidConnections = new List<char>();
            foreach (var differentDir in "NESW".ToHashSet().Except(validConnections.ToHashSet()))
                this.invalidConnections.Add(differentDir);
            
            this.parentScript = parentScript;
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
        public bool CanConnect(char directionToOther, GridTile other)
        {
            if (other is null)
                return false;

            char oppositeDirection = GetOppositeDirection(directionToOther);

            return (validConnections.Contains(directionToOther) && other.validConnections.Contains(oppositeDirection)) ||
                (invalidConnections.Contains(directionToOther) && other.invalidConnections.Contains(oppositeDirection));
        }

        // Returns if the tile can connect to another one only by road
        public bool CanConnectThroughRoad(char directionToOther, GridTile other)
        {
            if (other is null)
                return false;

            char oppositeDirection = GetOppositeDirection(directionToOther);

            return validConnections.Contains(directionToOther) && other.validConnections.Contains(oppositeDirection);
        }
    }
}
