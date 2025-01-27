using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    public static readonly float minGreenInterval = 5.0f;
    public static readonly float maxGreenInterval = 40.0f;
    public static readonly float yellowInterval = 4.0f;
    public static readonly float redIntervalBuffer = 4.0f;

    public enum Status
    {
        Red,
        Yellow,
        Green
    };

    public Road road;
    public Vector3 roadDirection;  // The direction to the road

    public List<Vehicle> vehicleQueue;
    public TrafficController trafficController;

    private Status _status;
    public Status status
    {
        get => _status;
        set
        {
            _status = value;

            meshRenderer.material.color = TrafficLight.StatusToColor(_status);

            if (value == Status.Green)
                vehicleQueue.Clear();
        }
    }

    public float greenInterval = 10.0f;
    private MeshRenderer meshRenderer;

    public void Awake()
    {
        trafficController = GetComponentInParent<TrafficController>();

        meshRenderer = GetComponent<MeshRenderer>();

        vehicleQueue.Clear();
        status = Status.Red;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = TrafficLight.StatusToColor(status);
        Gizmos.DrawSphere(transform.position, 1f);
        Handles.Label(transform.position + 3f * Vector3.up, $"{vehicleQueue.Count}");
    }

    public void ConfigureInterval(float greenInterval)
    {
        this.greenInterval = greenInterval;
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
