using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;


namespace Generation
{
    public class GridTile
    {
        public enum Type
        {
            None,
            Road,
            Junction,
            Building,
        }

        // instead of 0f, it has to be an offset for the center of the city
        public Vector3 physicalPos => new Vector3((coords.x + 0.5f) * Generate.tileSize + grid.centerOffset.x, 0, (coords.y + 0.5f) * Generate.tileSize + grid.centerOffset.y);
        public static GridTile empty => new GridTile(Type.None, null, 0, null, null, -Vector2Int.one);

        public Type type;
        public GameObject prefab;
        public int rotY;
        public List<char> validConnections;

        private List<char> invalidConnections;
        public Grid grid;
        public Vector2Int coords;

        public GridTile(Type type, GameObject prefab, int rotY, string validConnections, Grid grid, Vector2Int coords)
        {
            this.type = type;
            this.prefab = prefab;
            this.rotY = rotY;
            this.validConnections = validConnections?.ToCharArray().ToList();

            if (validConnections == null)
                invalidConnections = null;
            else
            {
                this.invalidConnections = new List<char>();
                foreach (var differentDir in "NESW".ToHashSet().Except(validConnections.ToHashSet()))
                    this.invalidConnections.Add(differentDir);
            }

            this.grid = grid;
            this.coords = coords;
        }

        public bool SetTile(GridTile tile)
        {
            if (tile == null || this == tile)
                return false;

            this.type = tile.type;
            this.prefab = tile.prefab;
            this.rotY = tile.rotY;
            this.validConnections = tile.validConnections;
            this.invalidConnections = tile.invalidConnections;

            return true;
        }

        public void SetGrid(Grid grid, Vector2Int coords)
        {
            this.grid = grid;
            this.coords = coords;
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

        public char GetDirectionToTile(GridTile other)
        {
            if (other == null || this == other)
                return 'N';

            if (coords.x == other.coords.x)
                if (coords.y > other.coords.y)
                    return 'S';
                else
                    return 'N';

            if (coords.y == other.coords.y)
                if (coords.x > other.coords.x)
                    return 'W';
                else
                    return 'E';

            return 'N';
        }


        // Returns if the tile can connect to another one either by road or by pavement
        public bool CanConnect(char direction, GridTile other)
        {
            if (other is null)
                return false;

            char oppositeDirection = GetOppositeDirection(direction);
            return (validConnections.Contains(direction) && other.validConnections.Contains(oppositeDirection)) ||
                (invalidConnections.Contains(direction) && other.invalidConnections.Contains(oppositeDirection));
        }

        // Returns if the tile can connect to another one only by road
        public bool CanConnectThroughRoad(char direction, GridTile other)
        {
            if (other is null || !other.IsValidTile() || !IsValidTile())
                return false;

            char oppositeDirection = GetOppositeDirection(direction);
            return validConnections.Contains(direction) && other.validConnections.Contains(oppositeDirection);
        }

        public bool IsValidTile()
        {
            return this.type != Type.None && this.prefab != null && this.validConnections != null;
        }

        public float DistanceToCenter() => Vector3.Distance(physicalPos, new Vector3(grid.center.x, 0f, grid.center.y));

        public List<GridTile> GetConnectedNeighbours()
        {
            List<GridTile> neighbours = GetNeighbours();
            return neighbours.FindAll(n => n.IsValidTile() && CanConnectThroughRoad(GetDirectionToTile(n), n));
        }

        public List<GridTile> GetNeighbours()
        {
            List<GridTile> neighbours = new List<GridTile>();

            if(coords.y + 1 < grid.size)
                neighbours.Add(grid.tiles[coords.x, coords.y + 1]);
            if (coords.y - 1 >= 0)
                neighbours.Add(grid.tiles[coords.x, coords.y - 1]);
            if (coords.x + 1 < grid.size)
                neighbours.Add(grid.tiles[coords.x + 1, coords.y]);
            if (coords.x - 1 >= 0)
                neighbours.Add(grid.tiles[coords.x - 1, coords.y]);

            if (neighbours.Distinct().Count() != neighbours.Count)
                Debug.Log("duplicates in neighbours");

            return neighbours;
        }

        public void ForNeighbours(Action<GridTile> lambda)
        {
            List<GridTile> neighbours = GetNeighbours();

            foreach (var n in neighbours)
                lambda(n);
        }

        public static bool IsNeighbours(GridTile tile1, GridTile tile2)
        {
            if (tile1 is not null && tile2 is not null &&
                tile1.grid == tile2.grid && tile1 != tile2 &&
                (tile1.coords.x + 1 == tile2.coords.x || tile1.coords.x - 1 == tile2.coords.x ||
                tile1.coords.y + 1 == tile2.coords.x || tile1.coords.y - 1 == tile2.coords.y))
                return true;

            return false;
        }
    }
}