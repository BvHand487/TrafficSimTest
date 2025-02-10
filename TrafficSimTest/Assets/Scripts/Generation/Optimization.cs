using System.Collections.Generic;


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

        // DFS algorithm that finds all neighbouring road tiles so that it can create a Road object
        public static void GetFullRoad(GridTile current, List<GridTile> road, List<GridTile> junctions, List<GridTile> buildings, bool[,] visited)
        {
            if (current == null || !current.IsValidTile())
                return;

            if (current.type == GridTile.Type.Junction)
            {
                junctions.Add(current);
                return;
            }

            if (current.type == GridTile.Type.Building)
            {
                buildings.Add(current);
                return;
            }

            if (visited[current.coords.x, current.coords.y]) return;
            visited[current.coords.x, current.coords.y] = true;

            road.Add(current);

            current.ForNeighbours(neighbour =>
            {
                if (neighbour.IsValidTile() && current.CanConnectThroughRoad(current.GetDirectionToTile(neighbour), neighbour) || neighbour.type == GridTile.Type.Building)
                    GetFullRoad(neighbour, road, junctions, buildings, visited);
            });
        }
    }
}
