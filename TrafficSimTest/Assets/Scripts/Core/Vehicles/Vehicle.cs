using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
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

    public VehicleManager vehicleManager;

    public VehiclePreset preset;

    public VehiclePath path;

    protected Vector3 bumperOffset;
    protected Vector3 bumperPosition;

    protected float velocity;
    protected float acceleration;

    public bool invisible;
    public Collider vehicleCollider;

    public void Initialize(VehicleManager vehicleManager, VehiclePreset preset, VehiclePath path)
    {
        this.vehicleManager = vehicleManager;
        this.preset = preset;
        this.path = path;
        this.status = Status.DRIVING;
    }

    protected void Start()
    {
        bumperOffset = GetBumperOffset();
        vehicleCollider = GetComponent<Collider>();
    }

    private void FixedUpdate()
    {
        if (path.Done())
        {
            vehicleManager.DestroyVehicle(this);
            Destroy(gameObject);
            return;
        }

        bumperPosition = transform.localPosition + transform.TransformVector(bumperOffset);
        HandleVehicle();
    }

    private bool CheckObstacle(out RaycastHit hit)
    {
        if (invisible)
        {
            hit = default(RaycastHit);
            return false;
        }

        return Physics.Raycast(bumperPosition, transform.forward, out hit, preset.lookAheadDistance);
    }

    TrafficLight tlight;

    private void HandleVehicle()
    {
        velocity = preset.maxVelocity;
        float distanceThisFrame = CalculateDistanceThisFrame();

        while (distanceThisFrame > 0 && !path.Done())
        {
            status = Status.DRIVING;
            velocity = preset.maxVelocity;

            // obstacle
            if (CheckObstacle(out RaycastHit hit))
            {
                float trueDistance = hit.distance - preset.stoppedGapDistance;

                if (trueDistance <= 0f)
                {
                    if (hit.collider.CompareTag("Junction"))
                    {
                        Junction junction = vehicleManager.simulation.junctionsDict[hit.collider.gameObject];
                        TrafficLight trafficLight = vehicleManager.GetTrafficLight(this, junction);

                        if (junction.type == Junction.Type.Lights && trafficLight.status != TrafficLight.Status.Green)
                        {
                            status = Status.WAITING_RED;
                            tlight = trafficLight;
                            tlight.AddVehicleToQueue(this);
                            velocity = 0.0f;
                        }
                        else if (IsJunctionExitBlocked(junction))
                        {
                            status = Status.WAITING_CAR;
                            velocity = 0.0f;
                        }
                    }

                    if (hit.collider.CompareTag("Vehicle"))
                    {
                        Vehicle vehicleInFront = hit.collider.GetComponent<Vehicle>();

                        velocity = 0.0f;
                        status = vehicleInFront.status;
                        
                        if (status == Status.WAITING_RED)
                        {
                            tlight = vehicleInFront.tlight;
                            tlight.AddVehicleToQueue(this);
                        }
                    }
                }
            }

            Vector3 nextPoint = path.Next();
            float distanceToNext = Vector3.Distance(transform.localPosition, nextPoint);

            if (distanceThisFrame >= distanceToNext)
            {
                transform.localPosition = nextPoint;
                path.Advance();
                distanceThisFrame -= distanceToNext;
            }
            else
            {
                Vector3 direction = (nextPoint - transform.localPosition).normalized;
                transform.localPosition += direction * velocity * Time.deltaTime;

                Vector3 lookDirection = CalculateLookDirection();
                transform.rotation = CalculateRotation(lookDirection);
                distanceThisFrame = 0;
            }
        }
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

                if (Mathf.Abs(Vector3.Dot(dir, Vector3.right)) > 0.99f ||
                    Mathf.Abs(Vector3.Dot(dir, Vector3.forward)) > 0.99f)
                {
                    Vector3 right = -Vector3.Cross(dir, Vector3.up);

                    Vector3 worldBumperOffset =
                        (right * bumperOffset.x) +
                        (Vector3.up * bumperOffset.y) +
                        (dir * bumperOffset.z);

                    exitBumperPos = a + worldBumperOffset - dir;
                    exitDir = dir;

                    return Physics.Raycast(exitBumperPos - dir, dir, 10f);
                }
            }
        }

        return false;
    }

    public Vector3 exitBumperPos = -Vector3.one;
    public Vector3 exitDir = -Vector3.one;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Junction"))
        {
            invisible = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Junction"))
        {
            invisible = false;
        }
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
        return Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), 10f * Time.deltaTime);
    }

    protected virtual Vector3 CalculateLookDirection()
    {
        return path.Length() > 1 ?
            (path.Next(1) - transform.localPosition).normalized :
            (path.Next() - transform.localPosition).normalized;
    }
}