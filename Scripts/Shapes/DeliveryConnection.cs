using DG.Tweening;
using HexGame.Resources;
using HexGame.Units;
using Nova;
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
    private Action<DeliveryConnection> returnAction;
    private ConnectionStatus status;

    [Header("Colors")]
    [SerializeField] private Color green;
    [SerializeField] private Color red;
    [SerializeField] private Color yellow;
    private static PlayerResources playerResources;

    [Header("Resource Display")]
    [SerializeField] private ListView resourceDisplay;
    private UIBlock resourceImageContainer;
    private ClipMask clipMask;
    [SerializeField] private Vector3 offset;

    [Header("Animation")]
    [SerializeField] private ConnectionCubeMotion connectionCube;
    private static ObjectPool<ConnectionCubeMotion> cubePool;
    [SerializeField] private float cubeSpeed = 1f;
    [SerializeField] private float cubeSize = 0.15f;
    [SerializeField] private float cubeDelay = 0.5f;
    private WaitForSeconds cubeWait;
    private Tween thicknessTween;

    private void Awake()
    {
        resourceDisplay.AddDataBinder<ResourceTemplate, ResourceImageVisuals>(SetResourceVisuals);
        resourceImageContainer = resourceDisplay.GetComponent<UIBlock>();
        clipMask = resourceDisplay.GetComponent<ClipMask>();

        if(cubePool == null)
        {
            cubePool = new ObjectPool<ConnectionCubeMotion>(connectionCube, 10);
        }

        cubeWait = new WaitForSeconds(cubeDelay);
    }


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
        }

        polyline.Thickness = 0f;
        DoLineThickness(0.05f, 0.5f);
        StartCoroutine(PlacePointsOverTime(points));
        PlaceHex();
        yield return null;
        if (status == ConnectionStatus.deliverable)
        {
            StartCoroutine(CreateInitialCubes(points));
            StartCoroutine(CreateCubesOverTime(points));
            SetResourcePosition(points);
        }
        yield break;
    }

    private IEnumerator PlacePointsOverTime(List<Vector3> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            polyline.SetPoints(points);
            yield return null;
        }
    }

    private IEnumerator CreateCubesOverTime(List<Vector3> points)
    {
        CreateInitialCubes(points);
        while(true)
        {
            ConnectionCubeMotion cube = cubePool.Pull();
            cube.transform.SetParent(this.transform);
            cube.transform.localPosition = Vector3.zero;

            ConnectionCubeMotion.ConnectionCubeData cubeData = new ConnectionCubeMotion.ConnectionCubeData();
            cubeData.start = start;
            cubeData.end = end;
            cubeData.speed = cubeSpeed;
            cubeData.size = cubeSize;
            cubeData.offset = 0;
            cubeData.color = polyline.Color;

            cube.StartMotion(cubeData);
            yield return cubeWait;
        }
    }

    private IEnumerator CreateInitialCubes(List<Vector3> points)
    {
        float distance = Vector3.Distance(start, end);
        float interval = cubeDelay * cubeSpeed;
        int count = Mathf.FloorToInt(distance / interval);

        for (int i = 1; i < count; i++)
        {
            ConnectionCubeMotion cube = cubePool.Pull();
            cube.transform.SetParent(this.transform);
            cube.transform.localPosition = Vector3.zero;
            
            ConnectionCubeMotion.ConnectionCubeData cubeData = new ConnectionCubeMotion.ConnectionCubeData();
            cubeData.start = start;
            cubeData.end = end;
            cubeData.speed = cubeSpeed;
            cubeData.size = cubeSize;
            cubeData.offset = i * interval;
            cubeData.color = polyline.Color;
            
            cube.StartMotion(cubeData);
            yield return null;
        }
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
        hexagon.Color = hexColor;
        hexagon.Radius = 0f;
        float _alpha = 0f;
        while (hexagon.Color.a < alpha)
        {
            _alpha += alphaStep;
            hexColor = hexagon.Color;
            hexColor.a = _alpha;
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

    public void SetResources(UnitStorageBehavior origin, UnitStorageBehavior destination)
    {
        HashSet<ResourceType> resources = origin.GetShippedResourceTypes(destination);
        SetResources(resources);
    }

    public void SetResource(ResourceType resource)
    {
        if (playerResources == null)
        {
            playerResources = FindFirstObjectByType<PlayerResources>();
        }
        List<ResourceTemplate> templates = new List<ResourceTemplate>();
        templates.Add(playerResources.GetResourceTemplate(resource));

        clipMask.SetAlpha(0f);
        resourceDisplay.SetDataSource(templates);
    }

    private void SetResources(HashSet<ResourceType> resources)
    {
        if(playerResources == null)
        {
            playerResources = FindFirstObjectByType<PlayerResources>();
        }
        List<ResourceTemplate> templates = new List<ResourceTemplate>();
        foreach (var resource in resources)
        {
            if (resource == ResourceType.Workers)
                continue;
            templates.Add(playerResources.GetResourceTemplate(resource));
        }

        clipMask.SetAlpha(0f);
        resourceDisplay.SetDataSource(templates);
    }

    private void SetResourceVisuals(Data.OnBind<ResourceTemplate> evt, ResourceImageVisuals target, int index)
    {
        target.resourceImage.SetImage(evt.UserData.icon);
        target.resourceImage.Color = evt.UserData.resourceColor;
    }

    private void SetResourcePosition(List<Vector3> points)
    {
        if (points.Count == 0)
            return;

        Vector3 highPoint = Vector3.zero;
        foreach (var point in points)
        {
            if(point.y > highPoint.y)
            {
                highPoint = point;
            }
        }
        clipMask.SetAlpha(0f);
        clipMask.DoFade(0.8f,.25f).SetEase(Ease.InOutCirc);
        resourceImageContainer.TrySetLocalPosition(highPoint + offset);
    }


    public void Initialize(Action<DeliveryConnection> returnAction)
    {
        this.returnAction = returnAction;
    }

    public void ReturnToPool()
    {
        returnAction?.Invoke(this);
    }

    internal void SetStatus(ConnectionStatus status)
    {
        this.status = status;
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

    public Tween DoLineThickness(float endValue, float duration)
    {
        thicknessTween = DOTween.To(() => this.polyline.Thickness, x => this.polyline.Thickness = x, endValue, duration);
        thicknessTween.OnComplete(() => this.polyline.Thickness = endValue);
        thicknessTween.SetUpdate(true);
        return thicknessTween; ;
    }
}
