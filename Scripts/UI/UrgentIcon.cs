using HexGame.Units;
using UnityEngine;

public class UrgentIcon : MonoBehaviour
{
    [SerializeField] private GameObject iconParent;
    private PlayerUnit urgentUnit;

    private void OnEnable()
    {
        UnitInfoWindow.urgentPriorityTurnOn += ShowIcon;
        UnitInfoWindow.urgentPriorityTurnOff += HideIcon;
        PlayerUnit.unitRemoved += HideIcon;
        HideIcon();
    }

    private void OnDisable()
    {
        UnitInfoWindow.urgentPriorityTurnOn -= ShowIcon;
        UnitInfoWindow.urgentPriorityTurnOff -= HideIcon;
        PlayerUnit.unitRemoved -= HideIcon;
    }

    private void ShowIcon(PlayerUnit unit)
    {
        this.urgentUnit = unit;
        this.transform.position = unit.transform.position;
        iconParent.SetActive(true);
    }

    private void HideIcon(Unit unit)
    {
        if(urgentUnit == null)
            return;

        if (unit.gameObject == urgentUnit.gameObject)
            HideIcon();
    }

    private void HideIcon(PlayerUnit unit)
    {
        HideIcon();
    }

    private void HideIcon()
    {
        iconParent.SetActive(false);
    }
}
