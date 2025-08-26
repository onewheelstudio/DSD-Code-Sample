using HexGame.Resources;
using HexGame.Units;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DailyDirectiveManager : MonoBehaviour
{
    [SerializeField] private List<DirectiveQuest> directives = new List<DirectiveQuest>();
    private bool assignDirectives = false;
    private DirectiveMenu directiveMenu;
    private PlayerResources playerResources;
    private SupplyShipManager supplyShipManager;
    private StockMarket stockMarket;
    [SerializeField] private float chanceForWorkerReward = 0.5f;
    [SerializeField] private float chanceForEnemyQuest = 0.25f;

    [SerializeField] private TipCommunication newDirectiveCommunication;
    private bool showedNewDirectiveCommunication = false;
    private bool firstDirectiveComplete = false;

    [Header("Upgrade Quests")]
    [SerializeField] private List<Upgrade> upgradesToUnlock = new();

    private void Awake()
    {
        directiveMenu = FindObjectOfType<DirectiveMenu>();
        playerResources = FindObjectOfType<PlayerResources>();
        supplyShipManager = FindObjectOfType<SupplyShipManager>();
        stockMarket = FindObjectOfType<StockMarket>();

        CheatCodes.AddButton(() => AssignNextDirective(), "Next Daily Directive");
    }

    private void OnEnable()
    {
        DayNightManager.toggleDay += AssignDirective;
        UnLockTechTree.unLockTechTree += TechTreeOpen;
        UpgradeTile.upgradePurchased += UpgradePurchased;
    }

    private void OnDisable()
    {
        DayNightManager.toggleDay -= AssignDirective;
        UnLockTechTree.unLockTechTree -= TechTreeOpen;
        UpgradeTile.upgradePurchased -= UpgradePurchased;
    }

    private void AssignDirective(int dayNumber)
    {
        if(dayNumber > 1 && StateOfTheGame.tutorialSkipped)
            assignDirectives = true;

        if (!assignDirectives)
            return;

        AssignNextDirective();
    }

    [Button]
    private DirectiveQuest AssignNextDirective()
    {
        //is there a back log of quests we want to assign?
        if(directives.Count > 0 && !StateOfTheGame.tutorialSkipped)
        {
            if (!showedNewDirectiveCommunication)
            {
                DirectiveQuest nextDirective = directives[0];
                if (!directiveMenu.TryAddQuest(nextDirective, true))
                    return null;

                directives.RemoveAt(0);
                nextDirective.isCorporate = true;
                nextDirective.Completed += FirstDailyDirectiveComplete;

                GameTipsWindow.AddTip(newDirectiveCommunication);
                showedNewDirectiveCommunication = true;

                return null;
            }

            if (!firstDirectiveComplete)
                return null;

            DirectiveQuest assignedDirective = directives[0];
            assignedDirective.isCorporate = true;
            if(!directiveMenu.TryAddQuest(assignedDirective, true))
                return null;

            directives.RemoveAt(0);

            SFXManager.PlaySFX(SFXType.DirectiveAdded, true);
            MessagePanel.ShowMessage("New Corporate Directive Assigned", null);
            return assignedDirective;
        }

        float roll = HexTileManager.GetNextFloat(0,1);

        if (roll > 0.85f && HexTechTree.AnyUpgradesToUnlock())
            return AssignUpgradeQuest();
        else if (roll > 0.5f)
            return AssignResourceBasedQuest();
        else if (roll > 0.3f)
            return AssignSellShipmentQuest();
        else
            return AssignEnemyBasedQuest();
    }

    [Button]
    private DirectiveQuest AssignSellShipmentQuest()
    {
        int shipCount = supplyShipManager.SupplyShipCount;
        //still assign a directive if there are no ships and tutorial has been skipped
        if (shipCount == 0 && StateOfTheGame.tutorialSkipped)
            shipCount = 1;

        int minLoads = Mathf.CeilToInt(shipCount / 2f);
        int maxLoads = Mathf.FloorToInt(shipCount * 1.49f);
        int loadsToShip = HexTileManager.GetNextInt(minLoads, maxLoads);

        if (loadsToShip == 0) //could this possibly happen?
            return null;

        SellResourceDirective quest = ScriptableObject.CreateInstance<SellResourceDirective>();
        quest.SetLoadsToSell(loadsToShip);
        quest.Setup(loadsToShip * 100, loadsToShip * 100);

        quest.isCorporate = true;
        directiveMenu.TryAddQuest(quest, true);
        quest.name = $"Sell {loadsToShip} Supply Ship Loads";

        SFXManager.PlaySFX(SFXType.DirectiveAdded, true);
        MessagePanel.ShowMessage("New Corporate Directive Assigned", null);

        return quest;
    }

    private void FirstDailyDirectiveComplete(DirectiveQuest quest)
    {
        quest.Completed -= FirstDailyDirectiveComplete;
        firstDirectiveComplete = true;
    }

    private DirectiveQuest AssignUpgradeQuest()
    {
        DirectiveQuest upgradeQuest = null;
        upgradeQuest = ScriptableObject.CreateInstance<UnlockAnyUpgradeQuest>();
        upgradeQuest.Setup(100, 500);
        upgradeQuest.name = "Unlock Any Upgrade";
        upgradeQuest.isCorporate = true;

        if (!directiveMenu.TryAddQuest(upgradeQuest, true))
            return null;

        SFXManager.PlaySFX(SFXType.DirectiveAdded, true);
        MessagePanel.ShowMessage("New Corporate Directive Assigned", null);

        return upgradeQuest;
    }


    private DirectiveQuest AssignResourceBasedQuest()
    {
        int shipCount = supplyShipManager.SupplyShipCount;
        //still assign a directive if there are no ships and tutorial has been skipped
        if (shipCount == 0 && StateOfTheGame.tutorialSkipped)
            shipCount = 1;
        int minLoads = Mathf.CeilToInt(shipCount / 4f);
        int maxLoads = Mathf.CeilToInt(shipCount / 2f);
        int loadsToShip = HexTileManager.GetNextInt(minLoads, maxLoads);
        float price = 0;

        DirectiveQuest quest = ScriptableObject.CreateInstance<DirectiveQuest>();
        quest.requestType = RequestType.sell;

        List<ResourceType> possibleResources = PlayerResources.producedResources.Select(x => x.type)
                                                                                .Where(r => r != ResourceType.Terrene)
                                                                                .ToList();
        if (possibleResources.Count == 0)
            return null;

        List<ResourceAmount> resourcesToShip = new List<ResourceAmount>();

        ResourceType resource = possibleResources[HexTileManager.GetNextInt(0, possibleResources.Count)];
        resourcesToShip.Add(new ResourceAmount(resource, SupplyShipManager.supplyShipCapacity * loadsToShip));

        price += stockMarket.GetResourcePrice(resource) * SupplyShipManager.supplyShipCapacity * loadsToShip;
        stockMarket.SellResource(resource, SupplyShipManager.supplyShipCapacity * loadsToShip);
        quest.name = $"Sell {loadsToShip * SupplyShipManager.supplyShipCapacity} {resource.ToNiceString()}";

        if (PlayerNeedsWorkers() && HexTileManager.GetNextFloat() < chanceForWorkerReward)
        {
            quest.useResourceReward = true;
            quest.SetResourceReward(new ResourceAmount(ResourceType.Workers, 5 * loadsToShip));
            quest.Setup(resourcesToShip, 100 * loadsToShip, Mathf.RoundToInt(price));
        }
        else
            quest.Setup(resourcesToShip, 100 * loadsToShip, Mathf.RoundToInt(price));

        
        quest.isCorporate = true;
        directiveMenu.TryAddQuest(quest, true);

        SFXManager.PlaySFX(SFXType.DirectiveAdded, true);
        MessagePanel.ShowMessage("New Corporate Directive Assigned", null);

        return quest;
    }

    private DirectiveQuest AssignEnemyBasedQuest()
    {
        int shipCount = supplyShipManager.SupplyShipCount;
        if (shipCount == 0 && StateOfTheGame.tutorialSkipped)
            shipCount = 1; 
        int minLoads = Mathf.CeilToInt(shipCount / 2f);
        int maxLoads = Mathf.FloorToInt(shipCount * 1.49f);
        int loadsToShip = HexTileManager.GetNextInt(minLoads, maxLoads);
        EnemyUnitType enemyUnitType = EnemyUnitType.serpent; //make better to include other types

        DirectiveQuest quest = ScriptableObject.CreateInstance<DirectiveQuest>();

        if (PlayerNeedsWorkers() && HexTileManager.GetNextFloat() < chanceForWorkerReward)
        {
            quest.useResourceReward = true;
            quest.SetResourceReward(new ResourceAmount(ResourceType.Workers, 5 * loadsToShip));
            quest.Setup(loadsToShip * SupplyShipManager.supplyShipCapacity, enemyUnitType, loadsToShip * 100, 0);
        }
        else
            quest.Setup(loadsToShip * SupplyShipManager.supplyShipCapacity, enemyUnitType, loadsToShip * 100, loadsToShip * 250);

        quest.name = $"Kill {loadsToShip * SupplyShipManager.supplyShipCapacity} {enemyUnitType.ToNiceString()}";
        quest.isCorporate = true;
        directiveMenu.TryAddQuest(quest, true);
        SFXManager.PlaySFX(SFXType.DirectiveAdded, true);
        MessagePanel.ShowMessage("New Corporate Directive Assigned", null);

        return quest;
    }

    private void TechTreeOpen()
    {
        assignDirectives = true;
        UnlockStockMarketButton.unlockStockMarketButton -= TechTreeOpen;
        SFXManager.PlaySFX(SFXType.DirectiveAdded, true);

        if(DayNightManager.secondRemaining > 30 && DayNightManager.DayNumber > 0)
            AssignNextDirective();
    }

    private bool PlayerNeedsWorkers()
    {
        return PlayerResources.GetAmountStored(ResourceType.Workers) < 5;
    }

    [Button]
    private void RefreshDirectives()
    {
        string path = "Assets/Prefabs/Communications/Directives/Quests";
        directives = HelperFunctions.GetScriptableObjects<DirectiveQuest>(path);
    }

    private void UpgradePurchased(UpgradeTile tile)
    {
        if (upgradesToUnlock.Contains(tile.upgrade))
            upgradesToUnlock.Remove(tile.upgrade);
    }

    private bool CheckForUpgradeQuest()
    {
        return upgradesToUnlock.Count > 0;
    }




}
