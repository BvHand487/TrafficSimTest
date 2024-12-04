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
                    float tentativeGScore = gScore[current] + road.path.Count;

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

        public static List<Vector3> JunctionToVectorPath(List<Junction> junctionPath)
        {
            List<Vector3> vectorPath = new List<Vector3>();

            for (int i = 0; i < junctionPath.Count - 1; ++i)
            {
                var juncPos1 = junctionPath[i].obj.transform.position;
                var juncPos2 = junctionPath[i + 1].obj.transform.position;
                var road = Junction.GetCommonRoad(junctionPath[i], junctionPath[i + 1]);

                vectorPath.Add(juncPos1);

                if (Vector3.Distance(juncPos1, road.path[0]) > Vector3.Distance(juncPos2, road.path[0]))
                    road.path.Reverse();

                vectorPath.Add(Math.GetMidpointVector(road.path.First(), juncPos1));

                for (int k = 0; k < road.path.Count; ++k)
                {
                    if (road.IsTurn(road.path[k]))
                    {
                        vectorPath.Add(Math.GetMidpointVector(road.path[k - 1], road.path[k]));
                        vectorPath.Add(road.path[k]);
                        vectorPath.Add(Math.GetMidpointVector(road.path[k], road.path[k + 1]));
                    }
                    else
                        vectorPath.Add(road.path[k]);
                }

                vectorPath.Add(Math.GetMidpointVector(road.path.Last(), juncPos2));

            }

            vectorPath.Add(junctionPath[junctionPath.Count - 1].obj.transform.position);

            return vectorPath;
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

    public static class Math
    {
        // Orders a list of points so that each pair of points are nearest-neighbours
        public static List<Vector3> OrderVectorPath(List<Vector3> points)
        {
            if (points == null || points.Count == 0)
                return null;

            if (points.Count <= 2)
                return points;

            List<Vector3> orderedPoints = new List<Vector3>() { points[points.Count - 1] };
            points.Remove(points[points.Count - 1]);

            while (points.Count > 0)
            {
                var closest = GetClosestVector(orderedPoints.Last(), points);
                orderedPoints.Add(closest);
                points.Remove(closest);
            }   

            return orderedPoints;
        }

        public static Vector3 GetClosestVector(Vector3 target, List<Vector3> points)
        {
            float minDist = float.MaxValue;
            Vector3 closest = Vector3.zero;

            foreach (var point in points)
            {
                if (point == target)
                    continue;

                var dist = Vector3.Distance(target, point);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = point;
                }
            }

            return closest;
        }

        public static Vector3 GetMidpointVector(Vector3 a, Vector3 b)
        {
            return a + (b - a) / 2;
        }
    }
}
