using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName="Lighting Preset", menuName ="Scriptables/Lighting Preset", order=1)]
public class LightPreset : ScriptableObject
{
    public Gradient ambientColor;
    public Gradient directionalColor;
    public Gradient fogColor;
}
