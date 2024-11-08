using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// General class that runs the simulation
public class Simulation : MonoBehaviour
{   
    // All junctions
    public List<Junction> junctions;

    // All roads
    public List<Road> roads;

    public void Initialize(List<Junction> js, List<Road> rs)
    {
        junctions = new List<Junction>(js);
        roads = new List<Road>(rs);
    }

    void Update()
    {
        foreach (var junction in junctions)
        {
            junction.Update();
        }
    }

    private void OnDrawGizmos()
    {
        foreach (var junction in junctions)
        {
            if (junction.lights == null) continue;

            foreach (var light in junction.lights)
            {
                Gizmos.color = light.GetStatusColor();
                Gizmos.DrawSphere(light.pos, 2);
            }
        }
    }
}
