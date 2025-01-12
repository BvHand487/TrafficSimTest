using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Utils;

// General class that runs the simulation
public class Simulation : MonoBehaviour
{
    public GameObject carPrefab;
    public Color homeColor, workColor;

    public float minCarTravelDistance = 75.0f;
    public float carTurnRadius = 7.5f;
    public int carTurnResolution = 5;
    public int maxCars = 20;
    private int simulatedMaxCars;

    private float homeToWorkTrafficChance = 1.0f;
    private float workToHomeTrafficChance = 1.0f;

    [System.NonSerialized]
    public int currentCars = 0;

    [System.NonSerialized]
    public List<Junction> junctions;
    public Dictionary<GameObject, Junction> junctionsDict;

    [System.NonSerialized]
    public List<Road> roads;

    [System.NonSerialized]
    public List<Building> buildings;

    [System.NonSerialized]
    private List<Car> carSpawnList;

    [System.NonSerialized]
    private Clock clock;

    public void Initialize(List<Junction> js, List<Road> rs, List<Building> bs)
    {
        junctions = new List<Junction>(js);
        junctionsDict = junctions.ToDictionary(j => j.obj, j => j);

        roads = new List<Road>(rs);
        buildings = new List<Building>(bs);
        carSpawnList = new List<Car>();
    }

    private void Start()
    {
        clock = Clock.Instance;
    }

    void Update()
    {
        simulatedMaxCars = Modeling.CalculateTrafficFlowFromTime(24f * clock.GetFractionOfDay(), ref homeToWorkTrafficChance, ref workToHomeTrafficChance, maxCars);
        
        if (clock == null) return;
        clock.Update();

        if (junctions == null) return;
        foreach (var junction in junctions)
            junction.Update();

        if (currentCars + carSpawnList.Count < simulatedMaxCars)
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
        var map = junction.trafficController.trafficLightDict;
        return map[Utils.Math.GetClosestVector(car.transform.position, map.Keys.ToList())].status;
    }

    // Spawns randomly for now
    private void SpawnCar()
    {
        if (buildings == null || buildings.Count <= 1)
            return;

        float rand = UnityEngine.Random.value;
        CarPath carPath;

        // Spawn a car going from home to work
        if (rand < homeToWorkTrafficChance)
            carPath = CreateDirectedCarPath();

        // Spawn a car going from work to home
        else if (rand < workToHomeTrafficChance)
        {
            carPath = CreateDirectedCarPath();
            carPath.points.Reverse();
            (carPath.from, carPath.to) = (carPath.to, carPath.from);
        }

        // Spawn a car going randomly
        else
            carPath = CreateRandomCarPath();

        var car = InstantiateCar(carPath);

        carSpawnList.Add(car);
    }

    private bool CanActivateCar(Car car)
    {
        var cars = FindObjectsByType(typeof(Car), FindObjectsSortMode.None);
        foreach (Car c in cars)
            if (c != null && c.gameObject.activeInHierarchy && Vector3.Distance(c.transform.position, car.transform.position) < 30.0f)
                return false;

        return true;
    }

    private CarPath CreateRandomCarPath()
    {
        Building start, end;

        do
        {
            start = Utils.Random.Select(buildings);
            end = Utils.Random.Select(buildings);
        }
        while (Vector3.Distance(start.obj.transform.position, end.obj.transform.position) < minCarTravelDistance);

        return new CarPath(start, end, Pathfinding.FindCarPath(start, end, carTurnRadius, carTurnResolution), carTurnResolution);
    }

    private CarPath CreateDirectedCarPath()
    {
        var homeBuildings = buildings.FindAll(b => b.type == Building.Type.Home);
        var workBuildings = buildings.FindAll(b => b.type == Building.Type.Work);

        foreach (var hb in homeBuildings)
        {

        }

        Building start, end;

        do
        {
            start = Utils.Random.Select(homeBuildings);
            end = Utils.Random.Select(workBuildings);
        }
        while (Vector3.Distance(start.obj.transform.position, end.obj.transform.position) < minCarTravelDistance);

        return new CarPath(start, end, Pathfinding.FindCarPath(start, end, carTurnRadius, carTurnResolution), carTurnResolution);
    }

    private Car InstantiateCar(CarPath carPath)
    {
        var car = Instantiate(carPrefab, carPath.from.obj.transform.position, Quaternion.identity, this.transform).GetComponent<Car>();
        car.gameObject.SetActive(false);
        car.gameObject.name = "Car";
        car.Initialize(carPath);

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
