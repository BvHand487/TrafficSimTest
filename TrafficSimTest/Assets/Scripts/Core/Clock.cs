using System;
using UnityEngine;


// Singleton class representing time in the simulation
public class Clock : SingletonMonobehaviour<Clock>
{
    public DateTime datetime { get; set; }
    public bool isPaused = false;
    private static readonly float clockRatio = 1f;

    private float effectiveTimeScale;
    public float timeScale
    {
        get
        {
            return effectiveTimeScale;
        }
        set
        {
            if (!isPaused)
            {
                Time.timeScale = value;
                effectiveTimeScale = value;
            }
            else
                effectiveTimeScale = value;
        }
    }

    public override void Awake()
    {
        base.Awake();

        datetime = DateTime.Today;
        timeScale = 1.0f;
    }

    public void Update()
    {
        datetime = datetime.AddSeconds(clockRatio * Time.deltaTime);
    }

    public void SetPaused(bool isPaused)
    {
        this.isPaused = isPaused;

        if (isPaused)
            Time.timeScale = 0f;
        else
            Time.timeScale = effectiveTimeScale;
    }

    public float GetFractionOfDay()
    {
        return (datetime.Hour + datetime.Minute / 60f) / 24f;
    }
}
