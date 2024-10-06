using HexGame.Resources;
using HexGame.Units;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "Hex/Upgrades/Production Upgrade")]
public class ProductionUpgrade : Upgrade
{
    public PlayerUnitType buildingType;
    public ResourceProduction productionToUpgrade;
    [Header("Changes to current production")]
    public List<ResourceAmount> productionResults = new List<ResourceAmount>();
    public List<ResourceAmount> productCost = new List<ResourceAmount>();
    public float timeToProduce = 0f;

    public override void DoUpgrade()
    {
        productionToUpgrade.AddUpgrade(this);
        UnlockQuests();
    }

    public override string GenerateDescription()
    {
        string generatedDescription = "";


        if (productionResults.Count == 1)
        {
            generatedDescription += $"Every production cycle {GetProductString(0)}";
        }
        else if (productionResults.Count == 2)
        {
            generatedDescription = $"Every production cycle {GetProductString(0)}";
            generatedDescription += $" and {GetProductString(1)}";
        }
        else if(productionResults.Count > 2)
        {
            generatedDescription += $"Every production cycle {GetProductString(0)}";

            for (int i = 1; i < productionResults.Count; i++)
            {
                if (i == productionResults.Count - 1)
                {
                    generatedDescription += $" and {GetProductString(i)}";
                }
                else
                {
                    generatedDescription += " " + GetProductString(i) + ",";
                }
            }
        }

        if(productionResults.Count > 0)
            generatedDescription += $" is produced by {buildingType.ToNiceString()}.";

        if (generatedDescription.Length > 0 && productCost.Count > 0)
            generatedDescription += $"\n\n";

        if (productCost.Count == 1)
        {
            generatedDescription += $"This comes at the cost of {GetCostResourceString(0)}.";
        }
        else if (productCost.Count == 2)
        {
            generatedDescription = $" This comes at the cost of {GetCostResourceString(0)}";
            generatedDescription += $" and {GetCostResourceString(1)}";
        }
        else if (productCost.Count > 2)
        {
            generatedDescription += $" This comes at the cost of {GetCostResourceString(0)}";

            for (int i = 1; i < productCost.Count; i++)
            {
                if (i == productCost.Count - 1)
                {
                    generatedDescription += $" and {GetCostResourceString(i)}.";
                }
                else
                {
                    generatedDescription += " " + GetCostResourceString(i)+",";
                }
            }
        }

        if(generatedDescription.Length > 0 && timeToProduce != 0)
            generatedDescription += $"\n\n";

        if(timeToProduce > 0)
        {
            generatedDescription += $"Production time is increased by {timeToProduce}s.";
        }
        else if(timeToProduce < 0)
        {
            generatedDescription += $"Production time is decreased by {Mathf.Abs(timeToProduce)}s.";
        }

        return generatedDescription;
    }

    private string GetProductString(int i)
    {
        return $"{Mathf.Abs(productionResults[i].amount)} {GetDescriptor(productionResults[i].amount)} <b>{productionResults[i].type.ToNiceString()} </b>";
    }

    private string GetCostResourceString(int i)
    {
        return $"{Mathf.Abs(productCost[i].amount)} {GetDescriptor(productCost[i].amount)} <b> {productCost[i].type.ToNiceString()} </b>";
    }

    private string GetDescriptor(float value)
    {
        if(value > 0)
        {
            return "more";
        }
        else
        {
            return "less";
        }
    }

    public override string GenerateNiceName()
    {
        return $"{buildingType.ToNiceString()}: Production";
    }
}
