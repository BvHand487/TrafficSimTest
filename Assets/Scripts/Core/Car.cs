using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
    public enum Status
    {
        DRIVING,
        WAITING_ON_RED,
        WAITING_ON_CAR,
    }

    public float stopDistance = 20.0f;
    public float stopGap = 3.0f;

    public float maxSpeed = 25.0f;

    private Vector3 bumperOffset;
    private Vector3 bumperPosition;
    private Vector3 velocity = Vector3.zero;

    public float rightTurnSpeed = 2.0f;
    public float leftTurnSpeed = 5.0f;
    public float acceleration = 0.05f;
    public float deceleration = 0.10f;

    public Status status { get; private set; }

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
        bumperPosition = transform.position + transform.TransformVector(bumperOffset);

        CheckObstacles();
        UpdateMovement();
    }

    private void CheckObstacles()
    {

        status = Status.DRIVING;

        if (Physics.Raycast(bumperPosition, transform.forward, out RaycastHit hit, stopDistance))
        {
            if (hit.collider.CompareTag("Junction"))
            {
                Junction junction = simulation.junctions.Find(j => j.obj == hit.collider.gameObject);

                if (junction.type == Junction.Type.Lights)
                {
                    TrafficLight.Status trafficLightStatus = simulation.GetTrafficLightStatus(this, junction);
                    if (trafficLightStatus == TrafficLight.Status.Red)
                        status = Status.WAITING_ON_RED;
                }
                else if (junction.type == Junction.Type.Stops)
                {

                }
            }

            else if (hit.collider.CompareTag("Car"))
            {
                Car hitCar = hit.collider.gameObject.GetComponent<Car>();
                
                status = Status.WAITING_ON_CAR;

                if (hitCar.status == Status.WAITING_ON_RED)
                    status = Status.WAITING_ON_RED;
            }
        }
    }

    private void UpdateMovement()
    {
        if (pathIndex >= path.Count)
        {
            simulation.currentCars--;
            PlayEffect();
            Destroy(gameObject);
            return;
        }

        Vector3 target = path[pathIndex];
        Vector3 direction = (target - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target);

        float targetSpeed = maxSpeed;
        if (status != Status.DRIVING)
            targetSpeed = 0.0f;
        else if (pathIndex + 2 < path.Count && Vector3.Dot(transform.forward, path[pathIndex + 2] - path[pathIndex + 1]) <= 0.02f)  // entering turn
        {
            if (Vector3.Dot(transform.right, path[pathIndex + 1] - path[pathIndex]) > 0.02f)
                targetSpeed = rightTurnSpeed;
            else
                targetSpeed = leftTurnSpeed;
        }

        // Smoothly adjust velocity
        float speedDifference = targetSpeed - velocity.magnitude;
        float accelerationRate = speedDifference > 0 ? acceleration : deceleration;
        velocity = transform.forward * velocity.magnitude;
        velocity += direction * Mathf.Clamp(speedDifference, -accelerationRate * Time.deltaTime, accelerationRate * Time.deltaTime);

        // Move car
        transform.position += velocity * Time.deltaTime;
            
        // Rotate towards target
        if (distanceToTarget > 0.05f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10.0f * Time.deltaTime);
        }

        // Update path progress
        if (distanceToTarget < 2.5f)
            pathIndex++;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.tag == "Junction")
        {
            status = Status.DRIVING;
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
