using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Junction : MonoBehaviour
{
    public Road[] roads;
    public TrafficLight[] lights;

    public void Initialize(Road[] roads)
    {
        this.roads = roads;
        
        lights = new TrafficLight[roads.Length];
        for (int i = 0; i < roads.Length; i++)
            lights[i] = new TrafficLight(this, roads[i]);
    }

    // Update is called once per frame
    void Update()
    {
        if (lights == null)
            return;

        foreach (var light in lights)
            light.Update();
    }

    private void OnDrawGizmos()
    {
        if (lights == null)
            return;

        foreach (var light in lights)
        {
            Gizmos.color = light.GetStatusColor();
            Gizmos.DrawSphere(transform.position, 3);
        }
    }
}
