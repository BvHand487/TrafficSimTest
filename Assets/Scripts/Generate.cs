using System;
using System.Collections;
using System.Collections.Generic;
using Unity.PlasticSCM.Editor.WebApi;
using UnityEngine;

public enum RoadTypes {
    None,
    RoadHorizontal,
    RoadVertical,
    RoadLeftUp,
    RoadUpRight,
    RoadRightDown,
    RoadDownLeft,
    RoadJoinRight,
    RoadJoinUp,
    RoadJoinLeft,
    RoadJoinDown,
    RoadCross,
    RoadEndLeft,
    RoadEndUp,
    RoadEndRight,
    RoadEndDown,
}

public class Generate : MonoBehaviour
{
    [SerializeField]
    private int gridSize = 50;

    // depends on prefab size
    [SerializeField]
    private float cellSize = 15;

    [SerializeField]
    private Vector2 center = new Vector2(0, 0);

    [SerializeField]
    private List<GameObject> prefabs;

    private Dictionary<RoadTypes, List<RoadTypes>> valids =
    {
        { RoadHorizontal, new List<RoadTypes> { } },
        { RoadVertical, },
        { RoadLeftUp, },
        { RoadUpRight, },
        { RoadRightDown, },
        { RoadDownLeft, },
        { RoadJoinRight, },
        { RoadJoinUp, },
        { RoadJoinLeft, },
        { RoadJoinDown, },
        { RoadCross, },
        { RoadEndLeft, },
        { RoadEndUp, },
        { RoadEndRight, },
        { RoadEndDown, },
    }

    void Start()
    {
        RoadTypes[,] grid = new RoadTypes[gridSize, gridSize];
        int x = 0, y = 0;

        for (int i = 0; i < gridSize; i++)
            for (int j = 0; j < gridSize; j++)
                grid[i, j] = RoadTypes.None;

        grid[x, y] = RoadTypes.RoadCross;

        float r = UnityEngine.Random.value;

        if (r < 0.25)
        {
            x += 1;

            List<RoadTypes> valid = new List<RoadTypes> { RoadTypes.RoadStraight, RoadTypes}
        }
        else if (r < 0.5)
        {
            x -= 1;
        }
        else if (r < 0.75)
        {
            y += 1;
        }
        else
        {
            y -= 1;
        }


        InstantiateGrid(grid);
    }

    //private void F(RoadTypes[,] grid, int x, int y)
    //{
    //    if (x >= gridSize || y >= gridSize || x < 0 || y < 0)
    //        return;

    //    if (grid[x, y] == RoadTypes.None)
    //    {
    //        float r = UnityEngine.Random.value;
    //        grid[x, y] = RoadTypes.None + (int)(r * 5 + 1);

    //        F(grid, x + 1, y);
    //        F(grid, x - 1, y);
    //        F(grid, x, y + 1);
    //        F(grid, x, y - 1);
    //    }
    //}

    public GameObject SpawnRoadPrefab(RoadTypes type, Vector2 pos)
    {
        int prefabIndex = (int) type - 1;
        var obj = Instantiate(prefabs[prefabIndex], new Vector3(pos.x, 0, pos.y), Quaternion.Euler(90, 0, 0));

        obj.SetActive(true);
        obj.name = prefabs[prefabIndex].name + "_" + System.Guid.NewGuid().ToString().Substring(0, 4);

        return obj;
    }

    private void InstantiateGrid(RoadTypes[,] grid)
    {
        for (int i = 0; i < gridSize; i++)
            for (int j = 0; j < gridSize; j++)
                SpawnRoadPrefab(
                    grid[i, j],
                    new Vector2(
                        cellSize * gridSize / 2 + center.x - i * cellSize,
                        cellSize * gridSize / 2 + center.y - j * cellSize
                        )
                    );
    }
}
