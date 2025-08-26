using HexGame.Units;
using OWS.ObjectPooling;
using System.Collections;
using UnityEngine;

public class RangeIndication : MonoBehaviour
{
    private HexRange rangeIndicator;
    private HexRange minRangeIndicator;
    [SerializeField] private Color borderColor = new Color(0.4392f, 0.7568f, 1f, 0.54f);
    [SerializeField] private Color bodyColor = new Color(0.4392f, 0.7568f, 1f, 0.36f);
    private CargoShuttleBehavior cargoShuttleBehavior;

    private Unit unit;

    [Header("Options")]
    [SerializeField] private bool showCargoRange = false;
    [SerializeField] private bool showAttackRange = false;
    [SerializeField] private bool showSightDistance = false;
    private static bool wasForced = false;

    private HexAreaDraw hexDraw;

    private void Awake()
    {
        if (showCargoRange)
            cargoShuttleBehavior = this.GetComponentInChildren<CargoShuttleBehavior>();
        hexDraw = FindFirstObjectByType<HexAreaDraw>();
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

        UnitSelectionManager.hoverOverUnit += ShowRange;
        UnitSelectionManager.unHoverOverUnit += HideRange;
        UnitSelectionManager.unitSelected += ShowRange;
        UnitSelectionManager.unitUnSelected += HideRange;

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

        UnitSelectionManager.hoverOverUnit -= ShowRange;
        UnitSelectionManager.unHoverOverUnit -= HideRange;
        UnitSelectionManager.unitSelected -= ShowRange;
        UnitSelectionManager.unitUnSelected -= HideRange;

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

    private void HideRange(PlayerUnit playerUnit)
    {
        if(wasForced || playerUnit == null)
            return;

        if (playerUnit.gameObject == this.gameObject)
            HideRange();
    }

    public void HideRange()
    {
        hexDraw.StopDrawing();
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

    private void ShowRange(PlayerUnit playerUnit)
    {
        if (wasForced)
            return;

        if (playerUnit.gameObject != this.gameObject)
            return;

        hexDraw.StopDrawing(); //clear previous range
        if (showCargoRange) //prevent showing cargo when selected
            return;
        ShowRange(playerUnit.unitType);
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
        hexDraw.SetColors(bodyColor, borderColor);
        hexDraw.AddRange(this.transform.position, minRange, range);
    }

    public void ShowRange(Vector3 center, int range, int minRange = 0)
    {
        hexDraw.SetColors(bodyColor, borderColor);
        hexDraw.AddRange(center, minRange, range);
    }

    public void SetColors(Color bodyColor, Color borderColor)
    {
        this.bodyColor = bodyColor;
        this.borderColor = borderColor;
    }

    private IEnumerator ShowRangeDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        ShowRange();
    }
}
