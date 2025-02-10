using System.Collections.Generic;
using UnityEngine;

public class VehiclePath
{
    public Building from, to;

    public List<Road> roads;
    public List<Junction> junctions;
    
    public List<Vector3> points;

    public float turnRadius;
    public int turnResolution;

    private bool enteredJunction = false;
    public int currentPointIndex = 0;
    private int currentRoadIndex = 0;
    private int currentJunctionIndex = 0;

    public VehiclePath(Building from, Building to, float turnRadius=7.5f, int turnResolution=5)
    {
        this.from = from;
        this.to = to;
        this.turnRadius = turnRadius;
        this.turnResolution = turnResolution;

        (roads, junctions, points) = Utils.Pathfinding.FindCarPath(from, to, turnRadius, turnResolution);
    }

    public bool Done()
    {
        return currentPointIndex == points.Count;
    }

    public Vector3 First() => points[currentPointIndex];
    public Vector3 Last() => points[points.Count - 1];
    public Vector3 Next(int idx=0) => points[currentPointIndex + idx];

    public int Length()
    {
        return points.Count - currentPointIndex;
    }

    // Advances the car path, returns true if the paths is done
    public bool Advance()
    {
        if (currentPointIndex < points.Count)
        {
            Vector3 point = points[currentPointIndex];
            currentPointIndex++;

            if (junctions != null && currentJunctionIndex < junctions.Count)
            {
                // path has entered a junction
                if (enteredJunction == false && junctions[currentJunctionIndex].IsPointInside(point))
                {
                    enteredJunction = true;
                    return true;
                }

                // path has exited a junction
                if (enteredJunction == true && !junctions[currentJunctionIndex].IsPointInside(point))
                {
                    enteredJunction = false;
                    currentJunctionIndex++;
                    currentRoadIndex++;
                    return true;
                }
            }

            return true;
        }

        return false;
    }

    public Road CurrentRoad() => roads[currentRoadIndex];
    public Junction UpcomingJunction()
    {
        if (junctions != null && currentJunctionIndex < junctions.Count)
            return junctions[currentJunctionIndex];

        return null;
    }
}
