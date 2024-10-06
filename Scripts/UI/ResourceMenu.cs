using HexGame.Resources;
using Nova;
using NovaSamples.UIControls;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResourceMenu : WindowPopup
{
    [SerializeField]
    private GameObject resourceUIPrefab;
    [SerializeField]
    private Transform uiContainer;
    public static event System.Action<ResourceType, bool> resourceStarToggled;
    private Dictionary<ResourceType, InventoryItemStarVisuals> resouceDisplays = new Dictionary<ResourceType, InventoryItemStarVisuals>();
    private ResourceHeader resourceHeader;

    private void Awake()
    {
        resourceHeader = FindObjectOfType<ResourceHeader>();
        foreach (var resource in PlayerResources.GetResourceList())
            CreateResourceUI(resource);
    }

    private void Start()
    {
        novaGroup.UpdateInteractables();
        CloseWindow();
    }

    private new void OnEnable()
    {
        base.OnEnable();
        ShowResourceTrigger.resourceStarToggled += ResourceToggled;
        PlayerResources.ResourceProductionStarted += ResourceToggled;
    }



    private new void OnDisable()
    {
        base.OnDisable();
        ShowResourceTrigger.resourceStarToggled -= ResourceToggled;
        PlayerResources.ResourceProductionStarted -= ResourceToggled;
    }

    private void CreateResourceUI(ResourceTemplate resourceTemplate)
    {
        if (resourceTemplate == null)
            return;

        GameObject resourceUI = Instantiate(resourceUIPrefab, uiContainer);
        resourceUI.GetComponent<ResourceUI>().SetResource(resourceTemplate);
        resourceUI.name = resourceTemplate.resourceName;

        InventoryItemStarVisuals visuals = resourceUI.GetComponent<ItemView>().Visuals as InventoryItemStarVisuals;
        visuals.starToggle.ToggledOn = resourceHeader.GetShowList().Contains(resourceTemplate.type);
        visuals.starToggle.toggled += (t,b) => resourceStarToggled?.Invoke(resourceTemplate.type, b);
        resouceDisplays.Add(resourceTemplate.type, visuals);
    }

    private void ResourceToggled(ResourceType type)
    {
        ResourceToggled(type, true);
    }

    private void ResourceToggled(ResourceType type, bool isStarred)
    {
        if (!resouceDisplays.TryGetValue(type, out InventoryItemStarVisuals resourceUI))
            return;

        resourceUI.starToggle.ToggledOn = isStarred;
    }
}
