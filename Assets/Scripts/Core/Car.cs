using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

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
    public float speedUp = 5.0f;

    private float velocity = 0.0f;
    private float acceleration = 0.0f;
    // private float brakingFactor = 0.0f;
    private bool canStop = true;


    private Vector3 bumperOffset;
    private Vector3 bumperPosition;

    private Simulation simulation;
    private BoxCollider carCollider;

    private List<Vector3> path; 
    private Vector3 from, to;
    private int pathIndex = 0;
    
    public ParticleSystem carEventEffect;

    public void Initialize(List<Vector3> path, Vector3 from, Vector3 to)
    {
        simulation = FindObjectOfType<Simulation>().GetComponent<Simulation>();
        carCollider = GetComponent<BoxCollider>();
        bumperOffset = new Vector3(carCollider.center.x, carCollider.center.y, carCollider.center.z + carCollider.size.z / 2);

        this.path = path;
        this.from = from;
        this.to = to;

        status = Status.DRIVING;

        PlayEffect();
    }


    void Update()
    {
        if (pathIndex >= path.Count)
        {
            simulation.currentCars--;
            Destroy(gameObject);
            return;
        }

        bumperPosition = transform.position + transform.TransformVector(bumperOffset);
        if (canStop)
            Check();
        Move();

        if (Vector3.Distance(transform.position, path[pathIndex]) < 1.0f)
            pathIndex++;

        //CheckObstacles();
        //UpdateMovement();
    }

    private void Check()
    {
        status = Status.DRIVING;

        if (Physics.Raycast(bumperPosition, transform.forward, out RaycastHit hit, stopDistance))
        {
            float trueDistance = hit.distance - stopGap;

            switch (hit.collider.tag)
            {
                case "Junction":
                    Junction junction = simulation.junctionsDict[hit.collider.gameObject];

                    switch (junction.type)
                    {
                        case Junction.Type.Lights:
                            TrafficLight.Status trafficLightStatus = simulation.GetTrafficLightStatus(this, junction);
                            if (trafficLightStatus != TrafficLight.Status.Green)
                            {
                                status = Status.STOPPING;
                                acceleration = 2f * (trueDistance - maxSpeed * 3f) / 9f;

                                if (IsStopped())
                                    status = Status.WAITING_RED;
                            }
                            break;

                        case Junction.Type.Stops:
                            break;
                    }
                    break;

                case "Car":
                    Car hitCar = hit.collider.gameObject.GetComponent<Car>();

                    if (trueDistance < 0.0f)
                    {
                        acceleration = 0f;
                        velocity = 0f;
                    }

                    if (hitCar.status != Status.DRIVING)
                    {
                        status = Status.STOPPING;
                        acceleration = 2f * (trueDistance - maxSpeed * 3f) / 9f;

                        if (IsStopped() && (hitCar.status == Status.WAITING_RED || hitCar.status == Status.WAITING_CAR))
                            status = hitCar.status;
                    }

                    if (IsStopped() && hitCar.IsStopped() && trueDistance < 0.0f)
                    {
                        Car carWithPriority;

                        if (transform.position.x > hitCar.transform.position.x)
                            carWithPriority = this;
                        else
                            carWithPriority = hitCar;

                        carWithPriority.status = Status.DRIVING;
                    }
                    break;
            }
        }
    }

    private void Move()
    {
        switch (status)
        {
            case Status.DRIVING:
                acceleration = speedUp;
                break;
        }

        Vector3 direction = (path[pathIndex] - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, path[pathIndex]);
        if (distanceToTarget >= 0.05f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);

        velocity = Mathf.Clamp(velocity + acceleration * Time.deltaTime, 0.0f, maxSpeed);
        transform.position += transform.forward * velocity * Time.deltaTime;
    }

    private bool IsStopped()
    {
        return Utils.Math.CompareFloat(velocity, 0.0f, 0.2f);
    }

    //private void CheckObstacles()
    //{
    //    status = Status.DRIVING;

    //    if (Physics.Raycast(bumperPosition, transform.forward, out RaycastHit hit, stopDistance))
    //    {
    //        if (hit.collider.CompareTag("Junction"))
    //        {
    //            Junction junction = simulation.junctions.Find(j => j.obj == hit.collider.gameObject);

    //            if (junction.type == Junction.Type.Lights)
    //            {
    //                TrafficLight.Status trafficLightStatus = simulation.GetTrafficLightStatus(this, junction);
    //                if (trafficLightStatus == TrafficLight.Status.Red)
    //                    status = Status.WAITING_RED;
    //            }
    //            else if (junction.type == Junction.Type.Stops)
    //            {

    //            }
    //        }

    //        else if (hit.collider.CompareTag("Car"))
    //        {
    //            Car hitCar = hit.collider.gameObject.GetComponent<Car>();
                
    //            status = Status.WAITING_CAR;

    //            if (hitCar.status == Status.WAITING_RED)
    //                status = Status.WAITING_RED;
    //        }
    //    }
    //}

    //private void UpdateMovement()
    //{
    //    if (pathIndex >= path.Count)
    //    {
    //        simulation.currentCars--;
    //        PlayEffect();
    //        Destroy(gameObject);
    //        return;
    //    }

    //    Vector3 target = path[pathIndex];
    //    Vector3 direction = (target - transform.position).normalized;
    //    float distanceToTarget = Vector3.Distance(transform.position, target);

    //    float targetSpeed = maxSpeed;
    //    if (status != Status.DRIVING)
    //        targetSpeed = 0.0f;
    //    else if (pathIndex + 2 < path.Count && Vector3.Dot(transform.forward, path[pathIndex + 2] - path[pathIndex + 1]) <= 0.02f)  // entering turn
    //    {
    //        //if (Vector3.Dot(transform.right, path[pathIndex + 1] - path[pathIndex]) > 0.02f)
    //            //targetSpeed = rightTurnSpeed;
    //        // else
    //           // targetSpeed = leftTurnSpeed;
    //    }

    //    // Smoothly adjust velocity
    //    float speedDifference = targetSpeed - velocity.magnitude;
    //    float accelerationRate = speedDifference > 0 ? acceleration : deceleration;
    //    velocity = transform.forward * velocity.magnitude;
    //    velocity += direction * Mathf.Clamp(speedDifference, -accelerationRate * Time.deltaTime, accelerationRate * Time.deltaTime);

    //    // Move car
    //    transform.position += velocity * Time.deltaTime;

    //    // Rotate towards target
    //    if (distanceToTarget > 0.05f)
    //    {
    //        Quaternion targetRotation = Quaternion.LookRotation(direction);
    //        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10.0f * Time.deltaTime);
    //    }
    //}

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Junction")
        {
            status = Status.DRIVING;
            canStop = false;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == "Junction")
        {
            canStop = true;
        }
    }

    void OnDrawGizmos()
    {

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
