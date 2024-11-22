using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class View : MonoBehaviour
{
    public float panSpeed;
    public float moveSpeed;
    public float zoomSpeed;

    private Camera cam;

    private Vector3 panOrigin;
    private Vector3 posOrigin;

    private float zoomFov;
    private float minZoomFov = 16.0f;
    private float maxZoomFov = 100.0f;

    void Start()
    {
        cam = GetComponent<Camera>();

        zoomFov = cam.fieldOfView;
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
            Vector3 move = panSpeed * (zoomFov / minZoomFov) * new Vector3(pos.x, 0, pos.y);
            transform.position = posOrigin + move;
        }
    }

    private void HandleZoom()
    {
        if (Input.GetAxis("Mouse ScrollWheel") > 0.0f && zoomFov > minZoomFov)
            zoomFov -= zoomSpeed;
        else if (Input.GetAxis("Mouse ScrollWheel") < 0.0f && zoomFov < maxZoomFov)
            zoomFov += zoomSpeed;

        cam.fieldOfView = Mathf.MoveTowards(cam.fieldOfView, zoomFov, 1000 * zoomSpeed * Time.unscaledDeltaTime);
    }
}
