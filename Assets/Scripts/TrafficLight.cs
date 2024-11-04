using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Status
{
    Red,
    Yellow,
    Green
};

public class TrafficLight
{
    public Junction junction;
    public Road road;
    Status status;
    Status prevStatus;
    public float greenInterval = 10.0f;
    public float redInterval = 10.0f;
    public static readonly float yellowInterval = 2.0f;
    public float elapsedTime = 0.0f;

    public TrafficLight(Junction junction, Road road)
    {
        this.junction = junction;
        this.road = road;
        status = Status.Red;
        prevStatus = Status.Red;
    }

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

    public void Update()
    {
        elapsedTime += Time.deltaTime;

        if (status == Status.Yellow && elapsedTime > yellowInterval)
        {
            if (prevStatus == Status.Red)
                SetStatus(Status.Green);
            else if (prevStatus == Status.Green)
                SetStatus(Status.Red);
        }
        else if ((status == Status.Red && elapsedTime > redInterval) ||
                 (status == Status.Green && elapsedTime > greenInterval))
            SetStatus(Status.Yellow);
    }

    // Changes traffic light status and resets elapsed time
    private void SetStatus(Status newStatus)
    {
        prevStatus = status;
        status = newStatus;
        elapsedTime = 0.0f;
    }
}
