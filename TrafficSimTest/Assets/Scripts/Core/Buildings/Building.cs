using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Building : MonoBehaviour
{
    public enum Type
    {
        None,
        Home,
        Work
    }
    public Type type;
    public Dictionary<Road, Vector3> spawnPoints = new Dictionary<Road, Vector3>();
    public Junction closestJunction;

    private Simulation simulation;
    private BuildingManager buildingManager;

    private MeshRenderer meshRenderer;

    public void Awake()
    {
        simulation = GetComponentInParent<Simulation>();
        buildingManager = GetComponentInParent<BuildingManager>();

        meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    public void OnEnable()
    {
        float halfSize = 0.5f * (simulation.transform.GetChild(0).localScale.x - GameManager.Instance.tileSize);
        float maxDistance = Mathf.Sqrt(2f) * halfSize;

        type = Utils.Modeling.ChooseRandomBuildingType(transform.position.magnitude / maxDistance);
        SetType(type);

        this.closestJunction = Building.GetClosestJunction(this);
    }

    public void SetType(Type type)
    {
        this.type = type;

        switch (type)
        {
            case Type.Home:
                meshRenderer.material.color = Color.green;
                break;

            case Type.Work:
                meshRenderer.material.color = Color.cyan;
                break;

            default:
                meshRenderer.material.color = Color.black;
                break;
        }
    }

    public static Junction GetClosestJunction(Building b)
    {
        List<Road> adjacentRoads = b.spawnPoints.Keys.ToList();

        if (adjacentRoads.Count == 2)
            return Road.GetCommonJunction(adjacentRoads.First(), adjacentRoads.Last());
        else
        {
            Road road = adjacentRoads.First();

            // If one of the junctions is an end road return the other one
            if (road.junctionStart.roads.Count == 1)
                return road.junctionEnd;

            if (road.junctionEnd.roads.Count == 1)
                return road.junctionStart;

            Vector3 spawnPos = b.spawnPoints[road];

            // If junctionStart is closer to the building than junctionEnd
            if (Vector3.Distance(spawnPos, road.junctionStart.transform.position) <=
                Vector3.Distance(spawnPos, road.junctionEnd.transform.position))
                return road.junctionStart;
            else
                return road.junctionEnd;
        }
    }
}