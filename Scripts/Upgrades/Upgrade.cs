using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using HexGame.Resources;

[CreateAssetMenu(menuName = "Hex/Upgrades/Upgrade")]
public abstract class Upgrade : UpgradeBase
{
    public int cost => GetCost();
    public bool showInTechTree = true;
    public bool unlockedAtStart;
    [Range(0,12)]
    public int upgradeTier = 0;
    [Range(0,10)]
    public int subTier = 0;

    public List<DirectiveQuest> unlockQuests;

    [Button]
    public abstract void DoUpgrade();
    protected void UnlockQuests()
    {
        if (unlockQuests != null)
        {
            DirectiveMenu directiveMenu = FindObjectOfType<DirectiveMenu>();
            foreach (var quest in unlockQuests)
            {
                directiveMenu.TryAddQuest(quest);
            }
        }
    }

    public enum UpgradeStatus
    {
        purchased,
        unlocked,
        locked
    }

    [Button]
    protected int GetCost()
    {
        return CalculateCost(upgradeTier);
    }

    //function that returns a integer from the fibonacci sequence based on the upgrade tier
    private int CalculateCost(int tier)
    {

        switch(tier)
        {
            case 0:
                return 500;
            case 1:
                return 500;
            case 2:
                return 3500;
        }

        tier++;
        int a = 0;
        int b = 1;
        int c = 0;
        for (int i = 0; i < tier; i++)
        {
            c = a + b;
            a = b;
            b = c;
        }
        return c * (c - 1) * GameConstants.upgradeCostMultiplier;
    }

    public int RequiredReputation()
    {
        if(upgradeTier > 0)
            return (upgradeTier -1) * 500;
        else
            return 0;
    }

}

[CreateAssetMenu(menuName = "Hex/Upgrade/Base")]
public abstract class UpgradeBase : SerializedScriptableObject
{

    [SerializeField] private bool overrideName = true;
    [ShowInInspector]
    [LabelWidth(125)]
    [VerticalGroup("Details")]
    [HideIf("overrideName")]
    [SerializeField]private string upgradeName;
    public string UpgradeName { get => overrideName ? GenerateNiceName(): upgradeName;}


    [ShowInInspector]
    [LabelWidth(125)]
    [VerticalGroup("Details")]
    [TextArea(3, 10)]
    [SerializeField]
    protected string description;

    public void SetNiceName(string niceName)
    {
        //niceUpgradeName = niceName;
    }

    public void Initialize()
    {
        description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";
    }

    public virtual void OnValidate()
    {
        upgradeName = this.name;
    }

    [Button]
    public virtual string GenerateDescription()
    {
        return description;
    }

    public virtual string GenerateNiceName()
    {
        return upgradeName;
    }
}

