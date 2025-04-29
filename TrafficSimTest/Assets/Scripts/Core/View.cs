using UnityEngine;

namespace Core
{
    public class View : MonoBehaviour
    {
        private Camera cam;

        public float panSpeed;
        private Vector3 panOrigin;
        private Vector3 posOrigin;
    
        public float zoomSpeed;
        public float minCameraY = 300f;
        public float maxCameraY = 300f;

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        void Update()
        {
            HandlePan();
            HandleZoom();
        }
    
        private void HandlePan()
        {
            if (Input.GetKeyDown(KeyCode.Mouse2))
            {
                panOrigin = Input.mousePosition;
                posOrigin = transform.position;
                return;
            }

            else if (Input.GetKey(KeyCode.Mouse2))
            {
                Vector3 pos = cam.ScreenToViewportPoint(panOrigin - Input.mousePosition);
                Vector3 move = panSpeed * (transform.position.y / minCameraY) * new Vector3(pos.x, 0, pos.y);
                transform.position = posOrigin + move;
            }
        }

        private void HandleZoom()
        {
            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");

            if (scrollWheel != 0f)
            {
                transform.position = new Vector3(
                    transform.position.x,
                    Mathf.Clamp(transform.position.y - scrollWheel * zoomSpeed * Time.unscaledDeltaTime, minCameraY, maxCameraY),
                    transform.position.z
                );
            }
        }
    }
}
