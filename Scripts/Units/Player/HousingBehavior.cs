using HexGame.Resources;
using HexGame.Units;
using System;
using static HexGame.Resources.ResourceProductionBehavior;
using System.Collections.Generic;
using UnityEngine;

public class HousingBehavior : UnitBehavior, IHaveHappiness
{
    public static event Action<HousingBehavior> housingAdded;
    public static event Action<HousingBehavior> housingRemoved;
    private UnitStorageBehavior usb;
    private WarningIcons warningIcons;
    private bool hasFood = true;
    private bool hasWater = true;

    private void Awake()
    {
        usb = this.GetComponent<UnitStorageBehavior>();
        warningIcons = this.GetComponentInChildren<WarningIcons>();
    }

    public override void StartBehavior()
    {
        isFunctional = true;
        housingAdded?.Invoke(this);
        WorkerManager.AddHappyBuilding(this, this.GetComponent<PlayerUnit>());
        usb.CheckResourceLevels();
        usb.resourceDelivered += DisplayWarning;
        usb.AddDeliverType(ResourceType.Food);
        usb.AddDeliverType(ResourceType.Water);
        DisplayWarning();
    }

    public override void StopBehavior()
    {
        isFunctional = false;
        housingRemoved?.Invoke(this);
        WorkerManager.RemoveHappyBuilding(this);
        usb.resourceDelivered += DisplayWarning;
    }

    public int GetHappiness()
    {
        return GetIntStat(Stat.happiness);
    }

    public string GetHappinessString()
    {
        return " Location";
    }

    public bool TryConsume(ResourceAmount resource)
    {
        bool consumed = usb.TryUseResource(resource);
        DisplayWarning();

        return consumed;
    }

    private void DisplayWarning(UnitStorageBehavior behavior, ResourceAmount amount)
    {
        DisplayWarning();
    }

    private void DisplayWarning()
    {
        hasFood = usb.GetAmountStored(ResourceType.Food) > 0;
        hasWater = usb.GetAmountStored(ResourceType.Water) > 0;

        if (hasFood && hasWater)
        {
            if(hasWarningIcon)
                warningIconInstance.ToggleIconsOff();
            return; //no warning icon if no food or water
        }

        if (warningIconInstance == null)
        {
            warningIconInstance = UnitManager.warningIcons.PullGameObject(this.transform.position, Quaternion.identity).GetComponent<WarningIcons>();
            warningIconInstance.transform.SetParent(this.transform);
        }

        List<ResourceType> resourceTypes = new List<ResourceType>();
        if (!hasFood)
            resourceTypes.Add(ResourceType.Food);
        if (!hasWater)
            resourceTypes.Add(ResourceType.Water);
        if(!hasFood || !hasWater)
            warningIconInstance.SetResourceWarnings(resourceTypes);
    }
}
