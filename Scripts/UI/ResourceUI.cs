using HexGame.Resources;
using HexGame.Units;
using Nova;
using NovaSamples.UIControls;
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

    [SerializeField] private Button hoverButton;
    public static event Action<ResourceType> resourceHovered;
    public static event Action<ResourceType> resourceUnHovered;

    private void Awake()
    {
        bars = barParent.GetComponentsInChildren<UIBlock2D>().Where(x => x.transform != barParent).ToArray();
        PlayerResources.resourceChange += UpdateUI;
        visuals = this.GetComponent<ItemView>().Visuals as InventoryItemVisuals;
        //ToggleChildren(false);

        PlayerResources.resourceUpdate += SetBarValue;
        PlayerResources.resourceInitialValue += SetBarInitialValues;
        if (resourceHeader == null)
            resourceHeader = FindFirstObjectByType<ResourceHeader>();

        //ResourceMenu.resourceStarToggled += ResourceStarToggled;

        hoverButton.hover += OnHover;
        hoverButton.unhover += OnUnHover;
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

        PlayerResources.resourceUpdate -= SetBarValue;
        PlayerResources.resourceInitialValue -= SetBarInitialValues;
        //ResourceMenu.resourceStarToggled -= ResourceStarToggled;

        hoverButton.hover += OnHover;
        hoverButton.unhover += OnUnHover;
    }

    private void UpdateUI(ResourceType type, int amount)
    {
        if (this.resourceTemplate == null || type != this.resourceTemplate.type)
            return;

        if (amount < 0)
            amount = 0;

        if (type != ResourceType.Workers)
        {
            visuals.count.Text = amount.ToString();
            toolTip?.SetOffset(new Vector2(-50, -125));

            int amountProduced = PlayerResources.GetAmountProducedYesterday(type);
            int amountUsed = PlayerResources.GetAmountUsedYesderday(type);
            string infoString;
            if(amountProduced == 0 && amountUsed == 0)
                infoString = $"Stored: {amount}";
            else
                infoString = $"Stored: {amount}\nUsed: {amountUsed}\nProduced: {amountProduced}";

            this.toolTip?.SetToolTipInfo(resourceTemplate.type.ToNiceString(), resourceTemplate.icon, infoString);

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

        string colonistValues = Mathf.Max(0, WorkerManager.AvailableWorkers).ToString();
        colonistValues += $"/{WorkerManager.TotalWorkers}";
        colonistValues += $"/{WorkerManager.housingCapacity}";

        visuals.count.Text = colonistValues;
        toolTip?.SetOffset(new Vector2(-50, -250));
        string infoString;

        infoString = "Available: " + Mathf.Max(0, WorkerManager.AvailableWorkers).ToString();
        if (WorkerManager.AvailableWorkers <= 0 && WorkerManager.workersNeeded > 0)
            infoString += ("\nNeeded: " + WorkerManager.workersNeeded.ToString()).TMP_Color(ColorManager.GetColor(ColorCode.offPriority));
        infoString += "\nTotal: " + WorkerManager.TotalWorkers.ToString();
        infoString += $"\nHousing: {WorkerManager.housingCapacity}";

        infoString += $"\nEfficiency: {WorkerManager.globalWorkerEfficiency * 100}%";
        toolTip?.SetToolTipInfo(resourceTemplate.type.ToNiceString(),
                                resourceTemplate.icon, infoString);

        if (WorkerManager.AvailableWorkers - WorkerManager.workersNeeded <= 0)
            visuals.count.Color = ColorManager.GetColor(ColorCode.offPriority);
        else if (WorkerManager.AvailableWorkers - WorkerManager.workersNeeded < 3)
            visuals.count.Color = ColorManager.GetColor(ColorCode.lowPriority);
        else
            visuals.count.Color = Color.white;
    }

    public void SetResource(ResourceTemplate resourceTemplate)
    {
        //ensure colonists on top
        this.resourceTemplate = resourceTemplate;
        visuals.icon.SetImage(resourceTemplate.icon);
        visuals.icon.Color = resourceTemplate.resourceColor;
        this.toolTip = this.GetComponentInChildren<InfoToolTip>(true);
        this.toolTip?.SetToolTipInfo($"{resourceTemplate.type.ToNiceString()}", resourceTemplate.icon, string.Empty);
        this.transform.SetAsLastSibling();
        visuals.count.Text = "0";
        //ToggleChildren(false);

        if (this.resourceTemplate.type == ResourceType.Workers)
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
            if (!active)
                child.transform.SetAsLastSibling();
        }
    }

    private void SetBarValue(ResourceType type, int amount)
    {
        if (this.resourceTemplate == null || type != this.resourceTemplate.type)
            return;

        SetBarValue(amount);
    }

    private void SetBarInitialValues(ResourceType type, int amount)
    {
        if (this.resourceTemplate == null || type != this.resourceTemplate.type)
            return;

        for (int i = 0; i < barValues.Length; i++)
        {
            barValues[i] = amount;
            bars[i].Size.Y.Percent = 1f;
        }
    }

    [Button]
    private void SetBarValue(int amount)
    {
        for (int i = 0; i < barValues.Length - 1; i++)
        {
            barValues[i] = barValues[i + 1];
        }

        barValues[barValues.Length - 1] = amount;
        float maxValue = barValues.Max(x => x);
        maxValue = Mathf.Max(25, maxValue);

        for (int i = 0; i < bars.Length; i++)
        {
            bars[i].Size.Y.Percent = barValues[i] / maxValue;
        }
    }

    public void SetBarValues(float[] values)
    {
        foreach (var value in values)
        {
            SetBarValue((int)value);
        }
    }

    public void OpenResourceMenu()
    {
        if(resourceTemplate.type == ResourceType.Workers)
            FindFirstObjectByType<WorkerMenu>().OpenWindow();
        else
            FindFirstObjectByType<ResourceMenu>().OpenWindow();
    }

    public float[] GetBarValues()
    {
        return barValues;
    }

    private void OnHover()
    {
        resourceHovered?.Invoke(resourceTemplate.type);
    }

    private void OnUnHover()
    {
        resourceUnHovered?.Invoke(resourceTemplate.type);
    }
}
