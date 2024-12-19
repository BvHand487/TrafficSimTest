using UnityEngine;

public class View : MonoBehaviour
{
    public float panSpeed;

    public float zoomSpeed;
    public float minCameraY = 300f;
    public float maxCameraY = 300f;

    private Camera cam;

    private Vector3 panOrigin;
    private Vector3 posOrigin;

    void Start()
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
        if (Input.GetMouseButtonDown(0))
        {
            panOrigin = Input.mousePosition;
            posOrigin = transform.position;
            return;
        }

        else if (Input.GetMouseButton(0))
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
