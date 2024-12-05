using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
