using HexGame.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnlockUpgradeQuest : DirectiveQuest
{
    [Header("Quest Unlock Settigns")]
    public Upgrade upgradeToUnlock;

    public override void Initialize()
    {
        base.Initialize();
        UpgradeTile.upgradePurchased += CheckUpgrade;
    }

    private void CheckUpgrade(UpgradeTile tile)
    {
        if(tile.upgrade == upgradeToUnlock)
        {
            UpgradeTile.upgradePurchased -= CheckUpgrade;
            isUnlocked = true;
            DirectiveUpdated();
        }
    }

    public override List<bool> IsComplete()
    {
        return new List<bool> (){isUnlocked};
    }

    public override List<string> DisplayText()
    {
        return new List<string>() { $"In the Tech Tree {upgradeToUnlock.GenerateNiceName()}: 0/1" };
    }

    public void Setup(Upgrade upgradeToUnlock, int repReward, int techCreditsReward)
    {
        this.useRepReward = true;
        this.questReward = new QuestReward(repReward, techCreditsReward);
        this.upgradeToUnlock = upgradeToUnlock;
    }

}
