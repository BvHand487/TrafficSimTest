using System.Collections.Generic;
using UnityEngine;


namespace Generation
{
    public static class Optimization
    {
        // Finds the largest section of connected roads and removes all other ones. Uses DFS 
        public static void KeepLargestRoadComponent(Grid grid)
        {
            List<List<GridTile>> allComponents = new List<List<GridTile>>();
            List<GridTile> largestComponentObj = null;
            int largestComponent = int.MinValue;

            bool[,] visited = new bool[grid.size, grid.size];

            for (int i = 0; i < grid.size; i++)
            {
                for (int j = 0; j < grid.size; j++)
                {
                    if (visited[i, j] == false)
                    {
                        List<GridTile> tempComponent = new List<GridTile>();
                        FindAllTiles(grid.tiles[i, j], tempComponent, visited);

                        var component = new List<GridTile>(tempComponent);
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
                    foreach (var tile in component)
                        grid.tiles[tile.coords.x, tile.coords.y] = null;
        }

        public static void FindAllTiles(GridTile current, List<GridTile> tiles, bool[,] visited)
        {
            visited[current.coords.x, current.coords.y] = true;
            tiles.Add(current);

            var neighbours = current.GetConnectedNeighbours();
            foreach (var n in neighbours)
                if (visited[n.coords.x, n.coords.y] == false)
                    FindAllTiles(n, tiles, visited);
        }
    }
}
