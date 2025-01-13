using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class VehiclePath
{
    public Building from, to;
    public List<Vector3> points;
    public float turnRadius;
    public int turnResolution;

    public VehiclePath(Building from, Building to, float turnRadius=7.5f, int turnResolution=5)
    {
        this.from = from;
        this.to = to;
        this.turnRadius = turnRadius;
        this.turnResolution = turnResolution;

        this.points = Utils.Pathfinding.FindCarPath(from, to, turnRadius, turnResolution);
    }

    public bool Done()
    {
        return points.Count == 0;
    }

    public Vector3 First() => points.First();
    public Vector3 Last() => points.Last();
    public Vector3 Next(int idx=0) => points[idx];

    // At the beginning of the path the car disrupts traffic
    public bool IsDistruptingRoad()
    {
        int i = 0;
        for (i = 2; i < points.Count - 1; ++i)
        {
            if (Utils.Math.AreCollinear(points[i - 2], points[i - 1], points[i]))
                if (i == turnResolution + 3)
                    return true;
        }

        return false;
    }

    public int Length()
    {
        return points.Count;
    }

    // Advances the car path, returns true if the paths is done
    public bool Advance()
    {
        if (points.Count > 0)
        {
            points.RemoveAt(0);
            return false;
        }

        return true;
    }

    public void Reverse()
    {
        points.Reverse();
        (from, to) = (to, from);
    }
}
