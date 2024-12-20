using System.Collections.Generic;
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

        public static List<Vector3> SmoothVectorPath(List<Vector3> points, float turnRadius, int resolution)
        {
            return null;
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
