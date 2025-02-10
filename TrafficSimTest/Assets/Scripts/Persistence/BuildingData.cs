[System.Serializable]
public class BuildingData
{
    public string typeName;
    public float[] pos;
    public float height;
    public string prefabPath;

    public BuildingData(Building building)
    {
        this.typeName = building.type.ToString();

        this.pos = new float[3] {
            building.transform.position.x,
            building.transform.position.y,
            building.transform.position.z
        };

        this.height = building.transform.localScale.y;

        this.prefabPath = $"Prefabs/{GameManager.Instance.buildingPrefab.name}";
    }
}