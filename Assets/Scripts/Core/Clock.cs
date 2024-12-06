using System;
using UnityEngine;


// Singleton class representing time in the simulation
public class Clock
{
    private static Clock _instance = null;
    public static Clock Instance
    {
        get
        {
            if (_instance == null)
                _instance = new Clock();
            return _instance;
        }
    }

    public DateTime datetime { get; private set; }
    private static readonly float clockRatio = 24;

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

    private Clock()
    {
        datetime = DateTime.Now;
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

    public string GetFormattedDatetime(string format)
    {
        return datetime.ToString(format, new System.Globalization.CultureInfo("en-US"));
    }
}
