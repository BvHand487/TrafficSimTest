using Core;

namespace Persistence
{
    [System.Serializable]
    public class JunctionData
    {
        public string typeName;
        public float[] pos;
        public float rotY;
        public string prefabPath;

        public JunctionData(Junction junction)
        {
            this.typeName = junction.type.ToString();

            this.pos = new float[3] { junction.transform.position.x, junction.transform.position.y, junction.transform.position.z };
            this.rotY = junction.transform.eulerAngles.y;

            this.prefabPath = $"Prefabs/{junction.gameObject.name}";
        }
    }
}   