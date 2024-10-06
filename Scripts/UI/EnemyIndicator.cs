using HexGame.Units;
using Nova;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyIndicator : MonoBehaviour
{
    [Range(0,300), SerializeField] private float offsetAmount = 135;
    [SerializeField] private Transform transformToAlign;
    [SerializeField] private Transform miniMapBackground;
    private static ScreenSpace screenSpace;
    private MinimapManager minimapManager;

    private static List<IndicatorObject> indicatorObjects = new List<IndicatorObject>();

    [Header("Indicator Prefabs")]
    [SerializeField] private UIBlock crystalIndicatorPrefab;
    [SerializeField] private UIBlock markerIndicatorPrefab;
    [SerializeField] private UIBlock enemyUnitIndicatorPrefab;
    private static ObjectPool<PoolObject> crystalIndicatorPool;
    private static ObjectPool<PoolObject> markerIndicatorPool;
    private static ObjectPool<PoolObject> enemyUnitIndicatorPool;

    private void Awake()
    {
        screenSpace = GetComponentInParent<ScreenSpace>();
        minimapManager = FindFirstObjectByType<MinimapManager>();
        crystalIndicatorPool = new ObjectPool<PoolObject>(crystalIndicatorPrefab.gameObject);
        markerIndicatorPool = new ObjectPool<PoolObject>(markerIndicatorPrefab.gameObject);
        enemyUnitIndicatorPool = new ObjectPool<PoolObject>(enemyUnitIndicatorPrefab.gameObject);

        indicatorObjects = new List<IndicatorObject>();
    }

    private void OnEnable()
    {
        DayNightManager.transitionToDay += ToggleIndicators;
    }

    private void OnDisable()
    {
        DayNightManager.transitionToDay -= ToggleIndicators;
    }

    private void Update()
    {
        foreach (var indicatorObject in indicatorObjects)
        {
            bool showIndicator = IsIndicatorVisible(indicatorObject);
            indicatorObject.indicator.Visible = showIndicator;

            if (!showIndicator)
                continue;

            Vector3 direction = indicatorObject.objectToFollow.transform.position - transformToAlign.transform.position;
            float angle = Vector3.SignedAngle(transformToAlign.transform.forward, direction, Vector3.up);

            SetIndicatiorPositionOnMap(indicatorObject.indicator, angle, indicatorObject.flipDirection);
        }
    }
    private void ToggleIndicators(int dayNumber, float delay)
    {
        for(int i = indicatorObjects.Count - 1; i >= 0; i--)
        {
            if (indicatorObjects[i].indicatorType != IndicatorType.crystal)
            {
                indicatorObjects[i].indicator.gameObject.SetActive(false);
                indicatorObjects.RemoveAt(i);
            }
        }
    }

    private bool IsIndicatorVisible(IndicatorObject indicatorObject)
    {
        if(!indicatorObject.objectToFollow.activeInHierarchy)
            return false;

        if (indicatorObject.renderer == null)
            return false;

        //if (!indicatorObject.renderer.isVisible)
        //    return true;

        //if a unit is visible then don't show the indicator
        if(indicatorObject.indicatorType == IndicatorType.enemyUnit && indicatorObject.enemyUnit.isVisible)
            return false;

        //check if the object is visible on the minimap
        float distanceToCenter = (indicatorObject.objectToFollow.transform.position - transformToAlign.transform.position).sqrMagnitude;

        if (indicatorObject.indicatorType == IndicatorType.enemyUnit)
        {
            indicatorObject.flipDirection = minimapManager.Size * minimapManager.Size > distanceToCenter;
            return true;
        }

        if (minimapManager.Size * minimapManager.Size > distanceToCenter)
            return false;

        return true;
    }

    private void SetIndicatiorPositionOnMap(UIBlock block, float angle, bool isFlipped)
    {
        Vector2 pos = miniMapBackground.transform.localPosition;
        pos += new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * offsetAmount;
        block.TrySetLocalPosition(pos);
        if(isFlipped)
            block.transform.localRotation = Quaternion.Euler(0, 0, -angle + 180);
        else
            block.transform.localRotation = Quaternion.Euler(0, 0, -angle);
    }

    [Button]
    public static void AddIndicatorObject(GameObject objectToFollow, IndicatorType indicatorType)
    {
        IndicatorObject indicatorObject = new IndicatorObject();
        indicatorObject.objectToFollow = objectToFollow;
        indicatorObject.indicatorType = indicatorType;
        switch (indicatorType)
        {
            case IndicatorType.crystal:
                indicatorObject.indicator = crystalIndicatorPool.Pull().GetComponent<UIBlock2D>();
                break;
            case IndicatorType.marker:
                indicatorObject.indicator = markerIndicatorPool.Pull().GetComponent<UIBlock2D>();
                break;
            case IndicatorType.enemyUnit:
                indicatorObject.indicator = enemyUnitIndicatorPool.Pull().GetComponent<UIBlock2D>();
                indicatorObject.enemyUnit = objectToFollow.GetComponent<EnemyUnit>();
                break;
        }

        foreach(Transform child in objectToFollow.transform)
        {
            if(child.gameObject.layer == 14)
            {
                indicatorObject.renderer = child.GetComponent<Renderer>();
                break;
            }
        }

        indicatorObject.indicator.transform.SetParent(screenSpace.transform);
        indicatorObject.indicator.transform.localScale = Vector3.one;
        indicatorObjects.Add(indicatorObject);
    }
}

[System.Serializable]
public class IndicatorObject
{
    public GameObject objectToFollow;
    public EnemyUnit enemyUnit;
    public UIBlock indicator;
    public IndicatorType indicatorType;
    public Renderer renderer;
    public bool flipDirection;
}

public enum IndicatorType
{
    crystal,
    marker,
    enemyUnit,
}
