using HexGame.Resources;
using HexGame.Units;
using Nova;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResourceUI : MonoBehaviour
{
    [SerializeField]
    private ResourceTemplate resourceTemplate;
    private InventoryItemVisuals visuals;
    [SerializeField]
    private List<GameObject> children = new List<GameObject>();
    private PlayerResources playerResources;
    private InfoToolTip toolTip;
    [SerializeField] private Transform barParent;
    [SerializeField] private UIBlock2D[] bars;
    [SerializeField] private float[] barValues = new float[5];
    protected static ResourceHeader resourceHeader;
    [SerializeField] private bool alwaysVisible = false;

    private void Awake()
    {
        bars = barParent.GetComponentsInChildren<UIBlock2D>().Where(x => x.transform != barParent).ToArray();
        PlayerResources.resourceChange += UpdateUI;
        visuals = this.GetComponent<ItemView>().Visuals as InventoryItemVisuals;
        //ToggleChildren(false);

        PlayerResources.resourceUpdate += SetBarValues;
        PlayerResources.resourceInitialValue += SetBarInitialValues;
        if(resourceHeader == null)
            resourceHeader = FindObjectOfType<ResourceHeader>();

        //ResourceMenu.resourceStarToggled += ResourceStarToggled;
    }

    private void OnDestroy()
    {
        PlayerResources.resourceChange -= UpdateUI;
        if (this.resourceTemplate.type == ResourceType.Workers)
        {
            WorkerManager.workerStateChanged -= UpdateColonistValues;
            UnitManager.unitPlaced -= UpdateColonistValues;
            PlayerUnit.unitRemoved -= UpdateColonistValues;
        }

        PlayerResources.resourceUpdate -= SetBarValues;
        PlayerResources.resourceInitialValue -= SetBarInitialValues;
        //ResourceMenu.resourceStarToggled -= ResourceStarToggled;
    }

    private void UpdateUI(ResourceType type, int amount, int storageLimit)
    {
        if (this.resourceTemplate == null || type != this.resourceTemplate.type)
            return;

        if(amount < 0)
            amount = 0;

        if(storageLimit < 0)
            storageLimit = 0;

        if (type != ResourceType.Workers)
        {
            visuals.count.Text = $"{Mathf.Min(amount, storageLimit)}";
            toolTip?.SetOffset(new Vector2(-50, -125));

            int amountProduced = PlayerResources.GetAmountProducedYesterday(type);
            int amountUsed = PlayerResources.GetAmountUsedYesderday(type);
            string infoString = $"Stored: {Mathf.Min(amount, storageLimit)}/{storageLimit}\nUsed: {amountUsed}\nProduced: {amountProduced}";
            this.toolTip?.SetToolTipInfo($"{resourceTemplate.type.ToNiceString()}", resourceTemplate.icon, infoString);

            if (amount < 10)
            {
                visuals.count.Color = ColorManager.GetColor(ColorCode.red);
            }
            else
            {
                visuals.count.Color = Color.white;
            }
        }
        else
        {
            UpdateColonistValues();
        }
    }

    private void UpdateColonistValues(Unit unit)
    {
        UpdateColonistValues();
    }

    private void UpdateColonistValues()
    {
        this.transform.SetAsFirstSibling();

        string colonistValues = WorkerManager.availableWorkers.ToString();
        colonistValues += $"/{WorkerManager.totalWorkers}";
        colonistValues += $"/{WorkerManager.housingCapacity}";

        visuals.count.Text = colonistValues;
        toolTip?.SetOffset(new Vector2(-50, -250));
        string infoString;

        infoString = "Available: " + WorkerManager.availableWorkers.ToString();
        if (WorkerManager.workersNeeded > 0)
            infoString += ("\nNeeded: " + WorkerManager.workersNeeded.ToString()).TMP_Color(ColorManager.GetColor(ColorCode.offPriority));
        infoString += "\nTotal: " + WorkerManager.totalWorkers.ToString();
        infoString += $"\nHousing: {WorkerManager.housingCapacity}";

        infoString += $"\nEfficiency: {WorkerManager.globalWorkerEfficiency * 100}%";
        toolTip?.SetToolTipInfo(resourceTemplate.type.ToNiceString(),
                                resourceTemplate.icon, infoString);

        if (WorkerManager.availableWorkers - WorkerManager.workersNeeded <= 0)
            visuals.count.Color = ColorManager.GetColor(ColorCode.offPriority);
        else if (WorkerManager.availableWorkers - WorkerManager.workersNeeded < 3)
            visuals.count.Color = ColorManager.GetColor(ColorCode.lowPriority);
        else
            visuals.count.Color = Color.white;
    }

    public void SetResource(ResourceTemplate resourceTemplate)
    {
        //ensure colonists on top
        this.resourceTemplate = resourceTemplate;
        visuals.icon.SetImage(resourceTemplate.icon);
        visuals.icon.Color =resourceTemplate.resourceColor;
        this.toolTip = this.GetComponentInChildren<InfoToolTip>(true);
        this.toolTip?.SetToolTipInfo($"{resourceTemplate.type.ToNiceString()}", resourceTemplate.icon, string.Empty);
        this.transform.SetAsLastSibling();
        visuals.count.Text = "0";
        //ToggleChildren(false);

        if(this.resourceTemplate.type == ResourceType.Workers)
        {
            WorkerManager.workerStateChanged += UpdateColonistValues;
            UnitManager.unitPlaced += UpdateColonistValues;
            PlayerUnit.unitRemoved += UpdateColonistValues;
        }

        //bars for the colonists behaves weirdly and we already have 3 values for them
        barParent.gameObject.SetActive(this.resourceTemplate.type != ResourceType.Workers);
    }



    private void ResourceStarToggled(ResourceType type, bool isStarred)
    {
        if (type != this.resourceTemplate.type || this.alwaysVisible)
            return;

        //ToggleChildren(isStarred);
    }

    private void ToggleChildren(bool active)
    {
        if (active && children[0].activeInHierarchy)
            return;

        foreach (var child in children)
        {
            child.SetActive(active);
            if(!active)
                child.transform.SetAsLastSibling();
        }
    }

    private void SetBarValues(ResourceType type, int arg2)
    {
        if (this.resourceTemplate == null || type != this.resourceTemplate.type)
            return;
            
        SetBarValues(arg2);
    }

    private void SetBarInitialValues(ResourceType type, int amount)
    {
        if(this.resourceTemplate == null || type != this.resourceTemplate.type)
            return;

        for (int i = 0; i < barValues.Length; i++)
        {
            barValues[i] = amount;
            bars[i].Size.Y.Percent = 1f;
        }
    }

    [Button]
    private void SetBarValues(int amount)
    {
        for (int i = 0; i < barValues.Length - 1; i++)
        {
            barValues[i] = barValues[i + 1];
        }

        barValues[barValues.Length - 1] = amount;
        float maxValue = barValues.Max(x => x);
        maxValue = Mathf.Max(25, maxValue);

        for(int i = 0; i < bars.Length; i++)
        {
            bars[i].Size.Y.Percent = barValues[i] / maxValue;
        }
    }

    public void OpenResourceMenu()
    {
        FindObjectOfType<ResourceMenu>().OpenWindow();
    }

}
