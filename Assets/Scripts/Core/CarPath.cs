using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public struct CarPath
{
    public Building from, to;
    public List<Vector3> points;

    public CarPath(Building from, Building to, List<Vector3> points)
    {
        this.from = from;
        this.to = to;
        this.points = points;
    }

    public bool Done()
    {
        return points.Count == 0;
    }

    public Vector3 Next()
    {
        return points.First();
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
}
