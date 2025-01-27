using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class Selection : MonoBehaviour
{
    private Camera cam;

    private Vehicle selectedVehicle;
    private LineRenderer lineRenderer;
    private int pathLength;

    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Material selectionMaterial;
    [SerializeField] private Tooltip tooltip;

    private Tooltip vehicleTooltip;
    private Tooltip junctionTooltip;
    private List<Tooltip> lightTooltips;

    private Junction selectedJunction;
    private BoxCollider selectedJunctionCollider;


    void Start()
    {
        cam = Camera.main;
        lineRenderer = GetComponent<LineRenderer>();

        lightTooltips = new List<Tooltip>();
    }


    void Update()
    {
        HandleSelection();
    }

    private void HandleSelection()
    {
        if (selectedVehicle)
        {
            if (selectedVehicle.path.Length() <= 1)
            {
                UnselectAll();
            }
            else
            {
                lineRenderer.SetPosition(0, selectedVehicle.transform.position);

                vehicleTooltip.SetText($"Time waiting: {selectedVehicle.timeWaiting.ToString("0.00")}");

                if (selectedVehicle.path.Length() != pathLength)
                {
                    lineRenderer.positionCount = pathLength;
                    pathLength--;
                    UpdateVehicleLineRenderer();
                }
            }
        }

        if (selectedJunction)
        {
            junctionTooltip.SetText($"Elapsed Time: {selectedJunction.trafficController.elapsedTime.ToString("0.00")}");

            for (int i = 0; i < selectedJunction.trafficController.lights.Count; ++i)
            {
                TrafficLight light = selectedJunction.trafficController.lights[i];
                lightTooltips[i].SetText($"Green Interval: {light.greenInterval.ToString("0.00")}\nQueue: {light.vehicleQueue.Count}");
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
                {
                    var vehicle = hit.collider.gameObject.GetComponent<Vehicle>();
                    SelectVehicle(vehicle);
                }

                if (hit.collider.CompareTag("Junction"))
                {
                    Vector3 offset = 0.01f * ray.direction.normalized;
                    if (Physics.Raycast(hit.point + offset, ray.direction, out RaycastHit repeatedHit))
                    {
                        if (repeatedHit.collider.CompareTag("Vehicle"))
                        {
                            var vehicle = repeatedHit.collider.gameObject.GetComponent<Vehicle>();
                            SelectVehicle(vehicle);
                        }
                        else
                        {
                            var junction = hit.collider.gameObject.GetComponent<Junction>();
                            SelectJunction(junction);
                        }
                    }
                }
            }
        }
    }

    void UnselectAll()
    {
        if (selectedVehicle != null)
        {
            var meshRenderer = selectedVehicle.path.from.GetComponentInChildren<MeshRenderer>();
            meshRenderer.materials = new Material[] { meshRenderer.materials[0] };

            meshRenderer = selectedVehicle.path.to.GetComponentInChildren<MeshRenderer>();
            meshRenderer.materials = new Material[] { meshRenderer.materials[0] };
        }
        selectedVehicle = null;
        lineRenderer.positionCount = 0;
        lineRenderer.loop = false;

        if (vehicleTooltip != null)
        {
            Destroy(vehicleTooltip.gameObject);
            vehicleTooltip = null;
        }


        selectedJunction = null;

        if (junctionTooltip != null)
        {
            Destroy(junctionTooltip.gameObject);
            junctionTooltip = null;

            if (lightTooltips != null)
            {
                foreach (var t in lightTooltips)
                    Destroy(t.gameObject);

                lightTooltips.Clear();
            }
        }
    }

    void SelectVehicle(Vehicle vehicle)
    {
        selectedVehicle = vehicle;
        pathLength = selectedVehicle.path.Length();
        lineRenderer.positionCount = pathLength + 1;

        vehicleTooltip = Instantiate(tooltip, selectedVehicle.transform.position, Quaternion.identity);
        vehicleTooltip.Initialize(selectedVehicle.transform, new Vector3(0.4f, 5f, 0.4f));

        UpdateVehicleLineRenderer();
        SelectBuilding(selectedVehicle.path.from);
        SelectBuilding(selectedVehicle.path.to);
    }

    void SelectBuilding(Building building)
    {
        var meshRenderer = building.GetComponentInChildren<MeshRenderer>();

        List<Material> mats = meshRenderer.materials.ToList();

        if (mats.Count == 1)
            mats.Add(outlineMaterial);

        meshRenderer.materials = mats.ToArray();
    }

    void UpdateVehicleLineRenderer()
    {
        for (int i = selectedVehicle.path.currentPointIndex; i < selectedVehicle.path.points.Count; ++i)
            lineRenderer.SetPosition(i - selectedVehicle.path.currentPointIndex + 1, selectedVehicle.path.points[i] + 0.05f * Vector3.up);
    }

    void SetJunctionLineRenderer()
    {
        Vector3[] corners = new Vector3[4];

        Bounds bounds = selectedJunctionCollider.bounds;
        corners[0] = new Vector3(bounds.min.x, bounds.min.y + 0.05f, bounds.min.z);
        corners[1] = new Vector3(bounds.min.x, bounds.min.y + 0.05f, bounds.max.z);
        corners[2] = new Vector3(bounds.max.x, bounds.min.y + 0.05f, bounds.max.z);
        corners[3] = new Vector3(bounds.max.x, bounds.min.y + 0.05f, bounds.min.z);

        lineRenderer.positionCount = 4;
        lineRenderer.SetPositions(corners);
        lineRenderer.loop = true;
    }

    void SelectJunction(Junction junction)
    {
        selectedJunction = junction;
        selectedJunctionCollider = junction.GetComponent<BoxCollider>();

        SetJunctionLineRenderer();

        junctionTooltip = Instantiate(tooltip, selectedJunction.transform.position, Quaternion.identity);
        junctionTooltip.Initialize(selectedJunction.transform, new Vector3(-0.2f, 5f, 0f), true);

        for (int i = 0; i < junction.trafficController.lights.Count; ++i)
        {
            TrafficLight light = junction.trafficController.lights[i];
            Vector3 customOffset = 7.5f * light.roadDirection;

            var lightTooltip = Instantiate(tooltip, light.transform.position, Quaternion.identity);
            lightTooltip.Initialize(light.transform, customOffset + 5f * Vector3.up, true);

            lightTooltips.Add(lightTooltip);
        }
    }
}
