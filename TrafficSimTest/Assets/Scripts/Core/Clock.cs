using System;
using UnityEngine;


// Singleton class representing time in the simulation
public class Clock : SingletonMonobehaviour<Clock>
{
    public DateTime datetime { get; private set; }
    private static readonly float clockRatio = 4 * 24;

    public float timeScale
    {
        get => Time.timeScale;
        set
        {
            if (Time.timeScale != 0f)
            {
                prevTimeScale = Time.timeScale;
                Time.timeScale = value;
            }
        }
    }
    private float prevTimeScale;

    public override void Awake()
    {
        base.Awake();

        datetime = DateTime.Today;
        prevTimeScale = 1.0f;
        timeScale = 1.0f;
    }

    public void Update()
    {
        datetime = datetime.AddSeconds(clockRatio * Time.deltaTime);
    }

    public void SetPaused(bool isPaused)
    {
        if (isPaused && timeScale != 0f)
        {
            timeScale = 0f;
            return;
        }
        
        if (!isPaused && timeScale == 0f)
            Time.timeScale = prevTimeScale;
    }

    public float GetFractionOfDay()
    {
        return (datetime.Hour + datetime.Minute / 60f) / 24f;
    }
}
