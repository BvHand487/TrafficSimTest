using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Straight roads and turns are considered a part of the road path
public class Road
{
    // Both of the junctions at the road's ends
    public Junction j1 { get; set; }
    public Junction j2 { get; set; }

    // A lsit of points that describe the path of the road
    public Vector3[] path { get; }

    public Road(Vector3[] path, Junction j1 = null, Junction j2 = null)
    {
        this.path = path;
        this.j1 = j1;
        this.j2 = j2;
    }
}
