using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Core.Buildings;
using Core.Vehicles;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Environment
{
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
        
        [SerializeField] private float popAmount = 1.5f;
        [SerializeField] private float popDuration = 0.35f;
        private static readonly int OutlineThicknessID = Shader.PropertyToID("_Outline_Thickness");
        private static float startingOutlineThickness;
        private static float endingOutlineThickness;
        private static float startingLineThickness;
        private static float endingLineThickness;
        private Sequence outlinePop;
        private Sequence linePop;
        
        void Start()
        {
            cam = Camera.main;
            lineRenderer = GetComponent<LineRenderer>();

            lightTooltips = new List<Tooltip>();
            
            if (outlineMaterial != null)
            {
                startingOutlineThickness = outlineMaterial.GetFloat(OutlineThicknessID);
                endingOutlineThickness = popAmount * startingOutlineThickness;
                
                outlinePop =
                    DOTween.Sequence()
                        .Append(DOTween.To(
                            () => outlineMaterial.GetFloat(OutlineThicknessID),
                            x => outlineMaterial.SetFloat(OutlineThicknessID, x),
                            endingOutlineThickness,
                            popDuration / 2f
                        ).SetEase(Ease.OutCubic)) // pop up
                        .Append(DOTween.To(
                            () => outlineMaterial.GetFloat(OutlineThicknessID),
                            x => outlineMaterial.SetFloat(OutlineThicknessID, x),
                            startingOutlineThickness,
                            popDuration / 2f
                        ).SetEase(Ease.InCubic)) // return down
                        .SetUpdate(true)
                        .SetAutoKill(false)
                        .Pause();
            }

            if (lineRenderer != null)
            {
                startingLineThickness = lineRenderer.widthMultiplier;
                endingLineThickness = popAmount * startingLineThickness;
                
                linePop = DOTween.Sequence()
                    .Append(DOTween.To(
                        () => lineRenderer.widthMultiplier,
                        x => lineRenderer.widthMultiplier = x,
                        endingLineThickness,
                        popDuration / 2f
                    ).SetEase(Ease.OutCubic)) // pop up
                    .Append(DOTween.To(
                        () => lineRenderer.widthMultiplier,
                        x => lineRenderer.widthMultiplier = x,
                        startingLineThickness,
                        popDuration / 2f
                    ).SetEase(Ease.InCubic)) // return down
                    .SetUpdate(true)
                    .SetAutoKill(false)
                    .Pause();
            }
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

                    vehicleTooltip.SetText($"Time waiting: {selectedVehicle.timeWaiting:0.00}");

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
                junctionTooltip.SetText($"Elapsed Time: {selectedJunction.trafficController.elapsedTime:0.00}\n" +
                                        $"Congestion: {selectedJunction.trafficController.agent.tracker.GetAverageCongestion():0.00}\n");

                for (int i = 0; i < selectedJunction.trafficController.lights.Count; ++i)
                {
                    TrafficLight trafficLight = selectedJunction.trafficController.lights[i];
                    lightTooltips[i].SetText($"Queue: {trafficLight.vehicleQueue.Count}");
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                UnselectAll();

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
                    return;

                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.CompareTag("Vehicle"))
                    {
                        var vehicle = hit.collider.gameObject.GetComponent<Vehicle>();
                        
                        if (vehicle != selectedVehicle)
                            UnselectAll();
                        
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
                                
                                if (vehicle != selectedVehicle)
                                    UnselectAll();
                                
                                SelectVehicle(vehicle);
                            }
                        }
                        else
                        {
                            var junction = hit.collider.gameObject.GetComponent<Junction>();
                            
                            if (junction != selectedJunction)
                                UnselectAll();
                            
                            SelectJunction(junction);
                        }
                    }
                }
            }
        }

        void UnselectAll()
        {
            if (selectedVehicle)
            {
                var meshRenderer = selectedVehicle.path.from.meshRenderer;
                meshRenderer.sharedMaterials = new Material[] { meshRenderer.sharedMaterials[0] };

                meshRenderer = selectedVehicle.path.to.meshRenderer;
                meshRenderer.sharedMaterials = new Material[] { meshRenderer.sharedMaterials[0] };
            }
            
            selectedVehicle = null;
            lineRenderer.positionCount = 0;
            lineRenderer.loop = false;

            if (vehicleTooltip)
            {
                Destroy(vehicleTooltip.gameObject);
                vehicleTooltip = null;
            }
            
            selectedJunction = null;

            if (junctionTooltip)
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
            bool isSameVehicle = vehicle == selectedVehicle;
            selectedVehicle = vehicle;
            pathLength = selectedVehicle.path.Length();
            lineRenderer.positionCount = pathLength + 1;

            if (vehicleTooltip is null)
            {
                vehicleTooltip = Instantiate(tooltip, selectedVehicle.transform.position, Quaternion.identity);
                vehicleTooltip.Initialize(selectedVehicle.transform, new Vector3(0.4f, 5f, 0.4f));
            }

            if (isSameVehicle == false)
                UpdateVehicleLineRenderer();
            
            LinePopEffect();
            
            SelectBuilding(selectedVehicle.path.from);
            SelectBuilding(selectedVehicle.path.to);
        }

        void SelectBuilding(Building building)
        {
            var meshRenderer = building.meshRenderer;

            List<Material> mats = meshRenderer.sharedMaterials.ToList();

            if (mats.Count == 1)
            {
                mats.Add(outlineMaterial);
                meshRenderer.sharedMaterials = mats.ToArray();
            }
            
            OutlinePopEffect();
        }

        // When buildings get highlighted a small pop effect with the outline happens.
        void OutlinePopEffect()
        {
            outlinePop.Restart();
        }
        
        // When a vehicle path gets highlighted a small pop effect with the line happens.
        void LinePopEffect()
        {
            linePop.Restart();
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
            bool isSameJunction = selectedJunction == junction;
            selectedJunction = junction;
            selectedJunctionCollider = junction.boxCollider;
         
            if (isSameJunction == false)
                SetJunctionLineRenderer();
            
            LinePopEffect();
            
            if (junctionTooltip is null)
            {
                junctionTooltip = Instantiate(tooltip, selectedJunction.transform.position, Quaternion.identity);
                junctionTooltip.Initialize(selectedJunction.transform, new Vector3(-0.2f, 5f, 0f), true);
            }

            if (lightTooltips.Count <= 0)
            {
                for (int i = 0; i < junction.trafficController.lights.Count; ++i)
                {
                    TrafficLight trafficLight = junction.trafficController.lights[i];
                    Vector3 customOffset = 7.5f * trafficLight.roadDirection;
                
                    var lightTooltip = Instantiate(tooltip, trafficLight.transform.position, Quaternion.identity);
                    lightTooltip.Initialize(trafficLight.transform, customOffset + 5f * Vector3.up, true);
                
                    lightTooltips.Add(lightTooltip);
                }
            }
        }

        private void OnDisable()
        {
            outlineMaterial.SetFloat(OutlineThicknessID, startingOutlineThickness);
            lineRenderer.widthMultiplier = startingLineThickness;
        }
    }
}
