using HexGame.Resources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResourceHeader : MonoBehaviour, ISaveData
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
        RegisterDataSaving();
        PlayerResources.ResourceProductionStarted += ResourceProductionStarted;
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

    private void OnDestroy()
    {
        PlayerResources.ResourceProductionStarted -= ResourceProductionStarted;
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


    private void ResourceProductionStarted(ResourceType type)
    {
        ToggleResource(type, true);
    }

    private void ToggleResource(ResourceType type, bool isStarred)
    {
        if (!resouceDisplays.TryGetValue(type, out GameObject resourceUI))
            return;

        if (resourceUI == null)
            return;

        resourceUI.SetActive(isStarred);

        if (isStarred && !showList.Contains(type))
            showList.Add(type);
        else if(!isStarred)
            showList.Remove(type);
    }

    private const string RESOURCE_DISPLAY = "resourceDisplay";
    private const string RESOURCE_BAR_DATA = "resourceBarData";
    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this);
    }

    public void Save(string savePath, ES3Writer writer)
    {

        List<ResourceBarData> barData = new();
        foreach (var resource in resouceDisplays.Keys)
        {
            ResourceUI resourceUI = resouceDisplays[resource].GetComponent<ResourceUI>();
            ResourceBarData data = new ResourceBarData
            {
                type = resource,
                barValues = resourceUI.GetBarValues()
            };

            barData.Add(data);
        }

        writer.Write<List<ResourceType>>(RESOURCE_DISPLAY, showList);
        writer.Write<List<ResourceBarData>>(RESOURCE_BAR_DATA, barData);
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if(ES3.KeyExists(RESOURCE_DISPLAY, loadPath))
        {
            List<ResourceType> resourcesDisplayed = ES3.Load<List<ResourceType>>(RESOURCE_DISPLAY, loadPath);
            foreach (var resource in resourcesDisplayed)
            {
                ToggleResource(resource, true);
            }
        }

        yield return null;

        if (ES3.KeyExists(RESOURCE_BAR_DATA, loadPath))
        {
            List<ResourceBarData> barData = ES3.Load<List<ResourceBarData>>(RESOURCE_BAR_DATA, loadPath);
            foreach (var data in barData)
            {
                if (resouceDisplays.TryGetValue(data.type, out GameObject resourceObject))
                {
                    ResourceUI resourceUI = resourceObject.GetComponent<ResourceUI>();
                    if (resourceUI == null)
                        continue;

                    resourceUI.SetBarValues(data.barValues);
                }
            }
        }
        yield return null;
    }

    public struct ResourceBarData
    {
        public ResourceType type;
        public float[] barValues;
    }
}
