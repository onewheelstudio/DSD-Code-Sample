using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Conditions/Use During Night")]
public class UseDuringNight : UnitCondition
{
    public override bool CanUse(GameObject gameObject)
    {
        return !DayNightManager.isDay;
    }
}