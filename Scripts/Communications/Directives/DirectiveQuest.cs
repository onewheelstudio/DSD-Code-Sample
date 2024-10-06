using HexGame.Resources;
using HexGame.Units;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New Directive Quest", menuName = "Hex/Directives/Directive Quest")]
public class DirectiveQuest : DirectiveBase, IEqualityComparer<DirectiveQuest>
{
    [TextArea(3, 10)]
    public string description;

    [Header("Requirements")]
    [SerializeField] private List<ResourceAmount> requiredResources;
    private List<ResourceAmount> collectedResources = new List<ResourceAmount>();
    [SerializeField] private List<BuildingRequirement> requiredBuildings;
    [SerializeField] private List<EnemyRequirement> enemyRequirements = new List<EnemyRequirement>();
    public bool requiresResources => requiredResources != null && requiredResources.Count > 0;
    public bool requiresBuilding => requiredBuildings != null && requiredBuildings.Count > 0;
    public bool requiresEnemies => enemyRequirements != null && enemyRequirements.Count > 0;    
    public bool allowAlreadyBuiltUnits = false;

    [Header("Rewards")]
    public bool useBuildingReward = false;
    [SerializeField, ShowIf("useBuildingReward")]private PlayerUnitType buildingReward;
    public bool useResourceReward = false;
    [SerializeField, ShowIf("useResourceReward")] private List<ResourceAmount> rewardResources;
    public bool useRepReward = false;
    [SerializeField, ShowIf("useRepReward")] private QuestReward questReward;

    [SerializeField, Min(1),InfoBox("Time in Minutes")] private int timeLimit = 5;
    public int TimeLimit => timeLimit;
    public int TimeLimitSeconds => timeLimit * 60;
    [SerializeField] private bool useTimeLimit = false;
    public bool UseTimeLimit => useTimeLimit;

    [NonSerialized] public bool completed = false;
    private float unlockedTime;
    public float UnlockedTime => unlockedTime;
    [NonSerialized] private float lastUsedTime;
    public float LastUsedTime => lastUsedTime;
    [SerializeField]
    private bool isUnlocked = false;
    public bool IsUnlocked => isUnlocked;

    public static event Action<DirectiveQuest> questCompleted;
    public static event Action<DirectiveQuest> questFailed;
    public RequestType buyOrSell = RequestType.sell;
    public bool isCorporate = false;

    public void Setup(List<ResourceAmount> resources, int repReward, int techCreditsReward)
    {
        requiredResources = resources;
        questReward = new QuestReward(repReward, techCreditsReward);
        useRepReward = true;
    }
    public void Setup(int enemiesToKill, EnemyUnitType typeToKill, int repReward, int techCreditsReward)
    {
        enemyRequirements = new List<EnemyRequirement> { new EnemyRequirement(typeToKill, enemiesToKill) };
        questReward = new QuestReward(repReward, techCreditsReward);
        useRepReward = true;
    }

    public override List<string> DisplayText()
    {
        List<string> displayText = new List<string>();
        if (requiredResources != null)
        {
            foreach (ResourceAmount resource in requiredResources)
            {
                int amountCollected = 0;
                if (collectedResources.Any(r => r.type == resource.type))
                    amountCollected = collectedResources.First(r => r.type == resource.type).amount;

                if(buyOrSell == RequestType.sell)
                    displayText.Add($"Export {resource.type.ToNiceString()}: {amountCollected}/{resource.amount}");
                else
                    displayText.Add($"Import {resource.type.ToNiceString()}: {amountCollected}/{resource.amount}");
            }
        }

        if (requiredBuildings != null)
        {
            foreach (BuildingRequirement building in requiredBuildings)
                displayText.Add($"Build {building.unitType.ToNiceString()}: {Mathf.Min(building.numberBuilt, building.totalToBuild)}/{building.totalToBuild}");
        }

        if (enemyRequirements != null)
        {
            foreach (EnemyRequirement enemy in enemyRequirements)
                displayText.Add($"Destroy {enemy.enemyType.ToNiceString()}: {Mathf.Min(enemy.currentAmount, enemy.requiredAmount)}/{enemy.requiredAmount}");
        }

        return displayText;
    }

    public override void Initialize()
    {
        if(collectedResources != null)
            collectedResources.Clear();


        if (requiredResources != null && requiredResources.Count > 0)
        {
            if(buyOrSell == RequestType.sell)
                SupplyShipBehavior.resourceReceived += ResourceReceived;
            else
                SupplyShipBehavior.resourcePickedUp += ResourceReceived;

        }
        
        if(requiredBuildings != null && requiredBuildings.Count > 0)
        {
            UnitManager.unitPlaced += UnitCreated;
            Unit.unitRemoved += UnitRemoved;
            requiredBuildings.ForEach(br => br.numberBuilt = 0);
            CheckAlreadyBuilt();
        }
        
        if (enemyRequirements != null && enemyRequirements.Count > 0)
        {
            EnemySubUnit.subUnitDied += EnemyKilled;
            enemyRequirements.ForEach(e => e.currentAmount = 0);
        }

        CommunicationMenu.AddCommunication(OnStartCommunication);
        completed = ES3.Load<bool>($"Quest:{this.name}", GameConstants.questInfo, false);
        lastUsedTime = Time.realtimeSinceStartup;
    }

    private void EnemyKilled(EnemyUnitType unitType)
    {
        if(enemyRequirements.Any(e => e.enemyType == unitType))
        {
            EnemyRequirement enemy = enemyRequirements.First(e => e.enemyType == unitType);
            enemy.currentAmount++;

            int index = enemyRequirements.FindIndex(e => e.enemyType == unitType);
            enemyRequirements[index] = enemy;
        }
        else
            enemyRequirements.Add(new EnemyRequirement(unitType, 1));

        DirectiveUpdated();
    }

    private void ResourceReceived(ResourceAmount amount, SubRequest subRequest)
    {
        if (subRequest.quest != this)
            return;

        if(collectedResources.Any(r => r.type == amount.type))
        {
            ResourceAmount collectedResource = collectedResources.First(r => r.type == amount.type);
            collectedResource.amount += amount.amount;

            int index = collectedResources.FindIndex(r => r.type == amount.type);
            collectedResources[index] = collectedResource;
        }
        else
            collectedResources.Add(amount);
        DirectiveUpdated();
    }

    public override List<bool> IsComplete()
    {
        List<bool> isComplete = new List<bool>();
        if (requiredResources != null)
        {
            foreach (ResourceAmount resource in requiredResources)
            {
                int amountCollected = 0;
                if (collectedResources.Any(r => r.type == resource.type))
                    amountCollected = collectedResources.First(r => r.type == resource.type).amount;

                isComplete.Add(amountCollected >= resource.amount);
            }
        }

        //.fix this logic the number built is stored in BuildingRequirement
        if (requiredBuildings != null)
        {
            foreach (BuildingRequirement building in requiredBuildings)
            {
                isComplete.Add(building.numberBuilt >= building.totalToBuild);
            }
        }

        if (enemyRequirements != null)
        {
            foreach (EnemyRequirement enemy in enemyRequirements)
            {
                isComplete.Add(enemy.currentAmount >= enemy.requiredAmount);
            }
        }

        ES3.Save<bool>($"Quest:{description}", isComplete.All(x => x == true), GameConstants.questInfo);

        return isComplete;
    }

    public override void OnComplete()
    {
        SupplyShipBehavior.resourceReceived -= ResourceReceived;
        CommunicationMenu.AddCommunication(OnCompleteCommunication);
        if(OnCompleteTrigger != null && OnCompleteTrigger.Count > 0)
            OnCompleteTrigger.ForEach(t => t.DoTrigger());
        questCompleted?.Invoke(this);

        if (useResourceReward)
        {
            PlayerResources pr = FindObjectOfType<PlayerResources>();
            foreach (var resource in rewardResources)
            {
                if(resource.type == ResourceType.Workers)
                {
                    WorkerManager.WorkersAdded(resource.amount);
                    MessagePanel.ShowMessage($"Quest Reward: {resource.amount} colonists have arrived.", null);
                }
                else
                {
                    pr.AddResource(resource);
                    MessagePanel.ShowMessage($"Quest Reward: {resource.amount} {resource.type.ToNiceString()} delivered.", null);
                }
            }
        }

        if(useBuildingReward)
        {
            BuildMenu bm = FindObjectOfType<BuildMenu>();
            bm.UnLockUnit(buildingReward);
        }

        if(useRepReward)
        {
            //placed above incase "rep tier" increases - which will be tiggered by the next line
            MessagePanel.ShowMessage($"Repution changed by {questReward.repReward}", null);

            ReputationManager.ChangeReputation(questReward.repReward);
            if(this.buyOrSell == RequestType.sell)
                HexTechTree.ChangeTechCredits(questReward.techCreditsReward);

            MessagePanel.ShowMessage($"Quest Reward: {questReward.techCreditsReward} Tech Credits earned.", null)
            .SetAction(FindObjectOfType<HexTechTree>().OpenWindow)
            .SetDisplayTime(20f);
        }

        //clear events
        if (requiredResources != null && requiredResources.Count > 0)
        {
            SupplyShipBehavior.resourceReceived -= ResourceReceived;
            SupplyShipBehavior.resourcePickedUp -= ResourceReceived;
        }
        else if (requiredBuildings != null && requiredBuildings.Count > 0)
        {
            UnitManager.unitPlaced -= UnitCreated;
            Unit.unitRemoved -= UnitRemoved;
        }
        else if (enemyRequirements != null && enemyRequirements.Count > 0)
        {
            EnemySubUnit.subUnitDied -= EnemyKilled;
        }
    }

    [Button]
    public void Failed()
    {
        MessagePanel.ShowMessage($"Quest Failed. Reputation lost.", null);
        ReputationManager.LoseReputation(questReward.repReward);
        questFailed?.Invoke(this);
        GameObject.FindObjectOfType<DirectiveMenu>().RemoveQuest(this);
    }

    [Button]
    private void ResetIsComplete()
    {
        ES3.Save<bool>($"Quest:{this.name}", false, GameConstants.questInfo);
    }

    public bool TryGetRequestedResources(out List<ResourceAmount> resources)
    {
        if(requiredResources != null && requiredResources.Count > 0)
        {
            resources = new List<ResourceAmount>(requiredResources);
            return true;
        }
        else
        {
            resources = null;
            return false;
        }
    }

    public bool TryGetRequestedBuilding(out List<BuildingRequirement> buildings)
    {
        if (requiredBuildings != null && requiredBuildings.Count > 0)
        {
            buildings = new List<BuildingRequirement>(requiredBuildings);
            return true;
        }
        else
        {
            buildings = null;
            return false;
        }
    }

    private void CheckAlreadyBuilt()
    {
        if (!allowAlreadyBuiltUnits)
            return;

        foreach (var br in requiredBuildings)
        {
            br.numberBuilt = UnitManager.playerUnits.Count(u => u.unitType == br.unitType);
        }

        DirectiveUpdated();
    }

    private void UnitRemoved(Unit unit)
    {
        if (unit is PlayerUnit playerUnit)
        {
            if (!requiredBuildings.Any(br => br.unitType == playerUnit.unitType))
                return;

            BuildingRequirement br = requiredBuildings.First(br => br.unitType == playerUnit.unitType);
            br.numberBuilt--;
            DirectiveUpdated();
        }
    }

    private void UnitCreated(Unit unit)
    {
        if (unit is PlayerUnit playerUnit)
        {
            if (!requiredBuildings.Any(br => br.unitType == playerUnit.unitType))
                return;

            BuildingRequirement br = requiredBuildings.First(br => br.unitType == playerUnit.unitType);
            br.numberBuilt++;
            DirectiveUpdated();
        }
    }

    public bool TrGetResourceRewards(out List<ResourceAmount> resources)
    {
        if (rewardResources != null && rewardResources.Count > 0)
        {
            resources = new List<ResourceAmount>(rewardResources);
            return true;
        }
        else
        {
            resources = null;
            return false;
        }
    }

    public bool TryGetBuildingReward(out PlayerUnitType building)
    {
        if (useBuildingReward)
        {
            building = buildingReward;
            return true;
        }
        else
        {
            building = PlayerUnitType.hq;
            return false;
        }
    }

    public bool TryGetRepReward(out QuestReward reward)
    {
        if (useRepReward)
        {
            reward = questReward;
            return true;
        }
        else
        {
            reward = null;
            return false;
        }
    }

    public bool Equals(DirectiveQuest x, DirectiveQuest y)
    {
        if(x == null || y == null)
            return false;

        return x.GetInstanceID() == y.GetInstanceID();
    }

    public int GetHashCode(DirectiveQuest obj)
    {
        return obj.GetHashCode(obj);
    }

    public void Unlock()
    {
        this.unlockedTime = Time.realtimeSinceStartup;
        this.isUnlocked = true;
    }

    internal void SetTimeLimit(float timeInMinutes)
    {
        if(timeInMinutes == 0)
            return;

        this.timeLimit = Mathf.RoundToInt(timeInMinutes);
        this.useTimeLimit = true;
    }

    public void SetResourceReward(ResourceAmount resourceAmount)
    {
        if(rewardResources == null)
            rewardResources = new List<ResourceAmount>();

        rewardResources.Add(resourceAmount);
    }

    public void SetRepReward(int repReward, int techCreditsReward)
    {
        this.questReward = new QuestReward(repReward, techCreditsReward);
    }

    [Button]
    public void AddAsQuest()
    {
        FindObjectOfType<DirectiveMenu>().TryAddQuest(this);
    }
}
