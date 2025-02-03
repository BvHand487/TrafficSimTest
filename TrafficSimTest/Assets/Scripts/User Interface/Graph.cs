using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Graph : MonoBehaviour
{
    [SerializeField] private Color edgeColor;

    [Header("Point settings")]
    [SerializeField] private bool spriteOnPoints = false;
    [SerializeField] private Sprite pointSprite;
    [SerializeField] private Color pointColor;

    [Header("Label settings")]
    [SerializeField] private float axisTickXOffset = -20f;
    [SerializeField] private float axisTickYOffset = -20f;

    [Header("Trendline settings")]
    [SerializeField] private bool activateTrendline = false;
    [SerializeField] private Gradient trendlineColorGradient;

    private RectTransform graphContainer;
    private RectTransform axisTickX;
    private RectTransform axisTickY;
    private RectTransform dashesX;
    private RectTransform dashesY;
    private List<GameObject> graphElements;

    public void Awake()
    {
        graphContainer = GetComponent<RectTransform>();
        axisTickX = transform.Find("AxisTickX").GetComponent<RectTransform>();
        axisTickY = transform.Find("AxisTickY").GetComponent<RectTransform>();
        dashesX = transform.Find("DashedLineX").GetComponent<RectTransform>();
        dashesY = transform.Find("DashedLineY").GetComponent<RectTransform>();
        graphElements = new List<GameObject>();
    }

    private GameObject CreatePoint(Vector2 position)
    {
        var obj = new GameObject("Circle", typeof(Image));
        obj.transform.SetParent(graphContainer, false);

        var img = obj.GetComponent<Image>();
        img.sprite = pointSprite;
        img.color = pointColor;

        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(5, 5);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;

        return obj;
    }

    private GameObject CreateEdge(Vector2 from, Vector2 to)
    {
        var obj = new GameObject("Edge", typeof(Image));
        obj.transform.SetParent(graphContainer, false);

        var img = obj.GetComponent<Image>();
        img.color = edgeColor;

        Vector2 relativeVector = to - from;
        Vector2 edgeDirection = relativeVector.normalized;
        float edgeLength = relativeVector.magnitude;

        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = Utils.Math.GetMidpointVector(from, to);
        rectTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(edgeDirection.y, edgeDirection.x) * Mathf.Rad2Deg);
        rectTransform.sizeDelta = new Vector2(edgeLength, 3);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;

        return obj;
    }

    public void ShowGraph(List<float> values, int maxVisibleValues = -1, Func<int, string> getLabelX = null, Func<float, string> getLabelY = null, bool isCoroutine=true)
    {
        var enumerator = ShowGraphCoroutine(values, maxVisibleValues, getLabelX, getLabelY);
        
        if (isCoroutine)
            StartCoroutine(enumerator);
        else
            while (enumerator.MoveNext()) { }
    }

    public IEnumerator ShowGraphCoroutine(List<float> values, int maxVisibleValues = -1, Func<int, string> getLabelX = null, Func<float, string> getLabelY = null)
    {
        if (getLabelX == null)
            getLabelX = delegate (int idx) { return $"{idx}"; };

        if (getLabelY == null)
            getLabelY = delegate (float value) { return $"{Mathf.RoundToInt(value)}"; };

        if (graphElements.Count > 0)
            DestroyGraph();

        if (maxVisibleValues <= 0)
            maxVisibleValues = values.Count;

        float yMax = float.MinValue;
        float yMin = float.MaxValue;
        for (int i = Mathf.Max(values.Count - maxVisibleValues, 0); i < values.Count; i++)
        {
            if (values[i] > yMax) yMax = values[i];
            if (values[i] < yMin) yMin = values[i];
        }

        float deltaY = yMax - yMin;
        float bufferSpace = deltaY <= 0 ? 5f : deltaY * 0.2f;

        yMax = yMax + bufferSpace;
        yMin = (yMin > 0 && yMin - bufferSpace < 0) ? 0f : yMin - bufferSpace;

        float graphWidth = graphContainer.sizeDelta.x;
        float graphHeight = graphContainer.sizeDelta.y;
        float xSize = graphWidth / (maxVisibleValues + 1);

        // for trendline
        float xSum = 0f;
        float ySum = 0f;
        float x2Sum = 0f;
        float xySum = 0f;

        int xIndex = 0;
        GameObject previousPoint = null, currentPoint;
        Vector2 previousPointPos = -Vector2.one, currentPointPos;

        for (int i = Mathf.Max(values.Count - maxVisibleValues, 0); i < values.Count; i++)
        {
            float xPos = xSize + xIndex * xSize;
            float yPos = ((values[i] - yMin) / (yMax - yMin)) * graphHeight;

            if (activateTrendline && maxVisibleValues > 1)
            {
                xSum += xPos;
                ySum += yPos;
                x2Sum += xPos * xPos;
                xySum += xPos * yPos;
            }

            // Create graph point
            currentPointPos = new Vector2(xPos, yPos);

            if (spriteOnPoints)
            {
                currentPoint = CreatePoint(currentPointPos);
                graphElements.Add(currentPoint);

                // Create graph edge between points
                if (previousPoint != null)
                {
                    var edge = CreateEdge(previousPoint.GetComponent<RectTransform>().anchoredPosition, currentPoint.GetComponent<RectTransform>().anchoredPosition);
                    graphElements.Add(edge);
                }

                previousPoint = currentPoint;
            }
            else
            {
                if (previousPointPos != -Vector2.one)
                {
                    var edge = CreateEdge(previousPointPos, currentPointPos);
                    graphElements.Add(edge);
                }
            }

            previousPointPos = currentPointPos;

            // Create axis ticks on the X-axis
            RectTransform tickX = Instantiate(axisTickX);
            tickX.SetParent(graphContainer, false);
            tickX.gameObject.SetActive(true);
            tickX.anchoredPosition = new Vector2(xPos, axisTickXOffset);
            tickX.GetComponent<TextMeshProUGUI>().text = getLabelX(i);
            graphElements.Add(tickX.gameObject);

            // Create X-axis dashes across the graph
            RectTransform dashX = Instantiate(dashesX);
            dashX.SetParent(graphContainer, false);
            dashX.gameObject.SetActive(true);
            dashX.anchoredPosition = new Vector2(xPos, 0);
            graphElements.Add(dashX.gameObject);

            xIndex++;

            // yield execution every few iterations
            if (xIndex % 17 == 0)
                yield return null;
        }

        // Create axis ticks on the Y-axis
        int tickCountY = 10;
        for (int i = 0; i <= tickCountY; i++)
        {
            RectTransform tickY = Instantiate(axisTickY);
            tickY.SetParent(graphContainer, false);
            tickY.gameObject.SetActive(true);
            graphElements.Add(tickY.gameObject);

            float normValue = i * 1f / tickCountY;
            tickY.anchoredPosition = new Vector2(axisTickYOffset, normValue * graphHeight);
            tickY.GetComponent<TextMeshProUGUI>().text = getLabelY(yMin + normValue * (yMax - yMin));

            // Create Y-axis dashes across the graph
            RectTransform dashY = Instantiate(dashesY);
            dashY.SetParent(graphContainer, false);
            dashY.gameObject.SetActive(true);
            dashY.anchoredPosition = new Vector2(0, normValue * graphHeight);
            graphElements.Add(dashY.gameObject);

            // yield execution every few iterations
            if (i % 4 == 0)
                yield return null;
        }

        if (activateTrendline && maxVisibleValues > 1)
        {
            // Create trendline using linear regression
            float slope = (maxVisibleValues * xySum - xSum * ySum) / (maxVisibleValues * x2Sum - xSum * xSum);
            float offset = (ySum - slope * xSum) / maxVisibleValues;
            Func<float, float> trendlineFunction = (x) => slope * x + offset;

            float normalizedSlope = Mathf.Clamp(slope, -1f, 1f);
            Color trendlineColor = trendlineColorGradient.Evaluate((normalizedSlope + 1) / 2f);

            float endPointX = graphWidth - xSize;
            float endPointY = trendlineFunction(endPointX);

            // if trendline goes above the graph limit it to yMax
            if (endPointY > graphHeight)
            {
                endPointX = (graphHeight - offset) / slope;
                endPointY = graphHeight;
            }

            var trendline = CreateEdge(new Vector2(xSize, trendlineFunction(xSize)), new Vector2(endPointX, endPointY));
            trendline.GetComponent<Image>().color = trendlineColor;

            graphElements.Add(trendline.gameObject);
        }
    }


    public void DestroyGraph()
    {
        foreach (var obj in graphElements)
            Destroy(obj);

        graphElements.Clear();
    }
}
