using HexGame.Resources;
using HexGame.Units;
using Nova;
using NovaSamples.UIControls;
using OWS.Nova;
using System;
using UnityEngine;

public class TradeResourceUI : MonoBehaviour
{
    [SerializeField] private ToggleSwitch useAutoTrader;
    [SerializeField] private ToggleSwitch buySellToggle;
    [SerializeField] private UIBlock2D resourceIcon;
    [SerializeField] private TextBlock resourceLabel;
    [SerializeField] private Slider stockPile;
    private InfoToolTip infoToolTip;
    [SerializeField] private TradeInfo tradeInfo;
    [SerializeField] private UIBlock2D notification;
    private InfoToolTip notificationToolTip;

    [Header("On/Off Settings")]
    [SerializeField] private ClipMask clipMask;
    [SerializeField] private Color offTint;
    public TradeInfo TradeInfo => tradeInfo;
    private PlayerResources playerResources;
    private int multiplier = 5;
    private StockMarket stockMarket;

    private void Awake()
    {
        playerResources = FindFirstObjectByType<PlayerResources>();
        infoToolTip = stockPile.GetComponentInChildren<InfoToolTip>();
        notificationToolTip = notification.GetComponentInChildren<InfoToolTip>();
        stockMarket = FindFirstObjectByType<StockMarket>();
    }

    private void OnEnable()
    {
        //PlayerResources.StoragedChanged += StorageChanged;
        HexTechTree.techCreditChanged += CheckCanAfford;
    }

    private void OnDisable()
    {
        //PlayerResources.StoragedChanged -= StorageChanged;
        HexTechTree.techCreditChanged -= CheckCanAfford;
    }

    private void StorageChanged(ResourceType type, int maxStorage)
    {
        if(type != tradeInfo.resource)
            return;
        this.stockPile.Max = maxStorage / 5;
    }

    public void SetUpTradeResource(ResourceType resource)
    {
        tradeInfo = new TradeInfo(resource, stockPile);

        this.useAutoTrader.SetValueWithOutCallback(false);
        this.useAutoTrader.RemoveAllListeners();
        this.useAutoTrader.Toggled += tradeInfo.SetUseAutoTrader;
        this.useAutoTrader.Toggled += ToggleTrading;

        this.buySellToggle.SetValueWithOutCallback(true);
        this.buySellToggle.RemoveAllListeners();
        this.buySellToggle.Toggled += tradeInfo.SetSell;
        this.buySellToggle.Toggled += UpdateSliderLabel;
        this.buySellToggle.Toggled += CheckCanAfford;

        ResourceTemplate resourceTemplate = playerResources.GetResourceTemplate(tradeInfo.resource);
        if (resourceTemplate == null)
            return;

        this.resourceIcon.SetImage(resourceTemplate.icon);
        this.resourceIcon.Color = resourceTemplate.resourceColor;
        this.resourceLabel.Text = resourceTemplate.type.ToNiceString();

        this.stockPile.Min = 0;
        this.stockPile.Max = PlayerResources.GetStorageLimit(tradeInfo.resource) / stockPile.UnitMultiplier;
        this.stockPile.Value = tradeInfo.stockPile / stockPile.UnitMultiplier;
        this.UpdateSliderLabel(buySellToggle, this.buySellToggle.ToggledOn);
        this.stockPile.RemoveAllListeners();
        this.stockPile.ValueChanged += tradeInfo.SetStockPile;
    }

    //used for loading
    public void SetUpTradeResource(TradeInfo tradeInfo)
    {
        this.useAutoTrader.ToggledOn = tradeInfo.useAutoTrader;
        this.buySellToggle.ToggledOn = tradeInfo.sell;

        this.stockPile.Min = 0;
        this.stockPile.Max = PlayerResources.GetStorageLimit(tradeInfo.resource) / stockPile.UnitMultiplier;
        this.stockPile.Value = tradeInfo.stockPile / stockPile.UnitMultiplier;
        this.UpdateSliderLabel(buySellToggle, this.buySellToggle.ToggledOn);
    }

    public void ToggleTrading(ToggleSwitch @switch, bool isOn)
    {
        clipMask.Tint = isOn ? Color.white : offTint;
        buySellToggle.enabled = isOn;
        stockPile.enabled = isOn;
        if(isOn)
            this.stockPile.Value = tradeInfo.stockPile / stockPile.UnitMultiplier;
        this.UpdateSliderLabel(buySellToggle, this.buySellToggle.ToggledOn);
    }

    private void UpdateSliderLabel(ToggleSwitch toggle, bool sell)
    {
        if(sell)
        {
            stockPile.Label = "Max Storage";
            stockPile.Min = 0;
            stockPile.Max = PlayerResources.GetStorageLimit(tradeInfo.resource) / stockPile.UnitMultiplier;
            infoToolTip.SetToolTipInfo("Max Storage", "If the amount stored is <b>more</b> than the value a trade will be placed.");
        }
        else
        {
            stockPile.Label = "Min Storage";
            stockPile.Min = 0;
            stockPile.Max = (PlayerResources.GetStorageLimit(tradeInfo.resource) - 50) / stockPile.UnitMultiplier;
            if(stockPile.Value > stockPile.Max)
                stockPile.Value = stockPile.Max;
            infoToolTip.SetToolTipInfo("Min Storage", "If the amount stored is <b>less</b> than the value and sufficient credits are available a trade will be placed.");
        }
    }

    public void ToggleNotification(bool isOn)
    {
        notification.gameObject.SetActive(isOn);
    }

    public void SetNotificationToolTip(string title, string description)
    {
        notificationToolTip.SetToolTipInfo(title, description);
    }

    private void CheckCanAfford()
    {
        CanAffordToBuy();
    }

    private void CheckCanAfford(ToggleSwitch @switch, bool sell)
    {
        CanAffordToBuy();
    }

    /// <summary>
    /// Can we afford to buy 1 shipment of the resource?
    /// </summary>
    /// <param name="resource"></param>
    /// <returns></returns>
    /// 

    public bool CanAffordToBuy()
    {
        if(tradeInfo.sell)
        {
            ToggleNotification(false);
            return false; 
        }

        float priceOfResource = stockMarket.GetResourcePrice(TradeInfo.resource);
        int price = Mathf.RoundToInt(SupplyShipManager.supplyShipCapacity * priceOfResource);

        price = Mathf.RoundToInt(price);
        if (HexTechTree.TechCredits < price)
        {
            ToggleNotification(true);
            SetNotificationToolTip("Not Enough Credits", "You do not have enough credits to buy this resource.");
            return false;
        }
        else
        {
            ToggleNotification(false);
            return true;
        }

    }
}

[System.Serializable]
public class TradeInfo
{
    public TradeInfo()
    {

    }

    public TradeInfo(ResourceType resource, Slider slider)
    {
        this.resource = resource;
        useAutoTrader = false;
        stockPile = 100;
        sell = true;
        multiplier = slider.UnitMultiplier;
    }

    public bool useAutoTrader;
    public ResourceType resource;
    public int stockPile;
    public bool sell;
    public static event Action<ResourceType> TradeResourceChanged;
    private int multiplier = 5;

    public void SetUseAutoTrader(ToggleSwitch toggle, bool isOn)
    {
        this.useAutoTrader = isOn;
        TradeResourceChanged?.Invoke(resource);
    }

    public void SetSell(ToggleSwitch toggle, bool isOn)
    {
        this.sell = isOn;
        TradeResourceChanged?.Invoke(resource);
    }

    internal void SetStockPile(float value)
    {
        this.stockPile = (int)value * multiplier;
        TradeResourceChanged?.Invoke(resource);
    }
}
