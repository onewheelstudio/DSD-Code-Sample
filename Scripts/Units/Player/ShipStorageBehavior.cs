using HexGame.Resources;
using HexGame.Units;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShipStorageBehavior : UnitStorageBehavior, IStoreResource, ITransportResources, IHaveResources
{
    private SupplyShipBehavior supplyShip;
    private StockMarket stockMarket;
    public static event Action<ResourceAmount> resourceConsumed;
    public static event Action<ResourceAmount> resourceImported;

    private new void Awake()
    {
        this.isSupplyShip = true;
        this.SetRequestPriority(CargoManager.RequestPriority.low);
        this.supplyShip = this.GetComponent<SupplyShipBehavior>();
        this.stockMarket = FindFirstObjectByType<StockMarket>();
        base.Awake();
    }

    private new void OnEnable()
    {
        base.resourceDelivered += OnResourceDelivered;
        base.OnEnable();
    }

    private new void OnDisable()
    {
        base.resourceDelivered -= OnResourceDelivered;
        base.OnDisable();
    }


    public void AddAllowedResource(ResourceType resource)
    {
        deliverTypes.Add(resource);
        pickUpTypes.Add(resource);
        allowedTypes.Add(resource);
    }

    public void RemoveAllowedResource(ResourceType resource)
    {
        deliverTypes.Remove(resource);
        pickUpTypes.Remove(resource);
        allowedTypes.Remove(resource);
    }

    private void OnResourceDelivered(UnitStorageBehavior behavior, ResourceAmount amount)
    {
        if(TotalStored() >= SupplyShipManager.supplyShipCapacity)
        {
            supplyShip.DoLaunch();
        }
    }

    public override bool CanDeliverResource(ResourceAmount deliver)
    {
        if(!CanAcceptDeliver() || !supplyShip.ReadyToLaunch)
            return false;

        return base.CanDeliverResource(deliver);
    }

    public override bool CanPickupResource(ResourceAmount pickUp)
    {
        if (!supplyShip.ReadyToLaunch)
            return false;

        if (GetResourceTotal(pickUp.type) >= pickUp.amount)
            return true;
        else
            return false;
    }

    private bool CanAcceptDeliver()
    {
        return GetTotalResources() < SupplyShipManager.supplyShipCapacity;
    }
    public int GetTotalResources()
    {
        int total = 0;
        //lock(listLock)
        //{
            for (int i = 0; i < resourceStored.Count; i++)
            {
                ResourceAmount resource = resourceStored[i];
                total += resource.amount;
            }

            for (int i = 0; i < resourceInTransit.Count; i++)
            {
                ResourceAmount resource = resourceInTransit[i];
                total += resource.amount;
            }
        //}

        return total;
    }


    public override int GetStorageCapacity()
    {
        return SupplyShipManager.supplyShipCapacity;
    }

    internal void SellResource(ResourceAmount resourceAmount)
    {
        if (this.TryUseResource(resourceAmount))
        {
            stockMarket.SellResource(resourceAmount);
            resourceConsumed?.Invoke(resourceAmount);
        }
    }


    public void BuyResource(ResourceAmount resourceToBuy)
    {
        AddResourceForPickup(resourceToBuy);
        resourceImported?.Invoke(resourceToBuy);
    }

    public new List<PopUpResourceAmount> GetPopUpResources()
    {
        List<PopUpResourceAmount> resourceInfos = new List<PopUpResourceAmount>();
        foreach (var resource in resourceStored)
        {
            if (resource.type == ResourceType.Workers)
                continue;
            else if (!deliverTypes.Contains(resource.type) && !pickUpTypes.Contains(resource.type))
                resourceInfos.Add(new PopUpResourceAmount(resource, resource.amount, 0, Color.white));
            else
                resourceInfos.Add(new PopUpResourceAmount(resource, GetResourceStorageLimit(resource), 0, Color.white));
        }

        //trying to get the case where the building needs workers but doesn't have any yet
        if (GetStat(Stat.workers) > 0)
        {
            int workerCount = GetAmountStored(ResourceType.Workers);
            ResourceAmount workers = new ResourceAmount(ResourceType.Workers, workerCount);
            resourceInfos.Add(new PopUpResourceAmount(workers, GetStat(Stat.workers), 0, Color.white));
        }

        foreach (var resource in pickUpTypes.Union(deliverTypes))
        {
            if (!ListContainsResource(resourceStored, resource))
                resourceInfos.Add(new PopUpResourceAmount(new ResourceAmount(resource, 0), GetResourceStorageLimit(resource), 0, Color.white));
        }

        return resourceInfos;
    }
}
