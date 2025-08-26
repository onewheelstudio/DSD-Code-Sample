using System;
using UnityEngine;

[CreateAssetMenu(fileName = "UnlockUpgrade", menuName = "Hex/Triggers/Unlock Upgrade")]
public class UnlockUpgrade : TriggerBase
{
    [SerializeField] private Upgrade upgradeToUnlock;
    public static event Action<Upgrade> UpgradeUnlocked;

    [SerializeField] private PriceChangeTrigger priceChangeTrigger;

    public override void DoTrigger()
    {
        upgradeToUnlock.DoUpgrade();
        UpgradeUnlocked?.Invoke(upgradeToUnlock);

        if (priceChangeTrigger != null)
            priceChangeTrigger.DoTrigger();
    }
    
}
