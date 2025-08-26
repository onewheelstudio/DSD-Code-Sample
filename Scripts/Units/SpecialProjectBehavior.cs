using DG.Tweening;
using HexGame.Resources;
using HexGame.Units;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpecialProjectBehavior : UnitBehavior
{
    [SerializeField] private SpecialProjectProduction recipe;
    [SerializeField] private BuildOverTime projectBuildOverTime;
    private UnitStorageBehavior storageBehavior;
    private StatusIndicator statusIndicator;
    private float progress;
    public float Progress => progress;
    public static event Action<SpecialProjectBehavior> Built;
    public static event Action<SpecialProjectProduction> ProjectComplete;
    public static event Action<SpecialProjectProduction, float> ProjectUpdated;

    [Header("Lift")]
    [SerializeField] private Transform lift;
    [SerializeField] private Transform constructionPoint;
    [SerializeField] private ParticleSystem[] engineParticles;
    [SerializeField] private ParticleSystem liftShield;
    [SerializeField] private BuildOverTime shieldFrame;
    private bool isLifting = false;
    private static bool hasProject = false;
    public static bool HasProject => hasProject;

    public static event Action OrbitalLiftDestroyed;

    public override void StartBehavior()
    {
        isFunctional = true;

        if (storageBehavior == null)
            storageBehavior = this.GetComponent<UnitStorageBehavior>();

        storageBehavior.resourceDelivered += CheckResources;
        //storageBehavior.RequestWorkers();
        Built?.Invoke(this);
        StartCoroutine(DelayedResourceCheck(storageBehavior));
    }

    public override void StopBehavior()
    {
        isFunctional = false;
        hasProject = false;
    }

    private void OnDisable()
    {
        OrbitalLiftDestroyed?.Invoke();
    }

    private void CheckResources(UnitStorageBehavior usb, ResourceAmount resource)
    {
        if (resource.type == ResourceType.Workers)
        {
            WorkersReceived(resource);
            return;
        }

        if (!CanProduceAndUpdateStatus())
            return;

        progress = GetProgress(usb, recipe);
        projectBuildOverTime.UpdateProgress(progress);
        ProjectUpdated?.Invoke(recipe, progress);

        if(usb.HasAllResources(recipe.GetCost()))
        {
            //then we're done!
            ProjectComplete?.Invoke(recipe);
            hasProject = false;
            DoLift();
            MessagePanel.ShowMessage("Project Complete!", this.gameObject);
        }
    }

    private void WorkersReceived(ResourceAmount resource)
    {
        numberOfWorkers += resource.amount;
        CanProduceAndUpdateStatus();
    }

    private float GetProgress(UnitStorageBehavior usb, SpecialProjectProduction recipe)
    {
        List<Vector2Int> currentResources = new();
        foreach (var resource in recipe.GetCost())
        {
            Vector2Int r = new Vector2Int(usb.GetAmountStored(resource.type), resource.amount);
            currentResources.Add(r);
        }

        int totalStored = 0;
        int totalNeeded = 0;

        foreach (var resource in currentResources)
        {
            totalStored += resource.x;
            totalNeeded += resource.y;
        }

        return (float)totalStored / (float)totalNeeded;
    }

    public bool CanProduceAndUpdateStatus()
    {
        //can we or should we do production
        if (!CanIProduce())
        {
            SetWarningStatus();
            return false;
        }
        else if (issueList.Count > 0)
        {
            //possible to be low on workers and still functional
            SetWarningStatus();
        }
        else if (hasWarningIcon)
        {
            //turn off and return to pool
            warningIconInstance.ToggleIconsOff();
        }

        //check if we have enough workers before requesting resources
        if (numberOfWorkers < unit.GetStat(Stat.workers))
            return false;

        return true;
    }

    public bool CanIProduce()
    {
        if(recipe == null)
        {
            return false;
        }

        issueList.Clear();
        //if (!recipe.CanProduce(this.gameObject))
        //    issueList.Add(ResourceProductionBehavior.ProductionIssue.blocked);

        float efficiency = storageBehavior.efficiency;
        if (efficiency <= 0.01f)
            issueList.Add(ResourceProductionBehavior.ProductionIssue.noWorkers);
        else if (efficiency < 1f)
            issueList.Add(ResourceProductionBehavior.ProductionIssue.missingWorkers);

        if (issueList.Count == 1 && issueList[0] == ResourceProductionBehavior.ProductionIssue.missingWorkers)
            return true;

        return issueList.Count == 0;
    }

    private void SetWarningStatus()
    {
        if (warningIconInstance == null)
        {
            warningIconInstance = UnitManager.warningIcons.PullGameObject(this.transform.position, Quaternion.identity).GetComponent<WarningIcons>();
            warningIconInstance.transform.SetParent(this.transform);
        }

        warningIconInstance.SetWarnings(issueList);
        statusIndicator?.SetStatus(StatusIndicator.Status.yellow);
    }

    [Button]
    public void AssignProject(SpecialProjectProduction project, Action loadInventory = null)
    {
        hasProject = true;
        this.recipe = project;
        projectBuildOverTime = Instantiate(project.ProjectPrefab, constructionPoint);
        projectBuildOverTime.transform.rotation = constructionPoint.rotation;
        projectBuildOverTime.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        
        storageBehavior.AdjustStorageForProject(project);
        loadInventory?.Invoke();
    }

    [Button]
    private void DoLift()
    {
        isLifting = true;
        var sequence = DOTween.Sequence();
        shieldFrame.gameObject.SetActive(true);
        shieldFrame.UpdateProgress(1f);
        sequence.SetDelay(4f);
        sequence.AppendCallback(() => liftShield.gameObject.SetActive(true));
        sequence.AppendInterval(1f);
        sequence.AppendCallback(() => ToggleEngineParticles(true));
        sequence.Append(lift.DOMoveY(45, 25).SetEase(Ease.InOutQuad));
        sequence.AppendCallback(UnloadProject);
        sequence.AppendInterval(5f);
        sequence.Append(lift.DOMoveY(0, 25).SetEase(Ease.InOutQuad));
        sequence.AppendInterval(1f);
        sequence.AppendCallback(FinishLift);
    }

    private void UnloadProject()
    {
        shieldFrame.UpdateProgress(0f);
        shieldFrame.gameObject.SetActive(false); 
        liftShield.gameObject.SetActive(false);
        projectBuildOverTime.gameObject.SetActive(false);
    }

    private void FinishLift()
    {
        ToggleEngineParticles(false);
        isLifting = false;
    }

    private void ToggleEngineParticles(bool isOn)
    {
        foreach (var particle in engineParticles)
        {
            particle.gameObject.SetActive(isOn);
        }
    }


}
