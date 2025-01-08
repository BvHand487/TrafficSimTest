using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class View : MonoBehaviour
{
    private Camera cam;
    private Simulation simulation;
    private LineRenderer carPathRenderer;
    private int carPathLength;
    private Material buildingMaterial;
    private Material selectionMaterial;

    public float panSpeed;
    private Vector3 panOrigin;
    private Vector3 posOrigin;
    
    public float zoomSpeed;
    public float minCameraY = 300f;
    public float maxCameraY = 300f;

    private Car selectedCar;
    private MeshRenderer carFromBuildingRenderer = default;
    private MeshRenderer carToBuildingRenderer = default;
    private Junction selectedJunction;

    void Start()
    {
        simulation = FindObjectOfType<Simulation>().GetComponent<Simulation>();
        cam = GetComponent<Camera>();
        carPathRenderer = GetComponent<LineRenderer>();

        buildingMaterial = carPathRenderer.material;
        selectionMaterial = carPathRenderer.material;
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
                for (int i = 0; i < path.Count; ++i)
                    path[i] += 0.35f * Vector3.up;
                carPathRenderer.SetPositions(path.ToArray());
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (carFromBuildingRenderer != null && carToBuildingRenderer != null)
            {
                carFromBuildingRenderer.material = buildingMaterial;
                carToBuildingRenderer.material = buildingMaterial;
            }
            selectedCar = null;
            carPathRenderer.positionCount = 0; 

            selectedJunction = null;
        }


        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (carFromBuildingRenderer != null && carToBuildingRenderer != null)
            {
                carFromBuildingRenderer.material = buildingMaterial;
                carToBuildingRenderer.material = buildingMaterial;
            }
            selectedCar = null;
            carPathRenderer.positionCount = 0;

            selectedJunction = null;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Junction"))
                {
                    selectedJunction = simulation.junctionsDict[hit.collider.gameObject];
                }

                if (hit.collider.CompareTag("Car"))
                {
                    selectedCar = hit.collider.gameObject.GetComponent<Car>();

                    carPathLength = selectedCar.carPath.Length();
                    carPathRenderer.positionCount = carPathLength + 1;

                    List<Vector3> path = new List<Vector3>();
                    path.Add(selectedCar.transform.position);
                    path.AddRange(selectedCar.carPath.points);
                    for (int i = 0; i < path.Count; ++i)
                        path[i] += 0.35f * Vector3.up;
                    carPathRenderer.SetPositions(path.ToArray());

                    carFromBuildingRenderer = selectedCar.carPath.from.obj.GetComponentInChildren<MeshRenderer>();
                    carToBuildingRenderer = selectedCar.carPath.to.obj.GetComponentInChildren<MeshRenderer>();
                    carFromBuildingRenderer.material = selectionMaterial;
                    carToBuildingRenderer.material = selectionMaterial;
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
