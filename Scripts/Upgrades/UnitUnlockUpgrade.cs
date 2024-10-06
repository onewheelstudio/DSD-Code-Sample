using HexGame.Resources;
using HexGame.Units;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

[ManageableData]
[CreateAssetMenu(menuName = "Hex/Upgrades/Building Unlock")]
public class UnitUnlockUpgrade : Upgrade
{
    public PlayerUnitType buildingToUnlock;
    public static event Action<PlayerUnitType> unlockBuilding;
    private static UnitManager unitManager;

    [Button]
    public override void DoUpgrade()
    {
        unlockBuilding?.Invoke(buildingToUnlock);

        UnlockQuests();
    }

    public override string GenerateDescription()
    {
        string genericDescription = $"Unlocks the ability to build <b>{buildingToUnlock.ToNiceString()}</b>";

        if(this.description.IsNullOrWhitespace())
        {
            return genericDescription +"." + GetRequiredResources();
        }
        else
            return genericDescription + "\n\n" + this.description + GetRequiredResources();
    }

    private string GetRequiredResources()
    {
        if (unitManager == null)
            unitManager = FindObjectOfType<UnitManager>();

        List<ResourceAmount> costs = unitManager.GetUnitCost(buildingToUnlock);
        string resourcesRequired = "";

        Color resourceColor = ColorManager.GetColor(ColorCode.lowPriority);

        if(costs == null || costs.Count == 0)
            return resourcesRequired;

        if(costs.Count == 1)
        {
            string amountString = TMPHelper.Color($"{costs[0].amount} {costs[0].type.ToNiceString()}", resourceColor);
            resourcesRequired = $"\n\nRequires {amountString} to build.";
        }
        else if (costs.Count == 2)
        {
            string amountString1 = TMPHelper.Color($"{costs[0].amount} {costs[0].type.ToNiceString()}", resourceColor);
            string amountString2 = TMPHelper.Color($"{costs[1].amount} {costs[1].type.ToNiceString()}", resourceColor);
            resourcesRequired = $"\n\nRequires  {amountString1} and {amountString2} to build.";
        }
        else
        {
            string amountString1 = TMPHelper.Color($"{costs[0].amount} {costs[0].type.ToNiceString()}", resourceColor);
            resourcesRequired = $"\n\nRequires {amountString1},";
            for (int i = 1; i < costs.Count; i++)
            {
                if (i == costs.Count - 1)
                {
                    string amountString = TMPHelper.Color($"{costs[i].amount} {costs[i].type.ToNiceString()}", resourceColor);
                    resourcesRequired += $" and {amountString} to build.";
                }
                else
                {
                    string amountString = TMPHelper.Color($"{costs[i].amount} {costs[i].type.ToNiceString()}", resourceColor);
                    resourcesRequired += $" {amountString}, ";
                }
            }
        }   

        return resourcesRequired;
    }

    public override string GenerateNiceName()
    {
        return $"Unlock: {buildingToUnlock.ToNiceString()}";
    }
}
