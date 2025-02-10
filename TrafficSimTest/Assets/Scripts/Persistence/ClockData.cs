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