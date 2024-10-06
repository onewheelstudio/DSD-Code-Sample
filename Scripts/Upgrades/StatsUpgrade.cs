using HexGame.Units;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ManageableData]
[CreateAssetMenu(menuName = "Hex/Upgrades/Stats Upgrade")]
public class StatsUpgrade : Upgrade
{
    public PlayerUnitType unitType;
    [Tooltip("The stats that this upgrade applies to.")]
    public Stats statsToUpgrade;
    public Dictionary<Stat, float> upgradeToApply = new Dictionary<Stat, float>();
    public bool isPercentUpgrade = false;
    public static event Action<PlayerUnitType, StatsUpgrade> statUpgradeComplete;


    [Button]
    public override void DoUpgrade()
    {
        foreach (var upgrade in upgradeToApply)
        {
            statsToUpgrade.UnlockUpgrade(this);
        }

        statUpgradeComplete?.Invoke(unitType, this);
        UnlockQuests();
    }



    [Button]
    public override string GenerateDescription()
    {
        string generatedDescription = "";
        int totalCount = upgradeToApply.Count;

        List<Stat> keys = upgradeToApply.Keys.ToList();
        List<float> values = upgradeToApply.Values.ToList();

        if (totalCount == 1)
        {
            generatedDescription = $"{GetDescriptor(keys[0],values[0])} <b>{keys[0].ToNiceString()}</b> by {Mathf.Abs(values[0])} for all {unitType.ToNiceStringPlural()}.";
        }
        else if (totalCount == 2)
        {
            if (Mathf.Sign(values[0]) == Mathf.Sign(values[1]))
                generatedDescription = $"{GetDescriptor(keys[0], values[0])} <b>{keys[0].ToNiceString()}</b> by {Math.Abs(values[0])} and <b>{keys[1].ToNiceString()}</b> by {Mathf.Abs(values[1])} for all {unitType.ToNiceStringPlural()}.";
            else
                generatedDescription = $"{GetDescriptor(keys[0], values[0])} <b>{keys[0].ToNiceString()}</b> by {Mathf.Abs(values[0])} and {GetDescriptor(keys[1], values[1]).ToLower()} <b>{keys[1].ToNiceString()}</b> by {Mathf.Abs(values[1])} for all {unitType.ToNiceStringPlural()}.";

        }
        else
        {
            generatedDescription = $"{GetDescriptor(keys[0], values[0])} <b>{keys[0].ToNiceString()}</b> by {Mathf.Abs(values[0])},";
            for (int i = 1; i < keys.Count; i++)
            {
                if (i == keys.Count - 1)
                {
                    generatedDescription += $" and <b>{keys[0].ToNiceString()}</b> by {Mathf.Abs(values[0])}";
                }
                else
                {
                    generatedDescription += $" <b>{keys[0].ToNiceString()}</b> by {Mathf.Abs(values[0])},";
                }
            }

            generatedDescription += $" for all {unitType.ToNiceStringPlural()}.";
        }

        return generatedDescription;
    }

    private string GetDescriptor(Stat stat, float value)
    {
        if (value > 0)
            return "Increase";
        else
            return "Decrease";
    }

    public override string GenerateNiceName()
    {
        List<Stat> keys = upgradeToApply.Keys.ToList();
        List<float> values = upgradeToApply.Values.ToList();

        string statsString = unitType.ToNiceString();

        for (int i = 0; i < keys.Count; i++)
        {
            if (i == 0)
            {
                string firstStat = $"{keys[0].ToNiceString()}";

                if(statsString.Length + firstStat.Length > 25)
                {
                    statsString += $"\n{firstStat}";
                }
                else
                {
                    statsString += $": {firstStat}";
                }
            }
            else
            {
                statsString += $"+{keys[i].ToNiceString()}";
            }
        }

        return statsString;
    }

    private string GetSign(float value)
    {
        if (value > 0)
            return "+";
        else
            return "-";
    }

}

