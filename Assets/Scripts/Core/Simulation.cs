using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

// General class that runs the simulation
public class Simulation : MonoBehaviour
{
    public GameObject carPrefab;
    public int maxCars = 20;

    [System.NonSerialized]
    public int currentCars = 0;

    [System.NonSerialized]
    public List<Junction> junctions;

    [System.NonSerialized]
    public List<Road> roads;

    [System.NonSerialized]
    private List<Car> carSpawnList;

    [System.NonSerialized]
    private Clock clock;

    public void Initialize(List<Junction> js, List<Road> rs)
    {
        junctions = new List<Junction>(js);
        roads = new List<Road>(rs);
        carSpawnList = new List<Car>();
    }

    private void Start()
    {
        clock = Clock.Instance;
    }

    void Update()
    {
        if (clock == null) return;
        clock.Update();

        if (junctions == null) return;
        foreach (var junction in junctions)
            junction.Update();

        if (currentCars + carSpawnList.Count < maxCars)
            SpawnCar();


        List<Car> toRemove = new List<Car>();
        foreach (var car in carSpawnList)
            if (CanActivateCar(car))
            {
                currentCars++;
                car.gameObject.SetActive(true);
                toRemove.Add(car);
            }

        foreach (var carToRemove in toRemove)
            carSpawnList.Remove(carToRemove);
    }

    public TrafficLight.Status GetTrafficLightStatus(Car car, Junction junction)
    {
        var trafficLights = junction.trafficLights;

        TrafficLight closestLight = trafficLights.First();
        float minDist = float.MaxValue;

        foreach (var tl in trafficLights)
        {
            float dist = Vector3.Distance(tl.pos, car.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closestLight = tl;
            }
        }

        return closestLight.status;
    }

    // Spawns randomly for now
    private void SpawnCar()
    {
        var carPath = CreateRandomCarPath();

        var car = InstantiateCar(
            carPath,
            carPath.First(),
            carPath.Last()
        );

        carSpawnList.Add(car);
    }

    private bool CanActivateCar(Car car)
    {
        var cars = FindObjectsOfType<Car>();

        foreach (var c in cars)
            if (c != null && c.gameObject.activeInHierarchy && Vector3.Distance(c.transform.position, car.transform.position) < 20.0f)
                return false;

        return true;
    }

    private List<Vector3> CreateRandomCarPath()
    {
        Junction a, b;
        List<Junction> path;

        do
        {
            a = junctions[UnityEngine.Random.Range(0, junctions.Count)];
            b = junctions[UnityEngine.Random.Range(0, junctions.Count)];
            path = Pathfinding.FindBestPath(a, b);
        }
        while (path.Count <= 1);

        return Pathfinding.JunctionToVectorPath(path);
    }

    private Car InstantiateCar(List<Vector3> path, Vector3 from, Vector3 to)
    {
        var zAlignment = Vector3.Dot(Vector3.forward, (path[1] - from).normalized);
        var xAlignment = Vector3.Dot(Vector3.right, (path[1] - from).normalized);

        Quaternion rot;
        if (zAlignment > 0 && xAlignment == 0) rot = Quaternion.Euler(0, 0, 0);
        else if (zAlignment < 0 && xAlignment == 0) rot = Quaternion.Euler(0, 180, 0);
        else if (zAlignment == 0 && xAlignment > 0) rot = Quaternion.Euler(0, 90, 0);
        else rot = Quaternion.Euler(0, 270, 0);

        var car = Instantiate(carPrefab, from, rot).GetComponent<Car>();
        car.Initialize(path, from, to);
        car.gameObject.name = "Car";
        return car;
    }

    private void OnDrawGizmos()
    {
        if (junctions == null)
            return;

        foreach (var junction in junctions)
        {
            if (junction.trafficLights == null) continue;

            for (int i = 0; i < junction.trafficLights.Count; ++i)
            {
                Gizmos.color = junction.trafficLights[i].GetStatusColor();
                Gizmos.DrawSphere(junction.trafficLights[i].pos, 1.5f);
            }
        }
    }
}
