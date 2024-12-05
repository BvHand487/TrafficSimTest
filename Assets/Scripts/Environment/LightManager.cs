using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LightManager : MonoBehaviour
{
    [SerializeField]
    private Light sun;

    [SerializeField]
    private LightPreset preset;


    // Updates lighting using the gradients defined in the scriptable object
    private void UpdateLighting(float timePercent)
    {
        RenderSettings.ambientLight = preset.ambientColor.Evaluate(timePercent);
        RenderSettings.fogColor = preset.fogColor.Evaluate(timePercent);

        sun.color = preset.directionalColor.Evaluate(timePercent);
        sun.gameObject.transform.rotation = Quaternion.Euler(new Vector3((timePercent * 360f) - 90f, 170f, 0f));
    }

    private void Update()
    {
        var simulationTime = Clock.Instance.datetime;
        int timeOfDayInMinutes = simulationTime.Hour * 60 + simulationTime.Minute;
        UpdateLighting(timeOfDayInMinutes / (24f * 60f));
    }
}
