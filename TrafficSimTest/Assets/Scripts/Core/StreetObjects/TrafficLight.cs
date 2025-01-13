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

    public Road road;
    public Vector3 pos;

    public Status status { get; set; }
    public float greenInterval = 10.0f;

    public static readonly float minGreenInterval = 5.0f;
    public static readonly float maxGreenInterval = 40.0f;
    public static readonly float yellowInterval = 4.0f;
    public static readonly float redIntervalBuffer = 4.0f;

    public int queueLength;

    public TrafficLight()
    {
        this.trafficController = null;
        this.road = null;
        queueLength = 0;

        status = Status.Red;
    }

    public TrafficLight(TrafficController controller, Road road)
    {
        this.trafficController = controller;
        this.road = road;
        queueLength = 0;

        Vector3 closestPoint = road.IsCyclic() ?
            road.path.First() :
            Utils.Math.GetClosestVector(junction.obj.transform.localPosition, road.path);

        this.pos = controller.junction.simulation.transform.position + Utils.Math.GetMidpointVector(junction.obj.transform.localPosition, closestPoint);
        status = Status.Red;
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
