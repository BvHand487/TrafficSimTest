using UnityEngine;

public class TimeScaleButtons : MonoBehaviour
{
    private Clock clock;

    private void Start()
    {
        clock = Clock.Instance;
    }

    public void SetTimeScale(GameObject buttonObj)
    {
        int timeScale = int.Parse(buttonObj.name);
        clock.timeScale = timeScale;
    }
}
