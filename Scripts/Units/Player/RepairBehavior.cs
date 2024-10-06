using HexGame.Resources;
using HexGame.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HexGame.Resources.ResourceProductionBehavior;

public class RepairBehavior : UnitBehavior
{
    private UnitStorageBehavior usb;
    private PlayerUnit repairTarget;
    private List<PlayerUnit> repairTargets;
    private UnitManager unitManger;
    [SerializeField]
    private Drone[] repairDrones = new Drone[0];
    [SerializeField]
    private ResourceType repairResource = ResourceType.Energy;
    [SerializeField]
    private int repairAmount = 10;
    [SerializeField]
    private float repairTime = 1f;

    private int requiredNumberOfWorkers => (int)this.GetStat(Stat.workers);
    private int numberOfWorkers;
    private WaitForSeconds repairInterval;


    private void Awake()
    {
        unitManger = FindObjectOfType<UnitManager>();
        repairInterval = new WaitForSeconds(repairTime);
    }

    private void OnEnable()
    {
        usb = this.GetComponent<UnitStorageBehavior>();
        usb.resourceDelivered += ResourceDelivered;

        DayNightManager.toggleDay += StartRepairs;
    }

    private void OnDisable()
    {
        usb.resourceDelivered -= ResourceDelivered;
        DayNightManager.toggleDay -= StartRepairs;
    }

    public override void StartBehavior()
    {
        isFunctional = numberOfWorkers > 0;
        usb.RequestWorkers();
        DisplayWarning();
    }

    public override void StopBehavior()
    {
        isFunctional = false;
    }

    private void StartRepairs(int dayNumber)
    {
        repairTargets = GetRepairableTargets();
        StartCoroutine(DoRepairs(repairTargets));
    }

    private IEnumerator DoRepairs(List<PlayerUnit> repairTargets)
    {
        while(repairTargets.Count > 0)
        {
            repairTarget = repairTargets[0];
            repairTargets.RemoveAt(0);
            Vector3 position = repairTarget.transform.position;

            while (GetRepairAmountNeeded(repairTarget) > 0)
            {
                yield return MoveDrones(position, repairDrones, repairInterval);
                if (usb.TryUseResource(new ResourceAmount(repairResource, 1)))
                {
                    repairTarget.RestoreHP(repairAmount * numberOfWorkers / requiredNumberOfWorkers);
                }

                usb.CheckResourceLevels(new ResourceAmount(ResourceType.Energy,1));
            }
        }
        
        ResetDrones();
    }

    private IEnumerator MoveDrones(Vector3 position, Drone[] repairDrones, WaitForSeconds repairInterval)
    {
        for (int i = 0; i < repairDrones.Length; i++)
        {
            if (i == repairDrones.Length - 1)
            {
                yield return repairDrones[i].DoDroneAction(position, repairInterval);
            }
            else
            {
                StartCoroutine(repairDrones[i].DoDroneAction(position));
            }
        }
    }

    private void ResourceDelivered(UnitStorageBehavior behavior, ResourceAmount amount)
    {
        if(amount.type == ResourceType.Workers)
        {
            numberOfWorkers = (int)amount.amount;
            isFunctional =numberOfWorkers > 0;
            DisplayWarning();
        }
        else if(amount.type == repairResource && amount.amount == usb.GetAmountStored(amount.type))
        {
            StartRepairs(0);
        }

        usb.CheckResourceLevels();
    }

    private void ResetDrones()
    {
        repairTarget = null;
        foreach (var drone in repairDrones)
        {
            drone.DoReturnToStart();
        }
        StopAllCoroutines();
    }

    private List<PlayerUnit> GetRepairableTargets()
    {
        List<PlayerUnit> playerUnits = UnitManager.PlayerUnitsInRange(this.transform.position.ToHex3(), GetIntStat(Stat.maxRange));
        playerUnits.Sort((a, b) => GetRepairAmountNeeded(b).CompareTo(GetRepairAmountNeeded(a)));

        for (int i = playerUnits.Count - 1; i >= 0; i--)
        {
            if (GetRepairAmountNeeded(playerUnits[i]) <= 0 || playerUnits[i].unitType == PlayerUnitType.infantry)
                playerUnits.RemoveAt(i);
        }

        return playerUnits;
    }

    private float GetRepairAmountNeeded(PlayerUnit repairTarget)
    {
        return repairTarget.GetStat(Stat.hitPoints) - repairTarget.GetHP();
    }

    private void DisplayWarning()
    {
        List<ProductionIssue> issueList = new List<ProductionIssue>();
        if (requiredNumberOfWorkers > numberOfWorkers)
            issueList.Add(ProductionIssue.missingWorkers);
        else if (numberOfWorkers == 0)
            issueList.Add(ProductionIssue.noWorkers);

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

}
