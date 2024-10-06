using HexGame.Resources;
using HexGame.Units;
using OWS.ObjectPooling;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class RangeIndication : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private GameObject rangePrefab;
    private static ObjectPool<HexRange> rangeIndicatorPool;
    private HexRange rangeIndicator;
    private HexRange minRangeIndicator;
    [SerializeField] private Color borderColor = new Color(0.4392f, 0.7568f, 1f, 0.54f);
    [SerializeField] private Color bodyColor = new Color(0.4392f, 0.7568f, 1f, 0.36f);
    [SerializeField] private Color secondaryBodyColor = new Color(0.4392f, 0.7568f, 1f, 0.36f);
    private CargoShuttleBehavior cargoShuttleBehavior;

    private Unit unit;

    [Header("Options")]
    [SerializeField] private bool showCargoRange = false;
    [SerializeField] private bool showAttackRange = false;
    [SerializeField] private bool showSightDistance = false;
    [SerializeField] private bool showOnClick = true;
    private bool wasForced = false;

    private void Awake()
    {
        rangeIndicatorPool = new ObjectPool<HexRange>(rangePrefab);
        if (showCargoRange)
            cargoShuttleBehavior = this.GetComponentInChildren<CargoShuttleBehavior>();
    }

    private void OnEnable()
    {
        if (showCargoRange)
        {
            UnitManager.unitPlacementStarted += ForceShowRange;
            UnitManager.unitPlacementFinished += ForceHideRange;
            ToggleRangeButton.toggleCargoRange += ToggleRange;
            HexTileManager.tilePlacementCompleted += HideRange;
            HexTileManager.tilePlacementStarted += ShowRange;
        }
        if (showAttackRange)
            ToggleRangeButton.toggleAttackRange += ToggleRange;

        unit = this.GetComponent<Unit>();

    }



    private void OnDisable()
    {
        if (showCargoRange)
        {
            UnitManager.unitPlacementStarted -= ForceShowRange;
            UnitManager.unitPlacementFinished -= ForceHideRange;
            ToggleRangeButton.toggleCargoRange -= ToggleRange;
            HexTileManager.tilePlacementCompleted -= HideRange;
            HexTileManager.tilePlacementStarted -= ShowRange;
        }
        else if(showAttackRange)
            ToggleRangeButton.toggleAttackRange -= ToggleRange;

        if (rangeIndicator != null)
            rangeIndicator.gameObject.SetActive(false);
    }

    private void ToggleRange(bool isOn)
    {
        if(!isOn)
        {
            ShowRange();
            wasForced = true;
        }
        else
        { 
            HideRange(); //type doesn't matter
            wasForced = false;
        }
    }

    private void ForceHideRange()
    {
        wasForced = false;
        HideRange();
    }

    private void HideRange()
    {
        if (rangeIndicator == null)
            return;

        rangeIndicator.HideRange();
        rangeIndicator = null;
        if (minRangeIndicator != null)
        {
            minRangeIndicator.HideRange();
            minRangeIndicator = null;
        }
    }
    private void ShowRange()
    {
        ShowRange(PlayerUnitType.hq);
    }

    private void ForceShowRange(PlayerUnitType unitType)
    {
        wasForced = true;
        ShowRange(unitType);
    }

    private void ShowRange(PlayerUnitType unitType)
    {
        if (rangeIndicator != null)
            return;

        if (showCargoRange)
            ShowRange((int)cargoShuttleBehavior.GetStat(Stat.movementRange));
        else if (showAttackRange)
            ShowRange((int)unit.GetStat(Stat.maxRange), (int)unit.GetStat(Stat.minRange));
        else if (showSightDistance && unit.IsFunctional())
            ShowRange((int)unit.GetStat(Stat.sightDistance));
        else if (showSightDistance && !unit.IsFunctional())
            ShowRange(0);
    }

    private void ShowRange(int range, int minRange = 0)
    {
        if(rangeIndicator == null)
            rangeIndicator = rangeIndicatorPool.Pull(this.transform.position + Vector3.up * 0.105f, Quaternion.Euler(90f,0f,0f));
        rangeIndicator.ShowRange(range, minRange, borderColor, bodyColor);

        //if (minRangeIndicator == null && minRange > 0)
        //{
        //    minRangeIndicator = rangeIndicatorPool.Pull(this.transform.position + Vector3.up * 0.1f, Quaternion.Euler(90f, 0f, 0f));
        //    minRangeIndicator.ShowRange(minRange, 0, borderColor, secondaryBodyColor);
        //}
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (wasForced)
            return; 
        
        if (showOnClick)
        {
            ShowRange();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (wasForced)
            return;
        
        if (showOnClick)
        {
            StartCoroutine(ShowRangeDelayed());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(wasForced)
            return;

        if (showOnClick)
        {
            HideRange();
            StopAllCoroutines();
        }
    }

    private IEnumerator ShowRangeDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        ShowRange();
    }
}
