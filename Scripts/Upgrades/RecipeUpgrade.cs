using HexGame.Resources;
using HexGame.Units;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Upgrades/Recipe Upgrade")]
public class RecipeUpgrade : Upgrade
{
    [Required]
    public ResourceProduction recipe;
    public ResourceType resourceType => recipe.GetProduction()[0].type;
    public PlayerUnitType usedByUnit;

    public override void DoUpgrade()
    {
        recipe.Unlock();
        MessagePanel.ShowMessage($"Unlocked {recipe.niceName} production", null);

        UnlockQuests();
    }

    public override string GenerateDescription()
    {
        Color resourceColor = ColorManager.GetColor(ColorCode.lowPriority);
        string buildingString = TMPHelper.Color($"{usedByUnit.ToNiceString()}", resourceColor);
        string timeString = TMPHelper.Color($"{recipe.GetTimeToProduce()}s", resourceColor);

        string description = $"Allows the production of {GetResourceString(this.recipe.GetProduction())} at a {buildingString}.";

        if(!string.IsNullOrEmpty(this.description))
            description += "\n\n" + this.description;
        description += $"\n\nProduction requires {GetResourceString(this.recipe.GetCost())}.";
        description += $"\n\nTime Required: {timeString}";
        return description;
    }

    private object GetResourceString(List<ResourceAmount> resources)
    {
        Color resourceColor = ColorManager.GetColor(ColorCode.lowPriority);
        if (resources.Count == 1)
        {
            string resourceString = TMPHelper.Color($"{resources[0].type.ToNiceString()}", resourceColor);
            return resourceString;
        }
        else if(resources.Count == 2)
        {
            string resourceString1 = TMPHelper.Color($"{resources[0].type.ToNiceString()}", resourceColor);
            string resourceString2 = TMPHelper.Color($"{resources[1].type.ToNiceString()}", resourceColor);

            return $"{resourceString1} and {resourceString2}";
        }
        else
        {
            string productString = "";
            for(int i = 0; i < resources.Count; i++)
            {
                string resourceString = TMPHelper.Color($"{resources[i].type.ToNiceString()}", resourceColor);
                if (i == resources.Count - 1)
                {

                    productString += $"and {resourceString}";
                }
                else
                {
                    productString += $"{resourceString}, ";
                }
            }

            return productString;
        }
    }

    public override string GenerateNiceName()
    {
        return $"{recipe.GetProduction()[0].type.ToNiceString(false)} Production";
    }
}
