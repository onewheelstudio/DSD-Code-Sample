using HexGame.Grid;
using HexGame.Resources;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Conditions/Use During Day")]
public class UseDuringDay : UnitCondition
{
    public override bool CanUse(ResourceProductionBehavior rpb, Hex3 location)
    {
        return !DayNightManager.isNight;
    }
}
 