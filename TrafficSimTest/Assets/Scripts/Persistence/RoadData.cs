using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;

namespace Persistence
{
    [System.Serializable]
    public class RoadData
    {
        public float[] path;
        public string[] prefabPaths;
        public float[] rotYs;

        public RoadData(Road road)
        {
            float[][] tempPath2DArray = new float[road.path.Count][];
            this.rotYs = new float[road.path.Count];
            this.prefabPaths = new string[road.path.Count];

            var roadObjects = new List<GameObject>();
            roadObjects.AddRange(GameObject.FindGameObjectsWithTag("Straight"));
            roadObjects.AddRange(GameObject.FindGameObjectsWithTag("Turn"));

            for (int i = 0; i < road.path.Count; i++)
            {
                tempPath2DArray[i] = new float[3] { road.path[i].x, road.path[i].y, road.path[i].z };

                // find closest road object and save its Y rotation
                GameObject closestRoadObject = roadObjects.First();
                float minDistance = float.MaxValue;
                foreach (var obj in roadObjects)
                {
                    float dist = Vector3.Distance(obj.transform.position, road.path[i]);

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestRoadObject = obj;
                    }
                }
                this.rotYs[i] = closestRoadObject.transform.eulerAngles.y;


                if (road.IsTurn(road.path[i]))
                {
                    this.prefabPaths[i] = $"Prefabs/{GameManager.Instance.roadTurnPrefab.name}";
                }
                else
                {
                    this.prefabPaths[i] = $"Prefabs/{GameManager.Instance.roadStraightPrefab.name}";
                }
            }

            this.path = Utils.Arrays.Flatten2DArray<float>(tempPath2DArray);
        }
    }
}