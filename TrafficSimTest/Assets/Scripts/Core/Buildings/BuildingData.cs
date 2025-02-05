using UnityEngine;


[System.Serializable]
public class BuildingData
{
    public string typeName;
    public float[] pos;
    public string prefabPath;

    public BuildingData(Building building)
    {
        this.typeName = building.type.ToString();

        this.pos = new float[3] {
            building.transform.position.x,
            building.transform.position.y,
            building.transform.position.z
        };

        this.prefabPath = $"Prefabs/{GameManager.Instance.buildingPrefab.name}";
    }
}