using UnityEngine;


[System.Serializable]
public class RoadData
{
    public float[][] path;
    public string[] prefabPaths;
    public float[] rotYs;

    public RoadData(Road road)
    {
        this.path = new float[road.path.Count][];
        this.rotYs = new float[road.path.Count];
        this.prefabPaths = new string[road.path.Count];

        for (int i = 0; i < road.path.Count; i++)
        {
            this.path[i] = new float[3] { road.path[i].x, road.path[i].y, road.path[i].z };

            // todo
            this.rotYs[i] = 0f;

            if (road.IsTurn(road.path[i]))
            {
                this.prefabPaths[i] = $"Prefabs/{GameManager.Instance.roadTurnPrefab.name}";
            }
            else
            {
                this.prefabPaths[i] = $"Prefabs/{GameManager.Instance.roadStraightPrefab.name}";
            }
        }
    }
}