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

    private LineRenderer pathRenderer;
    private int pathLength;

    public Material outlineMaterial;
    public Material selectionMaterial;

    private Vehicle selectedVehicle;
    // private MeshRenderer vehicleFromBuildingRenderer = default;
    // private MeshRenderer vehicleToBuildingRenderer = default;
    //private Junction selectedJunction;


    void Start()
    {
        cam = GetComponent<Camera>();
        pathRenderer = GetComponent<LineRenderer>();
    }


    void Update()
    {
        HandleSelection();
    }

    private void HandleSelection()
    {
        if (selectedVehicle ?? null)
        {
            pathRenderer.SetPosition(0, selectedVehicle.transform.position);

            if (selectedVehicle.path.Length() != pathLength)
            {
                pathRenderer.positionCount = pathLength;
                pathLength--;
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
                if (hit.collider.CompareTag("Vehicle"))
                    Selectvehicle(hit.collider.gameObject.GetComponent<Vehicle>());

                if (hit.collider.CompareTag("Junction"))
                {
                    Vector3 offset = 0.01f * ray.direction.normalized;
                    if (Physics.Raycast(hit.point + offset, ray.direction, out RaycastHit repeatedHit))
                    {
                        if (repeatedHit.collider.CompareTag("Vehicle"))
                            Selectvehicle(repeatedHit.collider.gameObject.GetComponent<Vehicle>());
                    }
                }
            }
        }
    }

    void UnselectAll()
    {
        if (selectedVehicle != null)
        {
            var meshRenderer = selectedVehicle.path.from.obj.GetComponentInChildren<MeshRenderer>();
            meshRenderer.materials = new Material[] { meshRenderer.materials[0] };

            meshRenderer = selectedVehicle.path.to.obj.GetComponentInChildren<MeshRenderer>();
            meshRenderer.materials = new Material[] { meshRenderer.materials[0] };
        }
        selectedVehicle = null;
        pathRenderer.positionCount = 0;

        //selectedJunction = null;
    }

    void Selectvehicle(Vehicle vehicle)
    {
        selectedVehicle = vehicle;
        pathLength = selectedVehicle.path.Length();
        pathRenderer.positionCount = pathLength + 1;

        UpdateLineRenderer();

        SelectBuilding(selectedVehicle.path.from);
        SelectBuilding(selectedVehicle.path.to);
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
        path.Add(selectedVehicle.transform.position);
        path.AddRange(selectedVehicle.path.points);
        for (int i = 0; i < path.Count; ++i)
            path[i] += 0.5f * Vector3.up;
        pathRenderer.SetPositions(path.ToArray());
    }
}
