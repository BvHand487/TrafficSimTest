using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public float accelerationRate = 5.0f;
    public float decelerationRate = 10.0f;

    private float velocity = 0.0f;
    private float acceleration = 0.0f;
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

        if (canStop)
            Check();

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
                    Junction junction = simulation.junctionsDict[hit.collider.gameObject];

                    switch (junction.type)
                    {
                        case Junction.Type.Lights:
                            TrafficLight.Status trafficLightStatus = simulation.GetTrafficLightStatus(this, junction);
                            if (trafficLightStatus != TrafficLight.Status.Green)
                            {
                                status = Status.STOPPING;

                                float timeToStop = velocity / decelerationRate;
                                float stoppingDistance = velocity * timeToStop - 0.5f * decelerationRate * timeToStop * timeToStop;

                                if (stoppingDistance >= trueDistance)
                                    acceleration = -decelerationRate;
                                else
                                    acceleration = -Mathf.Clamp((velocity * velocity) / (2 * trueDistance), 0, decelerationRate);

                                if (IsStopped())
                                    status = Status.WAITING_RED;
                            }
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
                        acceleration = -decelerationRate;

                        if (IsStopped() && (hitCar.status == Status.WAITING_RED || hitCar.status == Status.WAITING_CAR))
                            status = hitCar.status;
                    }

                    if (IsStopped() && hitCar.IsStopped() && trueDistance < 0.0f)
                    {
                        Car carWithPriority = transform.position.x > hitCar.transform.position.x ? this : hitCar;
                        carWithPriority.status = Status.DRIVING;
                    }
                    break;
            }
        }
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
                transform.position += direction * velocity * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * deltaTime);
                distanceThisFrame = 0;
            }
        }
    }

    private bool IsStopped()
    {
        return Mathf.Abs(velocity) < 0.01f;
    }

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
        if (carPath.points != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLineStrip(carPath.points.ToArray(), false);

            foreach (var p in carPath.points)
                Gizmos.DrawSphere(p, 0.2f);
        }
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