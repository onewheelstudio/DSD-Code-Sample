using HexGame.Grid;
using HexGame.Resources;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Conditions/Use During Night")]
public class UseDuringNight : UnitCondition
{
    public override bool CanUse(ResourceProductionBehavior rpb, Hex3 location)
    {
        return !DayNightManager.isDay;
    }
}