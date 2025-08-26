using HexGame.Resources;
using HexGame.Units;
using OWS.ObjectPooling;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourcePickupBehavior : UnitBehavior, IPoolable<ResourcePickupBehavior>
{
    [SerializeField] private List<CargoCube> resourceDrops = new();
    private UnitStorageBehavior usb;
    private Action<ResourcePickupBehavior> returnToPool;

    private void Awake()
    {
        this.usb = this.GetComponent<UnitStorageBehavior>();
    }

    private void OnEnable()
    {
        resourceDrops.Clear();
        usb.resourcePickedUp += ResourcePickedUp;
    }

    private void OnDisable()
    {
        usb.resourcePickedUp -= ResourcePickedUp;
    }

    public override void StartBehavior()
    {
        isFunctional = true;
        usb.UpdateStoredPosition();
    }

    public override void StopBehavior()
    {
        isFunctional = false;
    }

    public void AddResourcePickup(CargoCube cargoCube)
    {
        if (cargoCube == null || resourceDrops.Contains(cargoCube))
            return;

        resourceDrops.Add(cargoCube);
        var resource = new ResourceAmount(cargoCube.cargoType, 5);
        usb.AddPickUpType(cargoCube.cargoType);
        usb.AddResourceForPickup(resource);
    }

    private void ResourcePickedUp(UnitStorageBehavior behavior, ResourceAmount amount)
    {
        for (int i = 0; i < resourceDrops.Count; i++)
        {
            CargoCube cube = resourceDrops[i];
            if (cube.cargoType == amount.type && cube.gameObject.activeSelf)
            {
                cube.gameObject.SetActive(false);
                resourceDrops.RemoveAt(i);
                break;
            }
        }

        if (resourceDrops.Count == 0)
            this.gameObject.SetActive(false);
    }

    public void Initialize(Action<ResourcePickupBehavior> returnAction)
    {
        //cache reference to return action
        this.returnToPool = returnAction;
    }

    public void ReturnToPool()
    {
        //invoke and return this object to pool
        returnToPool?.Invoke(this);
    }
}
