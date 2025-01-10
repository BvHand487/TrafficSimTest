using Generation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class Selection : MonoBehaviour
{
    private Camera cam;
    private Simulation simulation;

    private LineRenderer carPathRenderer;
    private int carPathLength;

    public Material outlineMaterial;
    public Material selectionMaterial;

    private Car selectedCar;
    // private MeshRenderer carFromBuildingRenderer = default;
    // private MeshRenderer carToBuildingRenderer = default;
    private Junction selectedJunction;


    void Start()
    {
        simulation = FindObjectOfType<Simulation>().GetComponent<Simulation>();
        cam = GetComponent<Camera>();
        carPathRenderer = GetComponent<LineRenderer>();
    }


    void Update()
    {
        HandleSelection();
    }

    private void HandleSelection()
    {
        if (selectedCar ?? null)
        {
            carPathRenderer.SetPosition(0, selectedCar.transform.position);

            if (selectedCar.carPath.Length() != carPathLength)
            {
                carPathRenderer.positionCount = carPathLength;
                carPathLength--;
                UpdateLineRenderer();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            UnselectAll();

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            UnselectAll();

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Car"))
                    SelectCar(hit.collider.gameObject.GetComponent<Car>());

                if (hit.collider.CompareTag("Junction"))
                {
                    Vector3 offset = 0.01f * ray.direction.normalized;
                    if (Physics.Raycast(hit.point + offset, ray.direction, out RaycastHit repeatedHit))
                    {
                        if (repeatedHit.collider.CompareTag("Car"))
                            SelectCar(repeatedHit.collider.gameObject.GetComponent<Car>());
                        else
                            selectedJunction = simulation.junctionsDict[hit.collider.gameObject];
                    }
                }
            }
        }
    }

    void UnselectAll()
    {
        if (selectedCar != null)
        {
            var meshRenderer = selectedCar.carPath.from.obj.GetComponentInChildren<MeshRenderer>();
            meshRenderer.materials = new Material[] { meshRenderer.materials[0] };

            meshRenderer = selectedCar.carPath.to.obj.GetComponentInChildren<MeshRenderer>();
            meshRenderer.materials = new Material[] { meshRenderer.materials[0] };
        }
        selectedCar = null;
        carPathRenderer.positionCount = 0;

        selectedJunction = null;
    }

    void SelectCar(Car car)
    {
        selectedCar = car;
        carPathLength = selectedCar.carPath.Length();
        carPathRenderer.positionCount = carPathLength + 1;

        UpdateLineRenderer();

        SelectBuilding(selectedCar.carPath.from);
        SelectBuilding(selectedCar.carPath.to);
    }

    void SelectBuilding(Building building)
    {
        var meshRenderer = building.obj.GetComponentInChildren<MeshRenderer>();

        List<Material> mats = meshRenderer.materials.ToList();

        if (mats.Count == 1)
            mats.Add(outlineMaterial);

        meshRenderer.materials = mats.ToArray();
    }

    void UpdateLineRenderer()
    {
        List<Vector3> path = new List<Vector3>();
        path.Add(selectedCar.transform.position);
        path.AddRange(selectedCar.carPath.points);
        for (int i = 0; i < path.Count; ++i)
            path[i] += 0.5f * Vector3.up;
        carPathRenderer.SetPositions(path.ToArray());
    }
}
