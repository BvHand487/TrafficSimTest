using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrafficLight
{
    public enum Status
    {
        Red,
        Yellow,
        Green
    };

    public TrafficController trafficController;
    public Renderer renderer;
    public Junction junction => trafficController?.junction;

    public List<Vehicle> queue;
    public Road road;
    public Vector3 pos;

    private Status status;

    public float greenInterval = 10.0f;

    public static readonly float minGreenInterval = 5.0f;
    public static readonly float maxGreenInterval = 40.0f;
    public static readonly float yellowInterval = 4.0f;
    public static readonly float redIntervalBuffer = 4.0f;

    public TrafficLight()
    {
        this.trafficController = null;
        this.road = null;
        queue = new List<Vehicle>();

        SetStatus(Status.Red);
    }

    public TrafficLight(TrafficController controller, Road road, Renderer renderer)
    {
        this.trafficController = controller;
        this.renderer = renderer;
        this.road = road;
        queue = new List<Vehicle>();

        Vector3 closestPoint = road.IsCyclic() ?
            road.path.First() :
            Utils.Math.GetClosestVector(junction.obj.transform.localPosition, road.path);

        this.pos = controller.junction.simulation.transform.position + Utils.Math.GetMidpointVector(junction.obj.transform.localPosition, closestPoint);

        SetStatus(Status.Red);
    }

    public bool AddVehicleToQueue(Vehicle vehicle)
    {
        if (queue.Contains(vehicle))
            return false;

        queue.Add(vehicle);
        return true;
    }

    public void ConfigureInterval(float greenInterval)
    {
        this.greenInterval = greenInterval;
    }

    public void SetStatus(Status status)
    {
        this.status = status;
        this.renderer.material.color = TrafficLight.StatusToColor(status);
    }
    public Status GetStatus()
    {
        return status;
    }

    // Maps status to color
    public static Color StatusToColor(Status status)
    {
        switch (status)
        {
            case Status.Red: return Color.red;
            case Status.Green: return Color.green;
            case Status.Yellow: return Color.yellow;
            default: return Color.black;
        }
    }
}
