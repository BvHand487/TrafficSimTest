using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

// General class that runs the simulation
public class Simulation : MonoBehaviour
{
    public GameObject carPrefab;
    public int maxCars = 10;

    public int currentCars = 0;

    // All junctions
    public List<Junction> junctions;

    // All roads
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
            Junction a = junctions[Random.Range(0, junctions.Count)];
            Junction b = junctions[Random.Range(0, junctions.Count)];

            path = Utils.Pathfinding.FindBestPath(a, b);
        }

        if (currentCars < maxCars)
            SpawnCar();
    }

    public TrafficLight.Status GetTrafficLightStatus(Car car, GameObject junction)
    {
        var junc = junctions.Find(j => j.obj == junction); ;

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

        Junction a = junctions[Random.Range(0, junctions.Count)];
        Junction b = junctions[Random.Range(0, junctions.Count)];

        var path_ = Utils.Pathfinding.FindBestPath(a, b);

        Road startRoad = Junction.GetCommonRoad(path_[0], path_[1]);
        Road endRoad = Junction.GetCommonRoad(path_[path_.Count - 2], path_[path_.Count - 1]);

        InstantiateCar(startRoad.path[Random.Range(0, startRoad.path.Length)],
            Quaternion.Euler(0, 0, 0),
            path_,
            Random.Range(0, startRoad.path.Length),
            Random.Range(0, endRoad.path.Length)
        );
    }

    private void InstantiateCar(Vector3 pos, Quaternion rot, List<Junction> path, int fromRoadTile, int toRoadTile)
    {
        var car = Instantiate(carPrefab, pos, rot);

        car.GetComponent<Car>().Initialize(path, fromRoadTile, toRoadTile);

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
