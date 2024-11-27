using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Straight roads and turns are considered a part of the road path
public class Road
{
    // Both of the junctions at the road's ends
    public Junction junctionStart { get; set; }
    public Junction junctionEnd { get; set; }

    // A lsit of points that describe the path of the road
    public List<Vector3> path { get; }

    public Road(List<Vector3> path, Junction j1 = null, Junction j2 = null)
    {
        this.path = path;
        this.junctionStart = j1;
        this.junctionEnd = j2;
    }

    public Junction GetOtherJunction(Junction j)
    {
        return j == junctionStart ? junctionEnd : junctionStart;
    }

    public bool IsTurn(Vector3 point, float eps=0.01f)
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
}
