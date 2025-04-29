using Core;

namespace Persistence
{
    [System.Serializable]
    public class ViewData
    {
        public float[] pos;

        public ViewData(View view)
        {
            this.pos = new float[3] {
                view.transform.position.x,
                view.transform.position.y,
                view.transform.position.z,
            };
        }
    }
}