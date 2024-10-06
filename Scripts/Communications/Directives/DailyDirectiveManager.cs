using HexGame.Resources;
using HexGame.Units;
using Sirenix.OdinInspector;
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
    private bool playedNewDirectiveCommunication = false;

    private void Awake()
    {
        directiveMenu = FindObjectOfType<DirectiveMenu>();
        playerResources = FindObjectOfType<PlayerResources>();
        supplyShipManager = FindObjectOfType<SupplyShipManager>();
        stockMarket = FindObjectOfType<StockMarket>();
    }

    private void OnEnable()
    {
        DayNightManager.toggleDay += AssignDirective;
        UnLockTechTree.unLockTechTree += TechTreeOpen;
    }

    private void OnDisable()
    {
        DayNightManager.toggleDay -= AssignDirective;
        UnLockTechTree.unLockTechTree -= TechTreeOpen;
    }

    private void AssignDirective(int dayNumber)
    {
        if (!assignDirectives)
            return;

        AssignNextDirective();
    }


    [Button]
    private DirectiveQuest AssignNextDirective()
    {
        //is there a back log of quests we want to assign?
        if(directives.Count > 0)
        {
            DirectiveQuest assignedDirective = directives[0];
            assignedDirective.isCorporate = true;
            if(!directiveMenu.TryAddQuest(assignedDirective, true))
                return null;

            directives.RemoveAt(0);
            if (!playedNewDirectiveCommunication)
            {
                GameTipsWindow.AddTip(newDirectiveCommunication);
                playedNewDirectiveCommunication = true;
            }
            SFXManager.PlaySFX(SFXType.DirectiveAdded, true);
            MessagePanel.ShowMessage("New Corporate Directive Assigned", null);
            return assignedDirective;
        }

        if (HexTileManager.GetNextFloat() < chanceForEnemyQuest)
            return GetEnemyBasedQuest();
        else
            return GetResourceBasedQuest();
    }

    private DirectiveQuest GetResourceBasedQuest()
    {
        int shipCount = supplyShipManager.SupplyShipCount;
        int minLoads = Mathf.CeilToInt(shipCount / 2f);
        int maxLoads = Mathf.FloorToInt(shipCount * 1.49f);
        int loadsToShip = HexTileManager.GetNextInt(minLoads, maxLoads);
        float price = 0;

        DirectiveQuest quest = ScriptableObject.CreateInstance<DirectiveQuest>();
        quest.buyOrSell = RequestType.sell;

        List<ResourceType> possibleResources = PlayerResources.producedResources.Select(x => x.type).ToList();
        if (possibleResources.Count == 0)
            return null;

        List<ResourceAmount> resourcesToShip = new List<ResourceAmount>();
        for (int i = 0; i < loadsToShip; i++)
        {
            ResourceType resource = possibleResources[HexTileManager.GetNextInt(0, possibleResources.Count)];
            resourcesToShip.Add(new ResourceAmount(resource, 50));
            price += stockMarket.GetResourcePrice(resource) * 50;
            stockMarket.SellResource(resource, 50);
        }

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

    private DirectiveQuest GetEnemyBasedQuest()
    {
        int shipCount = supplyShipManager.SupplyShipCount;
        int minLoads = Mathf.CeilToInt(shipCount / 2f);
        int maxLoads = Mathf.FloorToInt(shipCount * 1.49f);
        int loadsToShip = HexTileManager.GetNextInt(minLoads, maxLoads);
        EnemyUnitType enemyUnitType = EnemyUnitType.serpent; //make better to include other types

        DirectiveQuest quest = ScriptableObject.CreateInstance<DirectiveQuest>();

        if (PlayerNeedsWorkers() && HexTileManager.GetNextFloat() < chanceForWorkerReward)
        {
            quest.useResourceReward = true;
            quest.SetResourceReward(new ResourceAmount(ResourceType.Workers, 5 * loadsToShip));
            quest.Setup(loadsToShip * 50, enemyUnitType, loadsToShip * 10, 0);
        }
        else
            quest.Setup(loadsToShip * 50, enemyUnitType, loadsToShip * 10, loadsToShip * 250);

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
}
