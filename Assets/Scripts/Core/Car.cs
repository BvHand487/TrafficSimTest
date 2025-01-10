using System.Linq;
using UnityEngine;

public class Car : MonoBehaviour
{
    public enum Status
    {
        DRIVING,
        STOPPING,
        WAITING_RED,
        WAITING_CAR
    };
    public Status status { get; private set; }

    public float stopDistance = 20.0f;
    public float stopGap = 3.0f;

    public float maxSpeed = 25.0f;
    public float accelerationRate = 5.0f;
    public float decelerationRate = 10.0f;

    private float velocity = 0.0f;
    private float acceleration = 0.0f;
    private bool invisible = false;
    private bool canStop = true;

    private Vector3 bumperOffset;
    private Vector3 bumperPosition;

    private Simulation simulation;
    private BoxCollider carCollider;

    public CarPath carPath;

    public ParticleSystem carEventEffect;

    public void Initialize(CarPath carPath)
    {
        simulation = FindObjectOfType<Simulation>().GetComponent<Simulation>();
        carCollider = GetComponent<BoxCollider>();
        bumperOffset = new Vector3(carCollider.center.x, carCollider.center.y, carCollider.center.z + carCollider.size.z / 2);

        this.carPath = carPath;

        status = Status.DRIVING;

        PlayEffect();
    }

    void Update()
    {
        if (carPath.Done())
        {
            simulation.currentCars--;
            Destroy(gameObject);
            return;
        }

        bumperPosition = transform.position + transform.TransformVector(bumperOffset);
        Move();
    }

    private void Check()
    {
        status = Status.DRIVING;
        acceleration = accelerationRate;

        if (Physics.Raycast(bumperPosition, transform.forward, out RaycastHit hit, stopDistance))
        {
            float trueDistance = hit.distance - stopGap;

            switch (hit.collider.tag)
            {
                case "Junction":
                    HandleJunction(hit, trueDistance);
                    break;

                case "Car":
                    HandleCar(hit, trueDistance);
                    break;
            }
        }
    }

    private void HandleJunction(RaycastHit hit, float trueDistance)
    {
        Junction junction = simulation.junctionsDict[hit.collider.gameObject];
        bool isClear = IsExitClear(junction);
        bool shouldStop = ShouldStop(trueDistance);
        bool isStopped = IsStopped();

        if (junction.type == Junction.Type.Lights)
        {
            TrafficLight.Status trafficLightStatus = simulation.GetTrafficLightStatus(this, junction);
            if (trafficLightStatus != TrafficLight.Status.Green || !isClear)
            {
                status = Status.STOPPING;

                if (shouldStop)
                    acceleration = StopAccel();

                if (isStopped)
                    status = Status.WAITING_RED;
            }
        }
        else
        {
            // For uncontrolled junctions, only check for exit clearance
            if (!isClear)
            {
                status = Status.STOPPING;

                if (shouldStop)
                    acceleration = StopAccel();

                if (isStopped)
                    status = Status.WAITING_CAR;
            }
        }
    }

    private void HandleCar(RaycastHit hit, float trueDistance)
    {
        Car hitCar = hit.collider.gameObject.GetComponent<Car>();
        foreach (var j in simulation.junctions)
            if (j.IsPointInside(hitCar.transform.position))
                return;

        if (trueDistance < 0.0f)
        {
            acceleration = 0f;
            velocity = 0f;
        }

        if (hitCar.status != Status.DRIVING)
        {
            status = Status.STOPPING;

            if (ShouldStop(trueDistance))
                acceleration = StopAccel();

            if (IsStopped() && (hitCar.status == Status.WAITING_RED || hitCar.status == Status.WAITING_CAR))
                status = hitCar.status;
        }
    }

    private float StopAccel()
    {
        return -decelerationRate;
    }

    private bool ShouldStop(float trueDistance)
    {
        float timeToStop = velocity / decelerationRate;
        float stoppingDistance = velocity * timeToStop - 0.5f * decelerationRate * timeToStop * timeToStop;

        return stoppingDistance >= trueDistance || trueDistance < stopGap;
    }

    private void Move()
    {
        float deltaTime = Time.deltaTime;
        float distanceThisFrame = velocity * deltaTime + 0.5f * acceleration * deltaTime * deltaTime;
        velocity = Mathf.Clamp(velocity + acceleration * deltaTime, 0.0f, maxSpeed);

        while (distanceThisFrame > 0 && !carPath.Done())
        {
            Vector3 nextPoint = carPath.Next();
            float distanceToNext = Vector3.Distance(transform.position, nextPoint);

            if (distanceThisFrame >= distanceToNext)
            {
                transform.position = nextPoint;
                carPath.Advance();
                distanceThisFrame -= distanceToNext;
            }
            else
            {
                Vector3 direction = (nextPoint - transform.position).normalized;
                transform.position += direction * velocity * deltaTime;

                Vector3 lookDirection = carPath.points.Count() > 2 ?
                    (carPath.Next(1) - transform.position).normalized :
                    (carPath.Last() - transform.position).normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), 10f * deltaTime);
                distanceThisFrame = 0;
            }
        }
    }

    private bool IsStopped()
    {
        return Mathf.Abs(velocity) < 0.01f;
    }

    //public Vector3 exitHit;
    //public Vector3 dir;

    private bool IsExitClear(Junction junction)
    {
        //bool enteredJunction = false;
        //Vector3 exitPosition = Vector3.zero;
        //Vector3 exitDirection = Vector3.zero;

        //for (int i = 0; i < carPath.points.Count; ++i)
        //{
        //    if (junction.IsPointInside(carPath.points[i]))
        //        enteredJunction = true;

        //    if (enteredJunction && !junction.IsPointInside(carPath.points[i]))
        //    {
        //        exitHit = carPath.points[i - 1];
        //        dir = carPath.points[i] - exitPosition;
        //        exitPosition = carPath.points[i - 1];
        //        exitDirection = carPath.points[i] - exitPosition;
        //        break;
        //    }
        //}

        //Vector3 forward = exitDirection.normalized;
        //Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        //Vector3 worldOffset = forward * bumperOffset.z + right * bumperOffset.x;

        //if (Physics.Raycast(exitPosition + worldOffset, forward, out RaycastHit hit, stopDistance))
        //    if (hit.collider.CompareTag("Car") && hit.collider.GetComponent<Car>().IsStopped())
        //        return false;

        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Junction"))
        {
            status = Status.DRIVING;
            canStop = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Junction"))
        {
            canStop = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Junction"))
        {
            status = Status.DRIVING;
            canStop = false;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Junction"))
        {
            status = Status.DRIVING;
            canStop = false;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Junction"))
        {
            canStop = true;
        }
    }

    void OnDrawGizmos()
    {
        // car junction exit check
        //if (exitHit != null)
        //{
        //    Vector3 forward = dir.normalized;
        //    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        //    Vector3 worldOffset = forward * bumperOffset.z + right * bumperOffset.x;
        //    Vector3 start = exitHit + worldOffset;
        //    Vector3 end = exitHit + worldOffset + dir;
        //    Gizmos.color = Color.magenta;
        //    Gizmos.DrawSphere(start, 1);
        //    Gizmos.DrawLine(start, end);
        //    Gizmos.DrawSphere(end, 1);
        //}

        // car path
        //if (carPath.points != null)
        //{
        //    Gizmos.color = Color.blue;
        //    Gizmos.DrawLineStrip(carPath.points.ToArray(), false);

        //    foreach (var p in carPath.points)
        //        Gizmos.DrawSphere(p, 0.2f);
        //}

        // car status check
        switch (status)
        {
            case Status.DRIVING:
                Gizmos.color = Color.green;
                break;
            case Status.STOPPING:
                Gizmos.color = Color.yellow;
                break;
            case Status.WAITING_CAR:
                Gizmos.color = Color.black;
                break;
            case Status.WAITING_RED:
                Gizmos.color = Color.red;
                break;
        }

        Gizmos.DrawCube(bumperPosition + Vector3.up, Vector3.one);
    }

    void PlayEffect()
    {
        if (carEventEffect == null)
            return;

        var effect = Instantiate(carEventEffect, transform.position + carCollider.center, Quaternion.identity);
        var settings = effect.main;

        settings.loop = false;
        settings.useUnscaledTime = true;
        effect.Play();

        Destroy(effect.gameObject, Time.timeScale * settings.duration);
    }
}