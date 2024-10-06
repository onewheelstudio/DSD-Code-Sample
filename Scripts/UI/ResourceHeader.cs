using HexGame.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResourceHeader : MonoBehaviour
{
    [SerializeField]
    private GameObject resourceUIPrefab;
    [SerializeField]
    private Transform uiContainer;

    [SerializeField] private List<ResourceType> showList = new List<ResourceType>();
    private Dictionary<ResourceType, GameObject> resouceDisplays = new Dictionary<ResourceType, GameObject>();

    private void Awake()
    {
        foreach (var resource in PlayerResources.GetResourceList().OrderBy(x => x.type))
            CreateResourceUI(resource);
    }

    private void OnEnable()
    {
        ResourceMenu.resourceStarToggled += ToggleResource;
        ShowResourceTrigger.resourceStarToggled += ToggleResource;
    }

    private void OnDisable()
    {
        ResourceMenu.resourceStarToggled -= ToggleResource;
        ShowResourceTrigger.resourceStarToggled -= ToggleResource;
    }


    private void CreateResourceUI(ResourceTemplate resourceTemplate)
    {
        if (resourceTemplate == null)
            return;

        GameObject resourceUI = Instantiate(resourceUIPrefab, uiContainer);
        resourceUI.GetComponent<ResourceUI>().SetResource(resourceTemplate);
        resourceUI.name = resourceTemplate.resourceName;

        resouceDisplays.Add(resourceTemplate.type, resourceUI);

        if(showList.Contains(resourceTemplate.type))
            resourceUI.SetActive(true);
        else
            resourceUI.SetActive(false);
    }

    public List<ResourceType> GetShowList()
    {
        return showList;
    }

    private void ToggleResource(ResourceType type, bool isStarred)
    {
        if (!resouceDisplays.TryGetValue(type, out GameObject resourceUI))
            return;
     
        resourceUI.SetActive(isStarred);

        if (isStarred && !showList.Contains(type))
            showList.Add(type);
        else
            showList.Remove(type);
    }
}
