using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public abstract class Vehicle : MonoBehaviour
{
    public enum Status
    {
        DRIVING,
        WAITING_RED,
        WAITING_CAR,
    };
    public Status status { get; private set; }

    private VehicleManager vehicleManager;

    public VehiclePreset preset;

    public VehiclePath path;

    protected Vector3 bumperOffset;
    protected Vector3 bumperPosition;

    protected float velocity;
    protected float acceleration;
    protected float distanceThisFrame;

    public float timeWaiting = 0f;
    public bool stoppedAtRed = false;
    public BoxCollider vehicleCollider;

    public void Awake()
    {
        vehicleManager = GetComponentInParent<VehicleManager>();
        vehicleCollider = GetComponent<BoxCollider>();

        bumperOffset = GetBumperOffset();
    }

    public void Initialize(VehiclePreset preset, VehiclePath path)
    {
        this.preset = preset;
        this.path = path;
        this.status = Status.DRIVING;
    }

    private void Update()
    {
        // do nothing while waiting on red
        if (stoppedAtRed == true)
        {
            if (upcomingLight.status != TrafficLight.Status.Green)
            {
                timeWaiting += Time.deltaTime;
                return;
            }
            else
            {
                timeWaiting = 0f;
                stoppedAtRed = false;
            }
        }

        if (path.Done())
        {
            vehicleManager.currentVehicleCount--;
            Destroy(gameObject);
            return;
        }

        bumperPosition = transform.localPosition + transform.TransformVector(bumperOffset);

        currentRoad = path.CurrentRoad();
        upcomingJunction = path.UpcomingJunction();
        upcomingLight = upcomingJunction?.trafficController.lights.Find(tl => tl.road == currentRoad);

        HandleVehicle();
    }

    private void HandleVehicle()
    {
        velocity = preset.maxVelocity;
        distanceThisFrame = CalculateDistanceThisFrame();

        while (distanceThisFrame > 0 && !path.Done())
        {
            status = Status.DRIVING;
            velocity = preset.maxVelocity;

            Vector3 nextPoint = path.Next();
            float distanceToNext = Vector3.Distance(transform.localPosition, nextPoint);

            transform.LookAt(nextPoint);

            if (distanceThisFrame >= distanceToNext)
            {
                path.Advance();
                distanceThisFrame -= distanceToNext;

                // handle junction
                if (upcomingJunction != null)
                {
                    switch (upcomingJunction.type)
                    {
                        case Junction.Type.Stops:
                            break;

                        case Junction.Type.Lights:
                            // stop at light instead of potentially skipping it
                            if (upcomingLight.status != TrafficLight.Status.Green && Vector3.Distance(bumperPosition, upcomingLight.transform.position + bumperOffset.y * Vector3.up) <= distanceToNext)
                            {
                                if (!stoppedAtRed)
                                {
                                    stoppedAtRed = true;
                                    upcomingLight.vehicleQueue.Add(this);
                                    timeWaiting = 0f;
                                }

                                Vector3 worldBumperOffset =
                                    (Vector3.Cross(Vector3.up, upcomingLight.roadDirection) * bumperOffset.x) +
                                    (Vector3.up * bumperOffset.y) +
                                    (upcomingLight.roadDirection * bumperOffset.z);

                                transform.localPosition = upcomingLight.transform.position + preset.stoppedGapDistance * upcomingLight.roadDirection + worldBumperOffset;
           
                                status = Status.WAITING_RED;
                                velocity = 0f;
                                continue;
                            }
                            else if (upcomingLight.status == TrafficLight.Status.Green)
                            {
                                if (stoppedAtRed)
                                {
                                    stoppedAtRed = false;
                                    upcomingLight.vehicleQueue.Clear();
                                    timeWaiting = 0f;
                                }
                            }
                            break;
                    }
                }

                // vehicle
                if (vehicleInFront != null)
                {
                    if (Vector3.Distance(transform.position, vehicleInFront.transform.position) <= preset.stoppedGapDistance + vehicleCollider.size.z)
                    {
                        velocity = 0f;
                    }
                }

                transform.localPosition = nextPoint;
            }
            else
            {
                distanceThisFrame -= distanceToNext;

                // handle junction
                if (upcomingJunction != null)
                {
                    switch (upcomingJunction.type)
                    {
                        case Junction.Type.Stops:
                            break;

                        case Junction.Type.Lights:
                            // stop at light instead of potentially skipping it
                            if (upcomingLight.status != TrafficLight.Status.Green && Vector3.Distance(bumperPosition, upcomingLight.transform.position + bumperOffset.y * Vector3.up) <= preset.stoppedGapDistance)
                            {
                                if (!stoppedAtRed)
                                {
                                    stoppedAtRed = true;
                                    upcomingLight.vehicleQueue.Add(this);
                                }

                                status = Status.WAITING_RED;
                                velocity = 0f;
                                continue;
                            }
                            else if (upcomingLight.status == TrafficLight.Status.Green)
                            {
                                if (stoppedAtRed)
                                {
                                    upcomingLight.vehicleQueue.Clear();
                                    stoppedAtRed = false;
                                }
                            }
                            break;
                    }
                }

                // vehicle
                if (vehicleInFront != null)
                {
                    if (Vector3.Distance(transform.position, vehicleInFront.transform.position) <= preset.stoppedGapDistance + vehicleCollider.size.z)
                    {
                        velocity = 0f;
                    }
                }

                Vector3 nextPointDirection = (nextPoint - transform.localPosition).normalized;
                transform.localPosition += nextPointDirection * velocity * Time.deltaTime;
            }
        }
    }

    public Road currentRoad;
    public Junction upcomingJunction;
    public TrafficLight upcomingLight;

    public Vehicle vehicleInFront;

    public void HandleObstacle()
    {
        // handle junction
        if (upcomingJunction != null)
        {
            switch (upcomingJunction.type)
            {
                case Junction.Type.Stops:
                    break;

                case Junction.Type.Lights:

                    if (upcomingLight != null)
                    {
                        if (upcomingLight.status != TrafficLight.Status.Green && Vector3.Distance(bumperPosition, upcomingLight.transform.position + bumperOffset.y * Vector3.up) <= preset.stoppedGapDistance)
                            velocity = 0f;
                    }

                    break;
            }
        }

        // vehicle
        if (vehicleInFront != null)
        {
            if (Vector3.Distance(transform.position, vehicleInFront.transform.position) <= preset.stoppedGapDistance + vehicleCollider.size.z)
            {
                velocity = 0f;
            }
        }
    }

    public virtual void OnDrawGizmos()
    {
        //Gizmos.color = new Color(0f, 0f, 1f);
        //for (int i = 0; i < path.roads.Count; ++i)
        //{
        //    Handles.Label(path.roads[i].path.First() + Vector3.up, $"{i}");
        //    Gizmos.DrawLineStrip(path.roads[i].path.Select(p => p + Vector3.up).ToArray(), false);
        //}

        //if (path.junctions != null)
        //{
        //    Gizmos.color = new Color(0f, 1f, 1f);
        //    for (int i = 0; i < path.junctions.Count; ++i)
        //    {
        //        Handles.Label(path.junctions[i].transform.position + Vector3.up, $"{i}");
        //        Gizmos.DrawSphere(path.junctions[i].transform.position + Vector3.up, 0.5f);
        //    }
        //}
    }

    public static bool AreLookingAtEachother(Vehicle v1, Vehicle v2)
    {
        return Vector3.Dot(v1.transform.forward, v2.transform.forward) <= -0.95 &&
            Vector3.Distance(v1.transform.position, v2.transform.position) <= 10f;
    }

    private bool IsJunctionExitBlocked(Junction junction)
    {
        bool reachedJunction = false;

        for (int i = 1; i < path.Length(); ++i)
        {
            if (junction.IsPointInside(path.Next(i - 1)))
            {
                reachedJunction = true;
            }

            if (reachedJunction)
            {
                Vector3 a = path.Next(i - 1);
                Vector3 b = path.Next(i);
                Vector3 dir = (b - a).normalized;

                if (Mathf.Abs(Vector3.Dot(dir, Vector3.right)) > 0.999f ||
                    Mathf.Abs(Vector3.Dot(dir, Vector3.forward)) > 0.999f)
                {
                    Vector3 right = -Vector3.Cross(dir, Vector3.up);

                    Vector3 worldBumperOffset =
                        (right * bumperOffset.x) +
                        (Vector3.up * bumperOffset.y) +
                        (dir * bumperOffset.z);

                    return Physics.Raycast(junction.transform.position + worldBumperOffset - dir, dir, GameManager.Instance.tileSize, 1 << 6);
                }
            }
        }

        return false;
    }

    public abstract Vector3 GetBumperOffset();

    protected virtual bool IsStopped()
    {
        return Mathf.Abs(velocity) <= 0.01f;
    }

    protected virtual float CalculateDistanceThisFrame()
    {
        return velocity * Time.deltaTime;
    }

    protected virtual Quaternion CalculateRotation(Vector3 lookDirection)
    {
        return Quaternion.Slerp(transform.localRotation, Quaternion.LookRotation(lookDirection, Vector3.up), 10f * Time.deltaTime);
    }

    protected virtual Vector3 CalculateLookDirection()
    {
        int lookIndex = Mathf.Clamp(path.Length() - 1, 0, 2);
        Vector3 pathDir = (path.Next(lookIndex) - transform.localPosition).normalized;

        return pathDir;
    }
}