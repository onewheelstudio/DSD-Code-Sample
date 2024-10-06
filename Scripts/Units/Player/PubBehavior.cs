using HexGame.Resources;
using HexGame.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HexGame.Resources.ResourceProductionBehavior;

public class PubBehavior : UnitBehavior, IHaveHappiness
{
    private UnitStorageBehavior usb;

    private int requiredNumberOfWorkers => (int)this.GetStat(Stat.workers);
    private int numberOfWorkers;
    [SerializeField] private float foodConsumptionDelay = 15f;
    private WaitForSeconds delay;
    public static event Action HappinessChanged;
    private bool hasFood = false;

    private void Awake()
    {
        delay = new WaitForSeconds(foodConsumptionDelay);
    }

    private void OnEnable()
    {
        usb = GetComponent<UnitStorageBehavior>();
        usb.resourceDelivered += OnResourceDelivered;
    }

    private void OnDisable()
    {
        usb.resourceDelivered -= OnResourceDelivered;
    }

    private void OnResourceDelivered(UnitStorageBehavior behavior, ResourceAmount resource)
    {
        if(resource.type == ResourceType.Workers)
        {
            numberOfWorkers += resource.amount;
            isFunctional = numberOfWorkers > 0;
        }

        usb.CheckResourceLevels();
        DisplayWarning();
    }

    public override void StartBehavior()
    {
        DisplayWarning();
        usb.CheckResourceLevels();
        StartCoroutine(ConsumeFood());
        isFunctional = numberOfWorkers > 0;
        WorkerManager.AddHappyBuilding(this, this.GetComponent<PlayerUnit>());
    }

    public override void StopBehavior()
    {
        isFunctional = false;
        StopAllCoroutines();
        WorkerManager.RemoveHappyBuilding(this);
    }

    private void DisplayWarning()
    {
        List<ProductionIssue> issueList = new List<ProductionIssue>();
        if (requiredNumberOfWorkers > numberOfWorkers)
            issueList.Add(ProductionIssue.missingWorkers);
        else if (numberOfWorkers == 0)
            issueList.Add(ProductionIssue.noWorkers);

        if (!usb.HasResource(new ResourceAmount(ResourceType.Food,1)))
            issueList.Add(ProductionIssue.missingResources);

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

    private IEnumerator ConsumeFood()
    {
        while(true)
        {
            yield return new WaitUntil(() => DayNightManager.isDay && isFunctional);

            if(usb.HasResource(new ResourceAmount(ResourceType.Food, 1)))
            {
                usb.TryUseResource(new ResourceAmount(ResourceType.Food, 1));
                DisplayWarning();

                if(!hasFood)
                {
                    hasFood = true;
                    HappinessChanged?.Invoke();
                }
            }
            else
            {
                if(hasFood)
                {
                    hasFood = false;
                    HappinessChanged?.Invoke();
                }
            }

            usb.CheckResourceLevels();
            yield return delay;
        }
    }

    public int GetHappiness()
    {
        if(!isFunctional || hasFood)
            return 0;
        else
            return GetIntStat(Stat.happiness);
    }

    public string GetHappinessString()
    {
        return " - Near Housing";
    }
}
