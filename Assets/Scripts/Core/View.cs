using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class View : MonoBehaviour
{
    private Simulation simulation;
    private Camera cam;
    private LineRenderer carPathRenderer;
    private int carPathLength;

    public float panSpeed;
    private Vector3 panOrigin;
    private Vector3 posOrigin;
    
    public float zoomSpeed;
    public float minCameraY = 300f;
    public float maxCameraY = 300f;

    private Car selectedCar;
    private Junction selectedJunction;

    void Start()
    {
        simulation = FindObjectOfType<Simulation>().GetComponent<Simulation>();
        cam = GetComponent<Camera>();
        carPathRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        HandlePan();
        HandleZoom();
        HandleSelection();
    }

    private void HandleSelection()
    {
        if (selectedCar ?? null)
        {
            carPathRenderer.SetPosition(0, selectedCar.transform.position);

            if (selectedCar.carPath.Length() != carPathLength)
            {
                carPathLength--;
                carPathRenderer.positionCount = carPathLength + 1;

                List<Vector3> path = new List<Vector3>();
                path.Add(selectedCar.transform.position);
                path.AddRange(selectedCar.carPath.points);
                carPathRenderer.SetPositions(path.ToArray());
            }
        }

        if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            selectedCar = null;
            carPathRenderer.positionCount = 0;

            selectedJunction = null;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Car"))
                {
                    selectedCar = hit.collider.gameObject.GetComponent<Car>();

                    carPathLength = selectedCar.carPath.Length();
                    carPathRenderer.positionCount = carPathLength + 1;

                    List<Vector3> path = new List<Vector3>();
                    path.Add(selectedCar.transform.position);
                    path.AddRange(selectedCar.carPath.points);
                    carPathRenderer.SetPositions(path.ToArray());
                }
                else if (hit.collider.CompareTag("Junction"))
                {
                    selectedJunction = simulation.junctionsDict[hit.collider.gameObject];
                }
            }
        }
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
