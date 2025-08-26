using HexGame.Resources;
using Nova;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WarningIcons : MonoBehaviour, IPoolable<WarningIcons>
{
    [Header("Colors")]
    [SerializeField] private Color red;
    [SerializeField] private Color yellow;

    [SerializeField] private ListView warningList;
    [SerializeField] private List<WarningIcon> warningIcons;
    private Dictionary<ResourceProductionBehavior.ProductionIssue, Sprite> warningIconDict = new Dictionary<ResourceProductionBehavior.ProductionIssue, Sprite>();
    private List<ResourceProductionBehavior.ProductionIssue> warnings = new();

    [SerializeField] private ListView resourceIcons;
    private List<ResourceType> missingResouces = new ();
    private PlayerResources playerResources;


    private static Action<WarningIcons> returnToPool;

    private void Awake()
    {
        playerResources = FindFirstObjectByType<PlayerResources>();
        resourceIcons.AddDataBinder<ResourceType, IconVisuals>(BindResourceIcons);
        warningList.AddDataBinder<ResourceProductionBehavior.ProductionIssue, IconVisuals>(BindWarningIcons);

        foreach (var icon in warningIcons)
        {
            if (!warningIconDict.ContainsKey(icon.issue))
                warningIconDict.Add(icon.issue, icon.icon);
        }
    }

    private void OnDisable()
    {
        ReturnToPool();
    }

    public void Initialize(Action<WarningIcons> returnAction)
    {
        returnToPool = returnAction;
    }

    public void ReturnToPool()
    {
        missingResouces.Clear();
        returnToPool?.Invoke(this);
    }

    public void SetWarnings(List<ResourceProductionBehavior.ProductionIssue> warnings)
    {
        if (warnings.Count != this.warnings.Count)
        {
            warningList.SetDataSource(warnings);
            this.warnings = new List<ResourceProductionBehavior.ProductionIssue>(warnings);
            return;
        }

        //if the list is the same length then check if the contents are the same
        for (int i = 0; i < warnings.Count; i++)
        {
            if (warnings[i] != this.warnings[i])
            {
                warningList.SetDataSource(warnings);
                this.warnings = new List<ResourceProductionBehavior.ProductionIssue>(warnings);
                return;
            }
        }
    }

    private void BindWarningIcons(Data.OnBind<ResourceProductionBehavior.ProductionIssue> evt, IconVisuals target, int index)
    {
        var icon = warningIconDict[evt.UserData];
        target.icon.SetImage(icon);

        switch (evt.UserData)
        {
            case ResourceProductionBehavior.ProductionIssue.notPowered:
            case ResourceProductionBehavior.ProductionIssue.blocked:
            case ResourceProductionBehavior.ProductionIssue.noWorkers:
                target.icon.Color = red;
                break;
            case ResourceProductionBehavior.ProductionIssue.fullStorage:
            case ResourceProductionBehavior.ProductionIssue.missingResources:
            case ResourceProductionBehavior.ProductionIssue.missingWorkers:
                target.icon.Color = yellow;
                break;
        }
    }

    private void BindResourceIcons(Data.OnBind<ResourceType> evt, IconVisuals target, int index)
    {
        var resourcesTemplate = playerResources.GetResourceTemplate(evt.UserData);
        target.icon.SetImage(resourcesTemplate.icon);
        target.icon.Color = resourcesTemplate.resourceColor;
    }

    [Button]
    public void SetResourceWarnings(List<ResourceType> resources)
    {
        if (resources == null && this.missingResouces.Count > 0)
        {
            this.missingResouces.Clear();
            resourceIcons.SetDataSource(this.missingResouces);
        }
        else if (resources.Count != this.missingResouces.Count)
        {
            this.missingResouces = new List<ResourceType>(resources);
            resourceIcons.SetDataSource(this.missingResouces);
        }
    }

    public void AddResourceWarning(ResourceType resouce)
    {
        if(!missingResouces.Contains(resouce))
            missingResouces.Add(resouce);
    }

    public void ClearResourceWarnings()
    {
        if(missingResouces.Count > 0)
            SetResourceWarnings(new List<ResourceType>());
    }

    public void ShowResourceWarnings()
    {
        if(ResourceNeedsUpdate())
            resourceIcons.SetDataSource(missingResouces);
    }

    private bool ResourceNeedsUpdate()
    {
        var dataSource = resourceIcons.GetDataSource<ResourceType>();
        if (dataSource == null  || dataSource.Count != missingResouces.Count)
            return true;

        for (int i = 0; i < missingResouces.Count; i++)
        {
            if (!resourceIcons.GetDataSource<ResourceType>().Contains(missingResouces[i]))
                return true;
        }

        return false;
    }


    public void ToggleIconsOff()
    {
        if(this.warnings.Count > 0)
        {
            this.warnings.Clear();
            warningList.SetDataSource(this.warnings);
        }

        if(this.missingResouces.Count > 0)
        {
            this.missingResouces.Clear();
            resourceIcons.SetDataSource(this.missingResouces);
        }
    }

    public void ToggleIsPowered(bool isPowered)
    {
    }

    private void ToggleIfActive()
    {
        bool isActive = warnings.Count > 0 || missingResouces.Count > 0;

        this.gameObject.SetActive(isActive);
    }

    public bool HasWarning()
    {
        return warnings.Count > 0 || missingResouces.Count > 0;
    }

    [System.Serializable]
    public class WarningIcon
    {
        public ResourceProductionBehavior.ProductionIssue issue;
        public Sprite icon;
    }
}
