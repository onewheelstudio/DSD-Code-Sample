using DG.Tweening;
using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static HexGame.Resources.ResourceProductionBehavior;

public class CollectionBehavior : UnitBehavior
{
    [SerializeField] private ResourceType resource;
    private Queue<ResourcePickup> pickupList = new Queue<ResourcePickup>();
    [SerializeField] private Transform collectionPoint;
    private bool waiting = false;
    private WaitForSeconds processTime = new WaitForSeconds(1f);

    [SerializeField] private BeamEmitter beamEmitter;
    private UnitStorageBehavior usb;
    private int requiredNumberOfWorkers => (int)this.GetStat(Stat.workers);
    private int numberOfWorkers;

    public static event Action<ResourcePickup> collected;
    public static event Action<ResourceAmount> terreneStored;

    private void OnEnable()
    {
        ResourcePickup.pickUpCreated += TryCollectLoot;
        usb = this.GetComponent<UnitStorageBehavior>();
        usb.SetAllowedTypes(new List<ResourceType>() { resource });
        usb.resourceDelivered += CheckTotals;
        StatsUpgrade.statUpgradeComplete += ApplyStatUpgrade;
    }


    private void OnDisable()
    {
        ResourcePickup.pickUpCreated -= TryCollectLoot;
        StatsUpgrade.statUpgradeComplete -= ApplyStatUpgrade;
        numberOfWorkers = 0;
        usb.resourceDelivered -= CheckTotals;
        DOTween.Kill(this,true);
    }

    public override void StartBehavior()
    {
        isFunctional = requiredNumberOfWorkers <= numberOfWorkers;
        usb.RequestWorkers();
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

        if(!usb.CanStoreResource(new ResourceAmount(ResourceType.Terrene, 1)))
            issueList.Add(ProductionIssue.fullStorage);

        if (issueList.Count == 0 && hasWarningIcon)
        {
            warningIconInstance.ToggleIconsOff();
            return;
        }

        if (!hasWarningIcon)
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
            WaitForSeconds wait = new WaitForSeconds(3f / GameConstants.GameSpeed);
            DisplayWarning();

            if (!usb.CanStoreResource(new ResourceAmount(resource, 1)))
            {
                terreneStored?.Invoke(new ResourceAmount(ResourceType.Terrene, 1));
                yield return wait;
                continue;
            }

            if (pickupList != null && pickupList.Count > 0)
            {
                ResourcePickup loot = pickupList.Dequeue();
                if (loot == null || loot.inUse)
                {
                    yield return null;
                    continue;
                }
                loot.inUse = true;
                float time = (loot.transform.position - collectionPoint.position).magnitude / GetStat(Stat.speed);
                 
                beamEmitter.SetTarget(loot.transform);
                beamEmitter.gameObject.SetActive(true);

                Tween tween = loot.transform.DOMove(collectionPoint.position, time / GameConstants.GameSpeed)
                                            .SetEase(Ease.Linear)
                                            .OnComplete(() => LootCollected(loot));

                yield return tween.WaitForCompletion();
                collected?.Invoke(loot);
            }

            yield return wait;
        }
    }

    //used to get loot when building is placed
    private void CheckForLoot()
    {
        FindObjectsOfType<ResourcePickup>().ForEach(r => TryCollectLoot(r));  
    }

    private void TryCollectLoot(ResourcePickup resourcePickup)
    {
        if(resourcePickup.resourceType == this.resource
            && HelperFunctions.HexRangeFloat(resourcePickup.transform.position, this.transform.position) <= GetStat(Stat.maxRange))
        {
            pickupList.Enqueue(resourcePickup);
        }
        else
        {

        }
    }

    private void LootCollected(ResourcePickup loot)
    {
        loot.gameObject.SetActive(false);
        beamEmitter.gameObject.SetActive(false);
        StartCoroutine(ProcessLoot(loot));
    }

    private IEnumerator ProcessLoot(ResourcePickup loot)
    {
        for (int i = 0; i < loot.amount; i++)
        {
            yield return processTime;
            usb.StoreResource(new ResourceAmount(resource, 1));
        }
    }

    private void ApplyStatUpgrade(PlayerUnitType type, StatsUpgrade upgrade)
    {
        if(upgrade.upgradeToApply.ContainsKey(Stat.maxRange))
            CheckForLoot();
    }
}
