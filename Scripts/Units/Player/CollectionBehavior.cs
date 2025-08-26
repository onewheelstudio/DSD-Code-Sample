using DG.Tweening;
using HexGame.Resources;
using HexGame.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HexGame.Resources.ResourceProductionBehavior;

public class CollectionBehavior : UnitBehavior
{
    [SerializeField] private ResourceType resource;
    private List<LootManager.LootData> lootList = new List<LootManager.LootData>();
    [SerializeField] private GameObject lootInstance;
    [SerializeField] private Transform collectionPoint;
    private bool waiting = false;
    private WaitForSeconds processTime = new WaitForSeconds(1f);

    [SerializeField] private BeamEmitter beamEmitter;
    private UnitStorageBehavior usb;

    public static event Action<LootManager.LootData> collected;
    public static event Action<ResourceAmount> terreneStored;

    private void OnEnable()
    {
        usb = this.GetComponent<UnitStorageBehavior>();
        usb.ClearPickupTypes();
        usb.AddPickUpType(resource);
        usb.resourceDelivered += CheckTotals;
        StatsUpgrade.statUpgradeComplete += ApplyStatUpgrade;

        LootManager.lootAdded += LootDropped;
        CollectionBehavior.collected += UpdateLoot;
        EnemySpawnManager.LootLoaded += CheckForLoot;
        DayNightManager.transitionToNight += StopCollecting;
        lootInstance = GameObject.Instantiate(lootInstance); //replace prefab with instance
        lootInstance.SetActive(false);
    }

    private void OnDisable()
    {
        StatsUpgrade.statUpgradeComplete -= ApplyStatUpgrade;
        numberOfWorkers = 0;
        usb.resourceDelivered -= CheckTotals;
        DOTween.Kill(this,true);

        LootManager.lootAdded -= LootDropped;
        CollectionBehavior.collected -= UpdateLoot;
        EnemySpawnManager.LootLoaded -= CheckForLoot;
        DayNightManager.transitionToNight -= StopCollecting;
    }

    public override void StartBehavior()
    {
        isFunctional = requiredNumberOfWorkers <= numberOfWorkers;
        DisplayWarning();
    }

    public override void StopBehavior()
    {
        this.isFunctional = false;
        StopAllCoroutines();
        DOTween.KillAll();
    }

    private void CheckTotals(UnitStorageBehavior behavior, ResourceAmount resource)
    {
        if (resource.type != ResourceType.Workers)
            return;
            
        WorkersReceived(resource);
    }

    private void WorkersReceived(ResourceAmount resource)
    {
        numberOfWorkers += resource.amount;
        isFunctional = requiredNumberOfWorkers <= numberOfWorkers;
        DisplayWarning();
        CheckForLoot();
        StartCoroutine(CollectLoot());
    }

    private void DisplayWarning()
    {
        List<ProductionIssue> issueList = new List<ProductionIssue>();
        if (requiredNumberOfWorkers > numberOfWorkers)
            issueList.Add(ProductionIssue.missingWorkers);
        else if (numberOfWorkers == 0)
            issueList.Add(ProductionIssue.noWorkers);

        if(!usb.CanStoreForPickup(new ResourceAmount(ResourceType.Terrene, 1)))
            issueList.Add(ProductionIssue.fullStorage);

        if (issueList.Count == 0 && hasWarningIcon)
        {
            warningIconInstance.ToggleIconsOff();
            return;
        }

        if (warningIconInstance == null)
        {
            warningIconInstance = UnitManager.warningIcons.PullGameObject(this.transform.position, Quaternion.identity).GetComponent<WarningIcons>();
            warningIconInstance.transform.SetParent(this.transform);
        }

        warningIconInstance.SetWarnings(issueList);
    }

    private IEnumerator CollectLoot()
    {

        while(isFunctional)
        {
            if(!DayNightManager.isDay)
            {
                yield return null;
                continue;
            }

            WaitForSeconds wait = new WaitForSeconds(3f / GameConstants.GameSpeed);
            DisplayWarning();

            if (!usb.CanStoreForPickup(new ResourceAmount(resource, 1)))
            {
                yield return wait;
                continue;
            }

            if (lootList != null && lootList.Count > 0)
            {
                LootManager.LootData loot = lootList.PullFirst();
                if (loot == null || loot.isCollected)
                {
                    yield return null;
                    continue;
                }

                float time = (loot.position - collectionPoint.position).magnitude / GetStat(Stat.speed);
                lootInstance.transform.position = loot.position;
                lootInstance.SetActive(true);
                collected?.Invoke(loot);
                 
                beamEmitter.SetTarget(lootInstance.transform);
                beamEmitter.gameObject.SetActive(true);

                Tween tween = lootInstance.transform.DOMove(collectionPoint.position, time / GameConstants.GameSpeed)
                                            .SetEase(Ease.Linear)
                                            .OnComplete(() => LootCollected(loot));

                yield return tween.WaitForCompletion();
            }

            yield return wait;
        }
    }

    private void StopCollecting(int dayNumber, float delay)
    {
        lootList.Clear();
    }

    //used to get loot when building is placed
    private void CheckForLoot()
    {
        lootList = FindFirstObjectByType<LootManager>().GetNearbyLoot(this.transform.position, GetStat(Stat.maxRange));
    }
    private void LootDropped(LootManager.LootData data)
    {
        if (HelperFunctions.HexRangeFloat(data.position, this.transform.position) <= GetStat(Stat.maxRange))
        {
            lootList.Add(data);
        }
    }

    private void LootCollected(LootManager.LootData loot)
    {
        lootInstance.gameObject.SetActive(false);
        beamEmitter.gameObject.SetActive(false);
        StartCoroutine(ProcessLoot(loot));
    }

    private IEnumerator ProcessLoot(LootManager.LootData loot)
    {
        yield return processTime;
        usb.StoreResource(new ResourceAmount(resource, 1));
        terreneStored?.Invoke(new ResourceAmount(ResourceType.Terrene, 1));
    }

    private void ApplyStatUpgrade(PlayerUnitType type, StatsUpgrade upgrade)
    {
        if(upgrade.upgradeToApply.ContainsKey(Stat.maxRange))
            CheckForLoot();
    }
    private void UpdateLoot(LootManager.LootData data)
    {
        if (lootList.Contains(data))
            lootList.Remove(data);
    }
}
