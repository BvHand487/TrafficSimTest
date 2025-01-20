using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Straight roads and turns are considered a part of the road path
public class Road
{
    public Simulation simulation;

    // Both of the junctions at the road's ends
    public Junction junctionStart { get; set; }
    public Junction junctionEnd { get; set; }

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

    public bool IsConnectedTo(Junction j) => junctionStart == j || junctionEnd == j;
    public Junction GetOtherJunction(Junction j) => j == junctionStart ? junctionEnd : junctionStart;

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

    public static Junction GetCommonJunction(List<Road> roads)
    {
        var starts = roads.Select(r => r.junctionStart).ToList();
        var ends = roads.Select(r => r.junctionEnd).ToList();

        var all = starts.Union(ends);

        return all.Where(junction => roads.All(r => r.IsConnectedTo(junction)))?.First();
    }

    // Returns a segment of the path from a junction to a point (inclusive) on the road.
    public List<Vector3> SplitPath(Junction junction, Vector3 at)
    {
        int fromIndex = GetClosestPathIndex(junction.transform.localPosition);
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
}
