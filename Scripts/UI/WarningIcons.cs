using HexGame.Resources;
using Nova;
using OWS.ObjectPooling;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WarningIcons : MonoBehaviour, IPoolable<WarningIcons>
{
    [SerializeField] private UIBlock2D blockedIcon;
    [SerializeField] private UIBlock2D poweredIcon;
    [SerializeField] private UIBlock2D storageIcon;
    [SerializeField] private UIBlock2D productionIcon;
    [SerializeField] private UIBlock2D workerIcon;

    private static Action<WarningIcons> returnToPool;

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
        returnToPool?.Invoke(this);
    }

    public void SetWarnings(List<ResourceProductionBehavior.ProductionIssue> warnings)
    {
        if (warnings.Contains(ResourceProductionBehavior.ProductionIssue.blocked))
            blockedIcon.gameObject.SetActive(true);
        else
            blockedIcon.gameObject.SetActive(false);
        
        if(warnings.Contains(ResourceProductionBehavior.ProductionIssue.fullStorage))
            storageIcon.gameObject.SetActive(true);
        else
            storageIcon.gameObject.SetActive(false);

        if(warnings.Contains(ResourceProductionBehavior.ProductionIssue.missingResources))
            productionIcon.gameObject.SetActive(true);
        else
            productionIcon.gameObject.SetActive(false);

        if(warnings.Contains(ResourceProductionBehavior.ProductionIssue.noWorkers))
        {
            workerIcon.gameObject.SetActive(true);
            workerIcon.Color = blockedIcon.Color;
        }
        else if(warnings.Contains(ResourceProductionBehavior.ProductionIssue.missingWorkers))
        {
            workerIcon.gameObject.SetActive(true);
            workerIcon.Color = storageIcon.Color;
        }
        else
            workerIcon.gameObject.SetActive(false);


    }

    public void ToggleIconsOff()
    {
        blockedIcon.gameObject.SetActive(false);
        storageIcon.gameObject.SetActive(false);
        productionIcon.gameObject.SetActive(false);
        workerIcon.gameObject.SetActive(false);
        ToggleIfActive();
    }

    public void ToggleIsPowered(bool isPowered)
    {
        poweredIcon.gameObject.SetActive(!isPowered);
        ToggleIfActive();
    }

    private void ToggleIfActive()
    {
        bool isActive = blockedIcon.gameObject.activeSelf 
            || storageIcon.gameObject.activeSelf 
            || productionIcon.gameObject.activeSelf 
            || workerIcon.gameObject.activeSelf
            || poweredIcon.gameObject.activeSelf;

        this.gameObject.SetActive(isActive);
    }
}
