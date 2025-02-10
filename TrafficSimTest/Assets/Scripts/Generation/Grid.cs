using UnityEngine;


namespace Generation
{
    public class Grid
    {
        public int size;
        public GridTile[,] tiles;
        public Vector2 centerOffset;

        public Grid(int size)
        {
            this.size = size;

            tiles = new GridTile[size, size];
            for (int i = 0; i < size; ++i)
            {
                for (int j = 0; j < size; ++j)
                {
                    tiles[i, j] = GridTile.empty;
                    tiles[i, j].SetGrid(this, new Vector2Int(i, j));
                }
            }

            this.centerOffset = - new Vector2((size / 2.0f) * GameManager.Instance.tileSize, (size / 2.0f) * GameManager.Instance.tileSize);
        }

        public static float DistanceOfTiles(GridTile tile1, GridTile tile2)
        {
            return Vector3.Distance(
                tile1.physicalPos,
                tile2.physicalPos
            );
        }
        public float DistanceOfTileToCenter(int x, int y) => tiles[x, y].DistanceToCenter();
        public float MaxDistanceFromCenter()
        {
            return Mathf.Sqrt(2f) * 0.5f * (size * GameManager.Instance.tileSize - GameManager.Instance.tileSize);
        }

        public GridTile GetTile(Vector2Int coords) => tiles[coords.x, coords.y];
        public GridTile GetTile(int x, int y) => tiles[x, y];

        public bool SetTile(Vector2Int coords, GridTile tile)
        {
            tiles[coords.x, coords.y].SetGrid(this, coords);
            return tiles[coords.x, coords.y].SetTile(tile);
        }
    }
}