using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;


[System.Serializable]
public class ClockData
{
    public long timeTicks;
    public float timeScale;
    public bool isPaused;

    public ClockData(Clock clock)
    {
        this.timeTicks = clock.datetime.Ticks;
        this.timeScale = clock.timeScale;
        this.isPaused = clock.isPaused;
    }
}