using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;


public class Car : MonoBehaviour
{
    public enum Status
    {
        DRIVING,
        WAITING_ON_RED,
        WAITING_ON_CAR,
    }

    public float carDistance = 20.0f;
    public float lookDistance = 30.0f;
    public float lightDistance = 20.0f;
    public float stoppedGap = 3.0f;

    public float maxSpeed = 25.0f;

    public float mass = 1800;

    public float torque = 500;
    public float dragConstant = 0.01f;
    public float rrConstant = 0.30f;
    public float brakingConstant = 35000.0f;

    public GameObject simulationObject;

    private Vector3 bumperOffset;
    private Vector3 velocity = Vector3.zero;
    private bool isAccelerating = true;

    // Represents how hard the car is braking (0.0f, 1.0f)
    private float brakingFactor = 0.0f;

    public Status status { get; private set; }

    private bool canStop = true;
    private Simulation simulation;

    private List<Vector3> path;
    private int pathIndex = 0;
    private Vector3 from, to;
    private float acceptibleCompletionDist = 0.2f;

    public void Initialize(List<Vector3> path, Vector3 from, Vector3 to)
    {
        this.path = new List<Vector3>(path);
        this.from = from;
        this.to = to;
    }

    void Start()
    {
        simulation = simulationObject.GetComponent<Simulation>();

        var collider = GetComponent<BoxCollider>();
        bumperOffset = new Vector3(collider.center.x, collider.center.y, collider.center.z + collider.size.z / 2);
        
        status = Status.DRIVING;
    }

    void Update()
    {
        if (IsWithinCompletionDist())
        {
            simulation.currentCars--;
            Destroy(this);
        }

        if (canStop)
            LookAhead();

        Move();
    }

    private void LookAhead()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position + bumperOffset, transform.forward, out hit, lookDistance))
        {
            if (hit.collider != null)
            {
                if (hit.collider.tag == "Car" && hit.distance < carDistance)
                {
                    isAccelerating = false;

                    if (IsStopped())
                    {
                        Car carInFront = hit.collider.gameObject.GetComponent<Car>();

                        switch (carInFront.status)
                        {
                            case Status.WAITING_ON_RED:
                                status = Status.WAITING_ON_RED; break;
                            case Status.WAITING_ON_CAR:
                                status = Status.WAITING_ON_CAR; break;

                            default: status = Status.WAITING_ON_CAR; break;
                        }
                    }
                    else
                        status = Status.DRIVING;
                }

                else if (hit.collider.tag == "Junction")
                {
                    var status = simulation.GetTrafficLightStatus(this, hit.collider.gameObject);

                    if ((status == TrafficLight.Status.Red || status == TrafficLight.Status.Yellow) && hit.distance < lightDistance)
                    {
                        brakingFactor = CalculateBrakingFactor(hit.distance - stoppedGap);

                        isAccelerating = false;

                        if (IsStopped())
                            this.status = Status.WAITING_ON_RED;
                        else
                            this.status = Status.DRIVING;
                    }
                    else
                    {
                        isAccelerating = true;
                        this.status = Status.DRIVING;
                    }
                }

                else if (hit.collider.tag == "Turn")
                {
                    
                }
            }
            else
            {
                isAccelerating = true;
                status = Status.DRIVING;
            }
        }
    }

    private void Move()
    {
        bumperOffset = transform.rotation * bumperOffset * Time.deltaTime;
        if (pathIndex < path.Count - 1)
        {
            if (Vector3.Distance(transform.position, path[pathIndex + 1]) < 0.10f)
                pathIndex++;

            float turnSpeed = Mathf.Lerp(5.0f, 20.0f, velocity.magnitude / maxSpeed);
            Quaternion targetRotation = Quaternion.LookRotation(path[pathIndex + 1] - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5.0f);
        }

        velocity += CalculateAcceleration() * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        if (IsStopped())
            velocity.z = 0.0f;

        transform.position += velocity * Time.deltaTime;
    }

    private Vector3 CalculateAcceleration()
    {
        Vector3 drag = -dragConstant * velocity.magnitude * velocity;
        Vector3 rollingResistance = -rrConstant * velocity;

        Vector3 totalForce = drag + rollingResistance;

        if (isAccelerating)
        {
            Vector3 traction = transform.forward * torque;
            totalForce += traction;
        }
        else
        {
            Vector3 braking = -transform.forward * brakingConstant * brakingFactor;
            totalForce += braking;
        }

        return totalForce / mass;
    }

    private bool IsWithinCompletionDist()
    {
        return Vector3.Distance(transform.position, to) <= acceptibleCompletionDist;
    }

    private float CalculateBrakingFactor(float distance)
    {
        float kineticEnergy = 0.5f * mass * velocity.magnitude * velocity.magnitude;
        float requiredForce = kineticEnergy / distance;

        return Mathf.Clamp01((requiredForce + (dragConstant + rrConstant) * velocity.magnitude) / brakingConstant);
    }

    private bool IsStopped()
    {
        return velocity.z <= 0.0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Junction")
        {
            canStop = false;
            isAccelerating = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == "Junction")
        {
            canStop = true;
            isAccelerating = true;
        }
    }

    private void OnDrawGizmos()
    {
        switch (status)
        {
            case Status.DRIVING:
                Gizmos.color = Color.green; break;
            case Status.WAITING_ON_CAR:
                Gizmos.color = Color.yellow; break;
            case Status.WAITING_ON_RED:
                Gizmos.color = Color.red; break;
            default:
                Gizmos.color = Color.black; break;
        }
        Gizmos.DrawCube(transform.GetChild(0).transform.position + 3 * Vector3.up, Vector3.one);

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(transform.position + bumperOffset, 0.8f);

        Gizmos.color = Color.cyan;
        var line = path.GetRange(pathIndex, path.Count - pathIndex);
        for (int i = 0; i < line.Count; ++i)
            line[i] += i * Vector3.up;
        Gizmos.DrawLineStrip(line.ToArray(), false);

        Gizmos.color = Color.grey;
        Gizmos.DrawLine(transform.position + Vector3.up, transform.position + 5 * transform.forward + Vector3.up);
    }
}
