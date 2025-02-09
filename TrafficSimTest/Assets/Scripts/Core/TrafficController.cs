using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrafficController : MonoBehaviour
{
    public enum Mode
    {
        Single,
        Double,
    }

    public Mode mode;

    private bool switchMode = false;
    private Mode newMode;

    public Junction junction;
    public TrafficLightAgent agent;
    public List<TrafficLight> lights;

    public float elapsedTime = 0.0f;
    private int activeLight = 0;

    public static readonly float defaultGreenInterval = 20f;

    public void Awake()
    {
        junction = GetComponentInParent<Junction>();
        agent = GetComponent<TrafficLightAgent>();
        lights = GetComponentsInChildren<TrafficLight>().ToList();
        
        mode = Mode.Double;
    }

    public void Start()
    {
        for (int i = 0; i < lights.Count; ++i)
        {
            Road closestRoad = null;
            float closestDistance = float.MaxValue;

            foreach (var road in junction.roads)
            {
                var closestRoadPoint = Utils.Math.GetClosestVector(junction.transform.position, road.path);
                var dist = Vector3.Distance(closestRoadPoint, lights[i].transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestRoad = road;
                }    
            }

            lights[i].road = closestRoad;
            lights[i].roadDirection = Utils.Math.GetClosestVector(junction.transform.position, closestRoad.path) - junction.transform.position;
            lights[i].roadDirection.Normalize();
        }

        ResetLights();
    }

    //public void Update()
    //{
    //    // keybind to switch the traffic mode across the whole simulation - for debugging purposes
    //    //if (Input.GetKeyDown(KeyCode.M))
    //    //{
    //    //    switchMode = true;

    //    //    if (mode == Mode.Single)
    //    //        newMode = Mode.Double;
    //    //    else
    //    //        newMode = Mode.Single;
    //    //}

    //    elapsedTime += Time.deltaTime;

    //    switch (mode)
    //    {
    //        case Mode.Single:
    //            {
    //                TrafficLight current = lights[activeLight];
    //                UpdateSingleMode(current);
    //                break;
    //            }

    //        case Mode.Double:
    //            {
    //                TrafficLight current = lights[activeLight];
    //                TrafficLight opposite = lights[(activeLight + 2) % lights.Count];

    //                if (lights.Count == 3)
    //                {
    //                    if (activeLight == 0)
    //                        opposite = lights[2];

    //                    if (activeLight == 2)
    //                        opposite = lights[0];

    //                    if (activeLight == 1)
    //                    {
    //                        UpdateSingleMode(current);
    //                        return;
    //                    }
    //                }

    //                UpdateDoubleMode(current, opposite);
    //                break;
    //            }
    //    }
    //}

    public int currentPhase;

    // phase = 0 or 1
    public void SetLights(int phase)
    {
        currentPhase = phase;

        if (lights.Count == 4)
        {
            lights[phase + 1].status = TrafficLight.Status.Red;
            lights[(phase + 3) % 4].status = TrafficLight.Status.Red;
            lights[phase].status = TrafficLight.Status.Green;
            lights[phase + 2].status = TrafficLight.Status.Green;
        }
        else if (lights.Count == 3)
        {
            if (phase == 1)
            {
                lights[0].status = TrafficLight.Status.Red;
                lights[1].status = TrafficLight.Status.Green;
                lights[2].status = TrafficLight.Status.Red;
            }
            else
            {
                lights[0].status = TrafficLight.Status.Green;
                lights[1].status = TrafficLight.Status.Red;
                lights[2].status = TrafficLight.Status.Green;
            }
        }
    }

    public void Switch()
    {
        SetLights(1 - currentPhase);
    }

    public bool IsGreenOver(TrafficLight light) => (light.status == TrafficLight.Status.Green && elapsedTime > light.greenInterval);
    public bool IsYellowOver(TrafficLight light) => (light.status == TrafficLight.Status.Yellow && elapsedTime > TrafficLight.yellowInterval);
    public bool IsRedOver(TrafficLight light) => (light.status == TrafficLight.Status.Red && elapsedTime > TrafficLight.redIntervalBuffer);

    public void UpdateSingleMode(TrafficLight current)
    {
        if (IsGreenOver(current))
        {
            current.status = TrafficLight.Status.Yellow;
            elapsedTime = 0.0f;
        }
        else if (IsYellowOver(current))
        {
            current.status = TrafficLight.Status.Red;

            if (switchMode)
            {
                mode = newMode;
                switchMode = false;
            }

            elapsedTime = 0.0f;
        }
        else if (IsRedOver(current))
        {
            activeLight = (activeLight + 1) % lights.Count;

            if (lights.Count == 3 && activeLight == 2 && mode == Mode.Double)
            {
                lights[0].status = TrafficLight.Status.Green;
            }

            lights[activeLight].status = TrafficLight.Status.Green;
            elapsedTime = 0.0f;
        }
    }

    public void UpdateDoubleMode(TrafficLight current, TrafficLight opposite)
    {
        if (IsGreenOver(current))
        {
            current.status = TrafficLight.Status.Yellow;
            opposite.status = TrafficLight.Status.Yellow;
            elapsedTime = 0.0f;
        }
        else if (IsYellowOver(current))
        {
            current.status = TrafficLight.Status.Red;
            opposite.status = TrafficLight.Status.Red;

            if (switchMode)
            {
                mode = newMode;
                switchMode = false;
            }

            elapsedTime = 0.0f;
        }
        else if (IsRedOver(current))
        {
            if (lights.Count == 3)
            {
                if (activeLight == 0 || activeLight == 2)
                    activeLight = 1;
                else
                    activeLight = 0;
            }
            else
            {
                activeLight = (activeLight + 1) % lights.Count;
                lights[(activeLight + 2) % lights.Count].status = TrafficLight.Status.Green;
            }

            lights[activeLight].status = TrafficLight.Status.Green;
            elapsedTime = 0.0f;
        }
    }

    /*
     * Acts as an interface to the ML agents - verifies the configuration is valid and then sets the traffic lights intervals
     */
    public bool ConfigureLights(List<float> greenIntervals, Mode mode)
    {
        if (lights.Count == 3 && greenIntervals.Count == 4)
            greenIntervals.RemoveAt(greenIntervals.Count - 1);

        for (int i = 0; i < (int) (lights.Count / 2f); ++i)
            if (!Utils.Math.CompareFloat(greenIntervals[i], greenIntervals[i + 2]))
                return false;

        for (int i = 0; i < lights.Count; ++i)
        {
            if (greenIntervals[i] < TrafficLight.minGreenInterval &&
                greenIntervals[i] > TrafficLight.maxGreenInterval)
                return false;

            lights[i].ConfigureInterval(greenIntervals[i]);
        }

        if (this.mode != mode)
        {
            switchMode = true;
            newMode = mode;
        }

        return true;
    }

    public void ResetLights()
    {
        //    ConfigureLights(new List<float> { defaultGreenInterval, defaultGreenInterval, defaultGreenInterval, defaultGreenInterval }, Mode.Double);

        SetLights(0);
    }
}
