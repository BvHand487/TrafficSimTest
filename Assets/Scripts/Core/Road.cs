using System;
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

    public static Junction GetCommonJunction(Road a, Road b)
    {
        var junctionsA = new List<Junction>() { a.junctionStart, a.junctionEnd };
        var junctionsB = new List<Junction>() { b.junctionStart, b.junctionEnd };

        return junctionsA.Intersect(junctionsB).First();
    }

    public static List<Road> OrderRoadsAntiClockwise(List<Road> roads)
    {
        if (roads.Count == 1)
            return roads;

        Vector3 junctionPos = Road.GetCommonJunction(roads.First(), roads.Last()).obj.transform.position;
        Dictionary<Road, float> anglesInWorld = new Dictionary<Road, float>();

        foreach (var road in roads)
        {
            var roadPos = Utils.Math.GetClosestVector(junctionPos, road.path);
            Vector3 roadDir = (roadPos - junctionPos).normalized;

            var angle = Vector3.Angle(roadDir, Vector3.right);
            if (Vector3.Dot(roadDir, Vector3.forward) < 0.1f)
                angle = 360f - angle;
            angle = Unity.Mathematics.math.fmod(angle, 360f);

            anglesInWorld.Add(road, angle);
        }

        return anglesInWorld.OrderBy(e => e.Value).Select(pair => pair.Key).ToList();
    }
}