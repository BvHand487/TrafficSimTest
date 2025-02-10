using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Vehicle Preset", menuName = "Scriptables/Vehicle Preset", order = 1)]
public class VehiclePreset : ScriptableObject
{
    /*
     * Vehicle settings
     */

    public GameObject prefab;

    public float maxVelocity = 25.0f;

    public float accelerationRate = 5.0f;
    public float decelerationRate = 10.0f;

    public float lookAheadDistance = 20.0f;
    public float stoppedGapDistance = 3.0f;
}