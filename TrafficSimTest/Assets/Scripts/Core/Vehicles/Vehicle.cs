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
        vehicleCollider.enabled = !invisible;
        HandleVehicle();
    }

    private bool CheckObstacle(out RaycastHit hit)
    {
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
                            // ...
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
        // Check if the exit of the junction is blocked by other vehicles
        foreach (var vehicle in vehicleManager.vehicles)
        {
            if (vehicle.status == Status.DRIVING && vehicle.path == path && vehicle != this)
            {
                // If any vehicle on the same path is driving, the junction exit is blocked
                return true;
            }
        }
        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Junction"))
        {
            Debug.Log("junction enter");
            invisible = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("Junction"))
        {
            Debug.Log("junction exit");
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