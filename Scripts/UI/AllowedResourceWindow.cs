using HexGame.Resources;
using Nova;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class AllowedResourceWindow : WindowPopup
{
    [SerializeField] private GridView resourceView;
    [SerializeField] private Button AddAllResources;
    [SerializeField] private Button RemoveAllResources;
    private ITransportResources transportStorage;
    private PlayerResources playerResources;
    private List<ResourceType> allResources = new List<ResourceType>();
    private HashSet<ResourceType> allowedResources;
    public static event Action<ITransportResources> AllowedResourcesChanged;

    private void Awake()
    {
        playerResources = FindFirstObjectByType<PlayerResources>();
        resourceView.SetSliceProvider(ResourceGridSlice);
        resourceView.AddDataBinder<ResourceType, ResourceIconDisplayVisuals>(BindResources);

        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            if (type == ResourceType.Workers)
                continue;

            var resourcesTemplate = playerResources.GetResourceTemplate(type);
            if(resourcesTemplate == null)
            {
                Debug.LogError($"Resource template for {type} not found.");
                continue;
            }

            allResources.Add(type);
        }
    }

    private new void OnEnable()
    {
        base.OnEnable();
        AddAllResources.Clicked += AllowAllResources;
        RemoveAllResources.Clicked += AllowNoResources;
    }

    private new void OnDisable()
    {
        base.OnDisable();
        AddAllResources.Clicked -= AllowAllResources;
        RemoveAllResources.Clicked -= AllowNoResources;
    }



    private void Start()
    {
        novaGroup.UpdateInteractables();
        CloseWindow();
    }

    private void ResourceGridSlice(int sliceIndex, GridView gridView, ref GridSlice gridSlice)
    {
        gridSlice.AutoLayout.Spacing.Value = 5;
    }

    private void BindResources(Data.OnBind<ResourceType> evt, ResourceIconDisplayVisuals target, int index)
    {
        var resourcesTemplate = playerResources.GetResourceTemplate(evt.UserData);
        target.Background.SetImage(resourcesTemplate.icon);
        if (allowedResources.Contains(evt.UserData))
            target.Background.Color = resourcesTemplate.resourceColor;
        else
            target.Background.Color = Color.grey;
        
        target.toolTip.SetToolTipInfo(evt.UserData.ToNiceString(), resourcesTemplate.icon);

        target.button.RemoveClickListeners();
        target.button.Clicked += () => ToggleResource(evt.UserData, target);
    }

    private void ToggleResource(ResourceType userData, ResourceIconDisplayVisuals target)
    {
        if(allowedResources.Contains(userData))
        {
            transportStorage.RemoveAllowedResource(userData);
            target.Background.Color = Color.grey;
        }
        else
        {
            transportStorage.AddAllowedResource(userData);
            target.Background.Color = playerResources.GetResourceTemplate(userData).resourceColor;
        }
        AllowedResourcesChanged?.Invoke(transportStorage);
    }

    public void SetTransportStorage(ITransportResources transportStorage)
    {
        this.transportStorage = transportStorage;
        this.allowedResources = transportStorage.GetAllowedResources();
        resourceView.SetDataSource(allResources);
    }

    [Button]
    private void AllowAllResources()
    {
        for (int i = 0; i < allResources.Count; i++)
        {
            transportStorage.AddAllowedResource(allResources[i]);
        }
        AllowedResourcesChanged?.Invoke(transportStorage);
        resourceView.SetDataSource(allResources);
    }

    [Button]
    private void AllowNoResources()
    {
        for (int i = 0; i < allResources.Count; i++)
        {
            transportStorage.RemoveAllowedResource(allResources[i]);
        }
        AllowedResourcesChanged?.Invoke(transportStorage);
        resourceView.SetDataSource(allResources);
    }
}
