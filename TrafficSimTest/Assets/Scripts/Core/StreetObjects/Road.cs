using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Straight roads and turns are considered a part of the road path
public class Road
{
    // Both of the junctions at the road's ends
    public Junction junctionStart { get; set; }
    public Junction junctionEnd { get; set; }

    public Simulation simulation;

    // A lsit of points that describe the path of the road
    public List<Vector3> path;
    public float length;

    public Road(Simulation simulation, List<Vector3> localPath, Junction j1 = null, Junction j2 = null)
    {
        path = new List<Vector3>();
        foreach (var localPoint in localPath)
            path.Add(simulation.transform.position + localPoint);

        this.junctionStart = j1;
        this.junctionEnd = j2;

        for (int i = 0; i < path.Count - 1; ++i)
            length += Vector3.Distance(path[i], path[i + 1]);
        length += GameManager.Instance.tileSize;
    }

    public Junction GetOtherJunction(Junction j)
    {
        return j == junctionStart ? junctionEnd : junctionStart;
    }

    public bool IsTurn(Vector3 point, float eps = 0.01f)
    {
        if (!path.Contains(point) || path.First() == point || path.Last() == point)
            return false;

        var index = path.IndexOf(point);
        var before = path[index - 1];
        var after = path[index + 1];

        var dir1 = (before - point).normalized;
        var dir2 = (after - point).normalized;

        return Mathf.Abs(Vector3.Dot(dir1, dir2)) < eps;
    }

    public static Junction GetCommonJunction(Road a, Road b)
    {
        var junctionsA = new List<Junction>() { a.junctionStart, a.junctionEnd };
        var junctionsB = new List<Junction>() { b.junctionStart, b.junctionEnd };

        return junctionsA.Intersect(junctionsB)?.First();
    }

    // Returns a segment of the path from a junction to a point (inclusive) on the road.
    public List<Vector3> SplitPath(Junction junction, Vector3 at)
    {
        int fromIndex = GetClosestPathIndex(junction.obj.transform.localPosition);
        int toIndex = GetClosestPathIndex(at);

        if (IsCyclic() && toIndex > path.Count / 2)
            fromIndex = path.Count - 1 - fromIndex;

        if (fromIndex > toIndex)
            (fromIndex, toIndex) = (toIndex, fromIndex);

        return path.Skip(fromIndex).Take(toIndex - fromIndex + 1).ToList();
    }

    // Returns a segment of the path from a start point (inclusive) to an end point (inclusive) on the road.
    public List<Vector3> SplitPath(Vector3 from, Vector3 to)
    {
        if (from == to)
            return new List<Vector3>() { from };

        int fromIndex = GetClosestPathIndex(from);
        int toIndex = GetClosestPathIndex(to);

        if (fromIndex > toIndex)
            (fromIndex, toIndex) = (toIndex, fromIndex);

        return path.Skip(fromIndex).Take(toIndex - fromIndex + 1).ToList();
    }

    public int GetClosestPathIndex(Vector3 point)
    {
        int idx = path.IndexOf(point);
        if (idx == -1)
            if (Vector3.Distance(point, path.First()) <= Vector3.Distance(point, path.Last()))
                return 0;
            else
                return path.Count - 1;

        return idx;
    }
    public Vector3 GetClosestPathPoint(Vector3 point)
    {
        return path[GetClosestPathIndex(point)];
    }

    public bool IsCyclic() => junctionStart == junctionEnd;
    
    public bool IsOnRoadPath(Vector3 point)
    {
        return path.Contains(point) ||
            point == 2 * path[path.Count - 1] - path[path.Count - 2] ||
            point == 2 * path[0] - path[1];
    }

    // Orders roads around the intersection sequentially
    // If it's a 4-way intersection it orders them anticlockwise
    public static List<Road> OrderRoads(List<Road> roads)
    {
        Vector3 junctionPos = Road.GetCommonJunction(roads.First(), roads.Last()).obj.transform.localPosition;
        Dictionary<float, Road> anglesInWorld = new Dictionary<float, Road>();

        switch (roads.Count)
        {
            case 1:
                return roads;

            case 3:
                Vector3 dirToIndex0 = roads[0].GetClosestPathPoint(junctionPos) - junctionPos;
                Vector3 dirToIndex1 = roads[1].GetClosestPathPoint(junctionPos) - junctionPos;
                Vector3 dirToIndex2 = roads[2].GetClosestPathPoint(junctionPos) - junctionPos;

                if (Vector3.Dot(dirToIndex0, dirToIndex1) <= -0.95)
                {
                    return new List<Road>() { roads[0], roads[2], roads[1] };
                }

                if (Vector3.Dot(dirToIndex0, dirToIndex2) <= -0.95)
                {
                    return new List<Road>() { roads[0], roads[1], roads[2] };
                }

                if (Vector3.Dot(dirToIndex1, dirToIndex2) <= -0.95)
                {
                    return new List<Road>() { roads[1], roads[0], roads[2] };
                }

                Debug.Log("null in order roads");
                return null;

            default:
                for (int i = 0; i < roads.Count; ++i)
                {
                    var roadPos = roads[i].IsCyclic() ?
                        roads[i].path.First() :
                        Utils.Math.GetClosestVector(junctionPos, roads[i].path);

                    Vector3 roadDir = (roadPos - junctionPos).normalized;

                    var angle = Vector3.Angle(roadDir, Vector3.right);
                    if (Vector3.Dot(roadDir, Vector3.forward) < 0.1f)
                        angle = 360f - angle;
                    angle = Unity.Mathematics.math.fmod(angle, 360f);

                    anglesInWorld.Add(angle, roads[i]);
                }

                return anglesInWorld.OrderBy(e => e.Key).Select(pair => pair.Value).ToList();
        }
    }
}
