using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils
{
    public static class Pathfinding
    {
        // A* algorithm for best path
        public static List<Junction> FindBestPath(Junction start, Junction end)
        {
            var openSet = new SortedSet<(float fScore, Junction junction)>(Comparer<(float fScore, Junction junction)>.Create((a, b) =>
            a.fScore == b.fScore ? a.junction.GetHashCode().CompareTo(b.junction.GetHashCode()) : a.fScore.CompareTo(b.fScore)));

            var cameFrom = new Dictionary<Junction, Junction>();
            var gScore = new Dictionary<Junction, float>();
            var fScore = new Dictionary<Junction, float>();

            gScore[start] = 0;
            fScore[start] = Heuristic(start, end);

            openSet.Add((fScore[start], start));

            while (openSet.Count > 0)
            {
                var current = openSet.Min.junction;
                if (current == end)
                    return ReconstructPath(cameFrom, current);

                openSet.Remove(openSet.Min);

                foreach (var road in current.roads)
                {
                    var neighbor = road.GetOtherJunction(current);
                    float tentativeGScore = gScore[current] + road.path.Length;

                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + Heuristic(neighbor, end);

                        if (!openSet.Any(item => item.junction == neighbor))
                        {
                            openSet.Add((fScore[neighbor], neighbor));
                        }
                    }
                }
            }

            return null; // No path found
        }

        // Heuristic function: Straight-line distance between two junctions
        private static float Heuristic(Junction a, Junction b)
        {
            Vector3 posA = a.obj.transform.position;
            Vector3 posB = b.obj.transform.position;

            return Vector3.Distance(posA, posB);
        }

        // Reconstructs the path from end to start by tracing the cameFrom dictionary
        private static List<Junction> ReconstructPath(Dictionary<Junction, Junction> cameFrom, Junction current)
        {
            var path = new List<Junction>();
            while (current != null)
            {
                path.Add(current);
                cameFrom.TryGetValue(current, out current);
            }
            path.Reverse();
            return path;
        }
    }

    public static class Time
    {
        // For speeding up time
        public static void SetTimeScale(int timeScale)
        {
            UnityEngine.Time.timeScale = timeScale;
        }
    }
}
