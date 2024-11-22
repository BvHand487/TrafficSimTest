using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Utils;

// General class that runs the simulation
public class Simulation : MonoBehaviour
{
    public GameObject carPrefab;
    public int maxCars = 10;

    [System.NonSerialized]
    public int currentCars = 0;

    [System.NonSerialized]
    public List<Junction> junctions;

    [System.NonSerialized]
    public List<Road> roads;

    private List<Junction> path;

    public void Initialize(List<Junction> js, List<Road> rs)
    {
        junctions = new List<Junction>(js);
        roads = new List<Road>(rs);
    }

    void Update()
    {
        foreach (var junction in junctions)
            junction.Update();

        if (Input.GetKeyDown("space"))
        {
            Junction a = junctions[UnityEngine.Random.Range(0, junctions.Count)];
            Junction b = junctions[UnityEngine.Random.Range(0, junctions.Count)];

            path = Pathfinding.FindBestPath(a, b);
        }

        if (currentCars < maxCars)
            SpawnCar();
    }

    public TrafficLight.Status GetTrafficLightStatus(Car car, GameObject junction)
    {
        var junc = junctions.Find(j => j.obj == junction);

        TrafficLight closestLight = junc.lights[0];
        float minDist = float.MaxValue;
        foreach (var l in junc.lights)
        {
            float dist = Vector3.Distance(l.pos, car.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closestLight = l;
            }
        }

        return closestLight.status;
    }

    // Spawns randomly for now
    private void SpawnCar()
    {
        currentCars++;

        Junction a = junctions[UnityEngine.Random.Range(0, junctions.Count)];
        Junction b = junctions[UnityEngine.Random.Range(0, junctions.Count)];

        var path_ = Pathfinding.JunctionToVectorPath(Pathfinding.FindBestPath(a, b));

        InstantiateCar(
            path_,
            path_[0],
            path_[path_.Count - 1]
        );
    }

    private void InstantiateCar(List<Vector3> path, Vector3 from, Vector3 to)
    {
        var zAlignment = Vector3.Dot(Vector3.forward, (path[1] - from).normalized);
        var xAlignment = Vector3.Dot(Vector3.right, (path[1] - from).normalized);

        Quaternion rot;
        if (zAlignment > 0 && xAlignment == 0) rot = Quaternion.Euler(0, 0, 0);
        else if (zAlignment < 0 && xAlignment == 0) rot = Quaternion.Euler(0, 180, 0);
        else if (zAlignment == 0 && xAlignment > 0) rot = Quaternion.Euler(0, 90, 0);
        else rot = Quaternion.Euler(0, 270, 0);

        var car = Instantiate(carPrefab, from, rot);

        car.GetComponent<Car>().Initialize(path, from, to);

        car.name = "Car";
        car.SetActive(true);
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
        
        if (path != null)
        {
            Gizmos.color = Color.blue;
            Vector3[] pointPath = path.Select((p) => p.obj.transform.position).ToArray();
            Gizmos.DrawLineStrip(pointPath, false);
        }
    }
}
