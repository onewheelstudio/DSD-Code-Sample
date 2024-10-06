using HexGame.Units;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Upgrades/Increase Limit")]
public class IncreaseLimitUpgrade : Upgrade
{
    [SerializeField] private PlayerUnitType unitType;
    public PlayerUnitType UnitType => unitType;
    [SerializeField] private int increaseBy = 1;
    public static event System.Action<PlayerUnitType, int> OnLimitIncreased;
    public override void DoUpgrade()
    {
        OnLimitIncreased?.Invoke(unitType, increaseBy);

        UnlockQuests();
    }

    public override string GenerateDescription()
    {
        return $"Allows {increaseBy} additional {unitType.ToNiceString()} to be built.";
    }

    public override string GenerateNiceName()
    {
        return $"+{increaseBy} {unitType.ToNiceString()}";
    }
}
