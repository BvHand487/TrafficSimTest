using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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

        public static List<Vector3> DiscretizeRoad(Road road)
        {
            List<Vector3> vectorPath = new List<Vector3>();

            for (int k = 0; k < road.path.Count; ++k)
            {
                if (road.IsTurn(road.path[k]))
                {
                    vectorPath.AddRange(
                        Math.GetBezier(
                            new List<Vector3>() {
                                    road.path[k - 1],
                                    road.path[k],
                                    road.path[k + 1]
                            },
                            segments: 3
                        )
                    );
                }
                else
                    vectorPath.Add(road.path[k]);
            }

            return vectorPath;
        }

        public static List<Vector3> JunctionToVectorPath(List<Junction> junctionPath)
        {
            List<Vector3> vectorPath = new List<Vector3>();
            Road prevRoad, road;
            prevRoad = road = Junction.GetCommonRoad(junctionPath[0], junctionPath[1]);

            for (int i = 1; i < junctionPath.Count; ++i)
            {
                var juncPos1 = junctionPath[i - 1].obj.transform.position;
                var juncPos2 = junctionPath[i].obj.transform.position;
                road = Junction.GetCommonRoad(junctionPath[i - 1], junctionPath[i]);

                if (Vector3.Distance(juncPos1, road.path[0]) > Vector3.Distance(juncPos2, road.path[0]))
                    road.path.Reverse();

                vectorPath.AddRange(
                    Math.GetBezier(
                        new List<Vector3>() {
                            Math.GetMidpointVector(prevRoad.path.Last(), juncPos1),
                            juncPos1,
                            Math.GetMidpointVector(juncPos1, road.path.First())
                        },
                        segments: 3
                    )
                );

                vectorPath.AddRange(DiscretizeRoad(road));
                prevRoad = road;
            }

            vectorPath.Add(junctionPath.Last().obj.transform.position);
            return vectorPath;
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


        // De Casteljau's algorithm for calculating a point on a bezier based on a parameter t.
        private static Vector3 EvaluateBezier(List<Vector3> controlPoints, float t)
        {
            List<Vector3> points = new List<Vector3>(controlPoints);

            while (points.Count > 1)
            {
                for (int i = 0; i < points.Count - 1; ++i)
                    points[i] = (1 - t) * points[i] + t * points[i + 1];

                points.RemoveAt(points.Count - 1);
            }

            return points.First();
        }

        public static List<Vector3> GetBezier(List<Vector3> controlPoints, int segments = 5)
        {
            float tStep = 1.0f / segments;
            List<Vector3> bezierPoints = new List<Vector3>();

            for (float t = 0.0f; t < 1.0f; t += tStep)
                bezierPoints.Add(EvaluateBezier(controlPoints, t));

            var msg = "";
            foreach (var p in controlPoints)
            {
                msg += $"{p.ToShortString()}, ";
            }

            foreach (var p in bezierPoints)
            {
                msg += $" -> {p.ToShortString()}";
            }
            Debug.Log(msg);

            return bezierPoints;
        }

        public static List<Vector3> GetBSpline(List<Vector3> controlPoints, int resolution = 5)
        {
            return null;
        }
    }
}
