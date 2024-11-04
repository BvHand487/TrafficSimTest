using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Road
{
    public Junction j1 { get; set; }
    public Junction j2 { get; set; }
    public Vector3[] path { get; }

    public Road(Vector3[] path, Junction j1 = null, Junction j2 = null)
    {
        this.path = path;
        this.j1 = j1;
        this.j2 = j2;
    }
}
