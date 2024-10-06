using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Conditions/Use During Day")]
public class UseDuringDay : UnitCondition
{
    public override bool CanUse(GameObject gameObject)
    {
        return !DayNightManager.isNight;
    }
}
 