using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Utils;

// General class that runs the simulation
public class Simulation : MonoBehaviour
{
    [System.NonSerialized] public VehicleManager vehicleManager;
    [System.NonSerialized] public BuildingManager buildingManager;
    [System.NonSerialized] public float physicalSize;

    public List<Road> roads;
    public List<Junction> junctions;
    public Dictionary<GameObject, Junction> junctionsDict;

    public void Initialize(List<Junction> js, List<Road> rs, List<Building> bs, float physicalSize)
    {
        this.physicalSize = physicalSize;

        roads = new List<Road>(rs);
        junctions = new List<Junction>(js);
        junctionsDict = junctions.ToDictionary(j => j.obj, j => j);

        buildingManager = new BuildingManager(this, bs);
        vehicleManager = new VehicleManager(this, GameManager.Instance.vehicleCount);
    }

    void Update()
    {
        if (junctions == null || vehicleManager == null)
            return;

        foreach (var junction in junctions)
            junction.Update();

        vehicleManager.Update();
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
                Gizmos.DrawSphere(transform.position + junction.trafficLights[i].pos, 1.5f);

                Handles.Label(transform.position + junction.trafficLights[i].pos + Vector3.up * 1f, $"{junction.trafficLights[i].queue.Count}");
            }
        }
    }

    public Simulation Duplicate()
    {
        Simulation copy = GameManager.Instance.simulations.CreateSimulation();

        copy.junctions = new List<Junction>();
        copy.junctionsDict = new Dictionary<GameObject, Junction>();
        for (int i = 0; i < transform.childCount; ++i)
        {
            var child = transform.GetChild(i);

            if (child.CompareTag("Vehicle"))
                continue;

            if (child.CompareTag("Junction") || child.CompareTag("End"))
            {
                GameObject jobj = Instantiate(child.gameObject, copy.transform);
                
                Junction j = new Junction(copy, jobj);
                copy.junctions.Add(j);
                copy.junctionsDict.Add(jobj, j);
                continue;
            }

            if (child.CompareTag("Road") || child.CompareTag("Turn"))
            {
                GameObject road = Instantiate(child.gameObject, copy.transform);
                continue;
            }

            GameObject ground = Instantiate(child.gameObject, copy.transform);
            var scale = ground.transform.localScale;
            scale.Scale(new Vector3(physicalSize / GameManager.Instance.tileSize, 1f, physicalSize / GameManager.Instance.tileSize));
            ground.transform.localScale = scale;
        }

        copy.roads = new List<Road>();
        foreach (Road r in roads)
        {
            r.simulation = copy;
            copy.roads.Add(r);
        }

        copy.buildingManager = new BuildingManager(this, buildingManager.buildings);

        copy.vehicleManager = new VehicleManager(copy, GameManager.Instance.vehicleCount);


        // Keep the physical size consistent
        copy.physicalSize = physicalSize;

        // Clone the GameObject hierarchy (if needed) to avoid modifying the original GameObject setup
        foreach (Transform child in transform)
        {
            GameObject childCopy = Instantiate(child.gameObject, copy.transform);
        }

        return copy;
    }

    public void Destroy()
    {
        vehicleManager.Destroy();

        buildingManager.Destroy();

        foreach (var junction in junctions)
        {
            GameObject.Destroy(junction.obj);
        }

        for (int i = 0; i < transform.childCount; ++i)
        {
            GameObject.Destroy(transform.GetChild(i).gameObject);
        }
    }
}
