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
    public Junction junction => trafficController?.junction;

    public List<Vehicle> queue;
    public Road road;
    public Vector3 pos;

    public Status status { get; set; }
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

        status = Status.Red;
    }

    public TrafficLight(TrafficController controller, Road road)
    {
        this.trafficController = controller;
        this.road = road;
        queue = new List<Vehicle>();

        Vector3 closestPoint = road.IsCyclic() ?
            road.path.First() :
            Utils.Math.GetClosestVector(junction.obj.transform.localPosition, road.path);

        this.pos = controller.junction.simulation.transform.position + Utils.Math.GetMidpointVector(junction.obj.transform.localPosition, closestPoint);
        status = Status.Red;
    }

    public bool AddVehicleToQueue(Vehicle vehicle)
    {
        if (!queue.Contains(vehicle))
            return false;

        queue.Add(vehicle);
        return true;
    }

    public void ConfigureInterval(float greenInterval)
    {
        this.greenInterval = greenInterval;
    }

    // Returns the traffic light color
    public Color GetStatusColor()
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
