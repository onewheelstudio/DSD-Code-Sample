using HexGame.Units;
using OWS.ObjectPooling;
using Shapes;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryConnection : MonoBehaviour, IPoolable<DeliveryConnection>
{
    [SerializeField] private Polyline polyline;
    [SerializeReference] private RegularPolygon hexagon;
    [SerializeField] private Vector3 start;
    [SerializeField] private Vector3 end;
    [SerializeField] private float height = 2f;
    [SerializeField] private List<Vector3> points = new List<Vector3>();
    [SerializeField] private float stepSize = 0.1f;
    [SerializeField] private float dashSpeed = 0.5f;
    [SerializeField] private float alpha = 0.7f;
    [SerializeField] private float hexRadius = 1f;
    private static Action<DeliveryConnection> returnAction;

    [Header("Colors")]
    [SerializeField] private Color green;
    [SerializeField] private Color red;
    [SerializeField] private Color yellow;

    private void OnDisable()
    {
        ReturnToPool();
    }

    [Button("Generate Points")]
    private void GeneratePoints()
    {
        StopAllCoroutines();
        StartCoroutine(GeneratePathOverTime());
    }

    private IEnumerator GeneratePathOverTime()
    {
        points.Clear();
        hexagon.gameObject.SetActive(false);
        this.transform.LookAt(end, Vector3.up);
        for (int i = 0; i < NumberOfPoints; i++)
        {
            float x = i * stepSize;
            float y = GetHeight(x);
            points.Add(new Vector3(0, y, x));
            polyline.SetPoints(points);
            yield return null;
        }
        PlaceHex();
    }

    private void PlaceHex()
    {
        hexagon.gameObject.SetActive(true);
        hexagon.transform.position = end + Vector3.up * 0.025f;
        hexagon.transform.eulerAngles = new Vector3(90, 90, 0);
        StartCoroutine(FadeInHex());
    }

    private IEnumerator FadeInHex()
    {
        float alphaStep = 0.05f;
        int alphaStepCount = Mathf.CeilToInt(alpha / alphaStep);
        float sizeStep = hexRadius / alphaStepCount;
        Color hexColor = hexagon.Color;

        hexColor.a = 0f;
        hexagon.Radius = 0f;
        while (hexagon.Color.a < alpha)
        {
            hexColor.a += alphaStep;
            hexagon.Color = hexColor;
            hexagon.Radius += sizeStep;
            yield return null;
        }

        //ensure we get back to the starting values
        hexagon.Radius = hexRadius;
        hexColor.a = alpha;
        hexagon.Color = hexColor;
    }

    private int NumberOfPoints => (int)(Vector3.Distance(start, end) / stepSize) + 1;

    private float GetHeight(float x)
    {
        float distance = Vector3.Distance(start, end);
        height = Mathf.Max(1f, distance / 5f);
        return Mathf.Abs(x * (x - distance) * 4 * height / (distance * distance));
    }

    [Button]
    public void SetPositions(Vector3 start, Vector3 end)
    {
        this.start = start;
        this.end = end;
        GeneratePoints();
    }

    public void Initialize(Action<DeliveryConnection> returnAction)
    {
        DeliveryConnection.returnAction = returnAction;
    }

    public void ReturnToPool()
    {
        returnAction?.Invoke(this);
    }

    internal void SetStatus(ConnectionStatus status)
    {
        switch (status)
        {
            case ConnectionStatus.deliverable:
                polyline.Color = green;
                hexagon.Color = green;
                break;
            case ConnectionStatus.unDeliverable:
                polyline.Color = red;
                hexagon.Color = red;
                break;
            case ConnectionStatus.storageFull:
                polyline.Color = yellow;
                hexagon.Color = yellow;
                break;
            default:
                break;
        }
    }
}
