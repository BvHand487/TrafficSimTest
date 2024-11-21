using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;


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

    private List<Junction> path;
    private Junction currentJunc;
    private Road currentRoad;
    private int fromRoadTile, toRoadTile;
    private float acceptibleCompletionDist = 0.2f;

    public void Initialize(List<Junction> path, int fromRoadTile, int toRoadTile)
    {
        this.path = path;
        this.currentJunc = path[0];
        this.currentRoad = Junction.GetCommonRoad(path[0], path[1]);
        this.fromRoadTile = fromRoadTile;
        this.toRoadTile = toRoadTile;
    }

    void Start()
    {
        simulation = simulationObject.GetComponent<Simulation>();

        var collider = GetComponent<BoxCollider>();
        bumperOffset = new Vector3(collider.center.x, collider.center.y, collider.size.z / 2 + collider.center.z);
        
        status = Status.DRIVING;
    }

    void Update()
    {
        if (IsOnLastRoad() && IsWithinCompletionDist())
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

        if (Physics.Raycast(transform.position + bumperOffset, Vector3.forward, out hit, lookDistance))
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

                if (hit.collider.tag == "Junction")
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
        velocity += CalculateAcceleration() * Time.deltaTime;

        if (isAccelerating && velocity.magnitude >= maxSpeed)
            velocity = maxSpeed * Vector3.forward;
       
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
            Vector3 traction = Vector3.forward * torque;
            totalForce += traction;
        }
        else
        {
            Vector3 braking = -Vector3.forward * brakingConstant * brakingFactor;
            totalForce += braking;
        }

        return totalForce / mass;
    }

    private bool IsOnLastRoad()
    {
        return (
            (currentRoad?.j1 == path?[path.Count - 2] && currentRoad?.j2 == path?[path.Count - 1]) ||
            (currentRoad?.j2 == path?[path.Count - 2] && currentRoad?.j1 == path?[path.Count - 1])
        );
    }

    private bool IsWithinCompletionDist()
    {
        if (currentRoad == null || currentRoad.path == null)
            return false;

        return Vector3.Distance(transform.position, currentRoad.path[toRoadTile]) <= acceptibleCompletionDist;
    }

    private float CalculateBrakingFactor(float distance)
    {
        float requiredForce = ((mass * velocity.magnitude * velocity.magnitude) / 2) / distance;
        return Mathf.Clamp01((requiredForce + dragConstant * velocity.magnitude + rrConstant * velocity.magnitude) / (brakingConstant * 1.0f));
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

            if (collision.gameObject == currentRoad.j1.obj)
                currentJunc = currentRoad.j1;
            else if (collision.gameObject == currentRoad.j2.obj)
                currentJunc = currentRoad.j2;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == "Junction")
        {
            canStop = true;
            isAccelerating = true;

            int juncIndex = path.FindIndex((j) => j.obj == collision.gameObject);
            currentRoad = Junction.GetCommonRoad(path[juncIndex], path[juncIndex + 1]);
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
        Gizmos.DrawSphere(transform.position + bumperOffset, 1);
    }
}
