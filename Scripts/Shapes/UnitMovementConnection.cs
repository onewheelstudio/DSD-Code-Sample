using HexGame.Units;
using OWS.ObjectPooling;
using Shapes;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMovementConnection : MonoBehaviour
{
    [SerializeField] private Polyline polyline;
    [SerializeReference] private RegularPolygon hexagon;
    public Vector3 destination => hexagon.transform.position;
    [SerializeField] private Vector3 start;
    [SerializeField] private Vector3 end;
    public Vector3 End => end;
    [SerializeField] private float height = 2f;
    [SerializeField] private List<Vector3> points = new List<Vector3>();
    [SerializeField] private float stepSize = 0.1f;
    [SerializeField] private float dashSpeed = 0.5f;
    [SerializeField] private float alpha = 0.7f;
    [SerializeField] private float hexRadius = 1f;
    private static Action<UnitMovementConnection> returnAction;

    [Header("Colors")]
    [SerializeField] private Color goColor;
    [SerializeField] private Color noGoColor;

    [Button("Generate Points")]
    private void GeneratePoints()
    {
        points.Clear();
        hexagon.gameObject.SetActive(false);
        this.transform.LookAt(end, Vector3.up);
        for (int i = 0; i < NumberOfPoints; i++)
        {
            float x = i * stepSize;
            float y = GetHeight(x);
            points.Add(new Vector3(0, y, x));
        }
        polyline.SetPoints(points);
        PlaceHex();
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
        StopAllCoroutines();
        StartCoroutine(FadeInHex());
        Debug.Log("Placing Hex");
    }

    private IEnumerator FadeInHex()
    {
        float alphaStep = 0.1f;
        int alphaStepCount = Mathf.CeilToInt(alpha / alphaStep);
        float sizeStep = hexRadius / alphaStepCount;
        Color hexColor = hexagon.Color;

        hexColor.a = 0f;
        hexagon.Radius = 0f;
        while (hexColor.a < alpha)
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

    public void Initialize(Action<UnitMovementConnection> returnAction)
    {
        UnitMovementConnection.returnAction = returnAction;
    }

    internal void SetStatus(bool isValidLocation)
    {
        if(isValidLocation)
        {
            polyline.Color = goColor;
            Color hexColor = goColor;
            hexColor.a = hexagon.Color.a;
            hexagon.Color = hexColor;

        }
        else
        {
            polyline.Color = noGoColor;
            Color hexColor = noGoColor;
            hexColor.a = hexagon.Color.a;
            hexagon.Color = hexColor;
        }
    }
}
