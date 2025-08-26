using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Directives/Unlock Any Upgrade")]
public class UnlockAnyUpgradeQuest : DirectiveQuest
{
    public override void Initialize()
    {
        base.Initialize();
        UpgradeTile.upgradePurchased += CheckUpgrade;
    }

    private void CheckUpgrade(UpgradeTile tile)
    {
        UpgradeTile.upgradePurchased -= CheckUpgrade;
        isUnlocked = true;
        DirectiveUpdated();
    }

    public override List<bool> IsComplete()
    {
        return new List<bool>() { isUnlocked };
    }

    public override List<string> DisplayText()
    {
        return new List<string>() { $"Purchase an upgrade from the tech tree: 0/1" };
    }
}
