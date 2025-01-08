using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Utils
{
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

        public static List<Vector3> SmoothVectorPath(List<Vector3> path, float radius, int resolution)
        {
            if (path == null || path.Count < 3)
            {
                Debug.LogWarning("Path must have at least 3 points to smooth.");
                return path;
            }

            List<Vector3> smoothedPath = new List<Vector3>();

            // Add the first point (it remains unchanged)
            smoothedPath.Add(path.First());

            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector3 prev = path[i - 1];
                Vector3 current = path[i];
                Vector3 next = path[i + 1];

                // Calculate directions
                Vector3 dirToPrev = (prev - current).normalized;
                Vector3 dirToNext = (next - current).normalized;

                // Calculate the angle between the two directions
                float angle = Vector3.Angle(dirToPrev, dirToNext);

                // If the angle is approximately 90 degrees, create a curve
                if (Mathf.Abs(angle - 90f) < 1e-2f)
                {
                    // Calculate arc points
                    List<Vector3> arcPoints = GenerateArcPointsPerpendicular(prev, current, next, radius, resolution);

                    // Add the arc points to the smoothed path
                    smoothedPath.AddRange(arcPoints);
                }
                else
                {
                    // Otherwise, keep the current point
                    smoothedPath.Add(current);
                }
            }

            // Add the last point (it remains unchanged)
            smoothedPath.Add(path.Last());

            return smoothedPath;
        }

        private static List<Vector3> GenerateArcPointsPerpendicular(Vector3 prev, Vector3 current, Vector3 next, float radius, int resolution)
        {
            List<Vector3> arcPoints = new List<Vector3>();

            // Calculate perpendicular bisectors of the two segments
            Vector3 prevDir = (prev - current).normalized;
            Vector3 nextDir = (next - current).normalized;

            Vector3 bisector = (prevDir + nextDir).normalized;
            Vector3 arcCenter = current + bisector * radius / Mathf.Sin(Vector3.Angle(prevDir, bisector) * Mathf.Deg2Rad);

            Vector3 startDir = (current + radius * prevDir - arcCenter).normalized;
            Vector3 endDir = (current + radius * nextDir - arcCenter).normalized;

            // Generate points along the arc
            Vector3 cross = Vector3.Cross(startDir, endDir);
            bool clockwise = cross.y < 0;

            for (int i = 0; i <= resolution; i++)
            {
                float t = (float)i / resolution;
                float angle = t * 90f;

                if (clockwise)
                    angle = -angle;

                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 pointOnArc = arcCenter + rotation * startDir * radius;

                arcPoints.Add(pointOnArc);
            }

            return arcPoints;
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

        public static Vector3 GetMidpointVector(Vector3 a, Vector3 b) => a + (b - a) / 2;


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

            return bezierPoints;
        }

        public static bool CompareFloat(float x, float cmp, float eps = 0.005f) => IsWithinFloat(x, cmp - eps, cmp + eps);

        public static bool IsWithinFloat(float x, float lo, float hi) => x > lo && x < hi;

        public static float NormalDistribution(float x, float sigma = 1.0f, float mean = 0.0f)
        {
            float dx = x - mean;

            return Mathf.Exp(-(dx * dx) / (2 * sigma * sigma));
        }
    }
}
