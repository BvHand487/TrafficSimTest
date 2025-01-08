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

    // A lsit of points that describe the path of the road
    public List<Vector3> path { get; }
    public float length;

    public Road(List<Vector3> path, Junction j1 = null, Junction j2 = null)
    {
        this.path = path;
        this.junctionStart = j1;
        this.junctionEnd = j2;

        for (int i = 0; i < path.Count - 1; ++i)
            length += Vector3.Distance(path[i], path[i + 1]);
        length += Generation.Generate.tileSize;
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

    public List<Vector3> SplitPath(Junction from, Vector3 at)
    {
        Vector3 pointClosestToJunction = Utils.Math.GetClosestVector(from.obj.transform.position, this.path);
        int targetIndex = this.path.IndexOf(at);

        if (pointClosestToJunction == this.path.First())
            return this.path.GetRange(0, targetIndex + 1);
        else
            return this.path.GetRange(targetIndex, this.path.Count - targetIndex);
    }

    public List<Vector3> SplitPath(Vector3 from, Vector3 to)
    {
        if (from == to)
            return new List<Vector3>() { from };

        int fromIndex = this.path.IndexOf(from);
        int toIndex = this.path.IndexOf(to);
        if (fromIndex > toIndex)
            (fromIndex, toIndex) = (toIndex, fromIndex);

        return path.Skip(fromIndex).Take(toIndex - fromIndex + 1).ToList();
    }

    // Orders roads around the intersection sequentially
    // If it's a 4-way intersection it orders them anticlockwise
    public static List<Road> OrderRoads(List<Road> roads)
    {
        Vector3 junctionPos = Road.GetCommonJunction(roads.First(), roads.Last()).obj.transform.position;

        switch (roads.Count)
        {
            case 1:
                return roads;

            case 3:
                int IndexOfRoadThatSticksOut = 1;

                for (int i = 0; i < roads.Count; ++i)
                    if (roads.Count(r => Utils.Math.CompareFloat(
                            Vector3.Dot(
                                r.path.First() - junctionPos,
                                roads[i].path.First() - junctionPos
                            ), 0.0f)) == 2)
                    {
                        IndexOfRoadThatSticksOut = i;
                        break;
                    }

                var temp = roads[1];
                roads[1] = roads[IndexOfRoadThatSticksOut];
                roads[IndexOfRoadThatSticksOut] = temp;

                return roads;


            default:
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
}
