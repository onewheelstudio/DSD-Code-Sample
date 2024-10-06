using HexGame.Resources;
using Nova;
using NovaSamples.UIControls;
using OWS.Nova;
using OWS.ObjectPooling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarketWindow : WindowPopup
{
    [SerializeField] private GameObject itemInfoPrefab;
    [SerializeField] private Transform resourceInfoParent;
    private List<MarketResourceItemInfo> items = new List<MarketResourceItemInfo>();

    [SerializeField] private Button confirmButton;
    private bool canTrade = true;
    [SerializeField] private TextBlock confirmButtonText;

    [SerializeField] private ToggleSwitch buyOrSell;
    [SerializeField] private Color buyColor;
    [SerializeField] private Color sellColor;
    private UIBlock2D marketBackground;

    [Header("Volume Buttons")]
    [SerializeField] private Button plus1;
    [SerializeField] private Button plus5;
    [SerializeField] private Button plus10;
    [SerializeField] private Button maxLoads;
    [SerializeField] private Slider loadSlider;
    [SerializeField] private UIBlock2D sliderFill;
    [SerializeField] private Transform sliderBackground;
    [SerializeField] private GameObject sliderChunkPrefab;
    private ObjectPool<PoolObject> sliderChunkPool;
    private List<PoolObject> activeSliderChunks = new List<PoolObject>();

    [Header("Current Order")]
    [SerializeField] private ResourceType resource;
    [SerializeField] private int volumeOfOrder = 0;
    [SerializeField] private int priceOfOrder = 0;
    [SerializeField] private int bonus;
    [SerializeField] private int fees;
    [SerializeField] private float feeRate = 0.1f;
    [SerializeField] private float priceOfResource;

    [Header("OrderInfo")]
    [SerializeField] private TextBlock headerText;
    [SerializeField] private TextBlock unitsText;
    [SerializeField] private TextBlock shipmentsText;
    [SerializeField] private TextBlock priceText;
    [SerializeField] private TextBlock feesText;
    [SerializeField] private TextBlock totalText;
    [SerializeField] private TextBlock bonusText;
    [SerializeField] private TextBlock paymentText;

    [Header("Icons")]
    [SerializeField] private UIBlock2D headerIcon;
    [SerializeField] private UIBlock2D orderIcon;
    private PlayerResources playerResources;

    [Header("Bar Graphs")]
    [SerializeField] private List<UIBlock2D> bars = new List<UIBlock2D>();
    [SerializeField] private TextBlock upperValue;
    [SerializeField] private TextBlock lowerValue;
    [SerializeField] private UIBlock2D barParent;
    private StockMarket stockMarket;
    private DirectiveMenu directiveMenu;
    private bool marketMenuUnlocked = false;

    private void Awake()
    {
        stockMarket = FindFirstObjectByType<StockMarket>();
        directiveMenu = FindObjectOfType<DirectiveMenu>();
        playerResources = FindObjectOfType<PlayerResources>();
        marketBackground = this.gameObject.GetComponent<UIBlock2D>();
        
        CheatCodes.AddButton(() => { UnlockWindow(); OpenWindow(); }, "Open Market");

        sliderChunkPool = new ObjectPool<PoolObject>(sliderChunkPrefab);
    }



    private void Start()
    {
        CreateResourceInfoButtons();//needs to be in start to allow stock market to be initialized
        novaGroup.UpdateInteractables();
        CloseWindow();
    }

    private new void OnEnable()
    {
        base.OnEnable();

        plus1.clicked += () => SetOrderVolume(1);
        plus5.clicked += () => SetOrderVolume(5);
        plus10.clicked += () => SetOrderVolume(10);
        maxLoads.clicked += SetMaxOrderVolume;

        loadSlider.ValueChanged += SetOrderVolume;
        
        confirmButton.clicked += ConfirmTrade;
        buyOrSell.Toggled += (t,b) => UpdateUI();

        StockMarket.allPricesUpdated += UpdateOrder;
        StockMarket.allPricesUpdated += UpdateUI;
        UnlockStockMarketButton.unlockStockMarketButton += UnlockWindow;

        SetResource(ResourceType.Food);
        SetOrderVolume(1);
    }


    private new void OnDisable()
    {
        base.OnDisable();
        confirmButton.RemoveClickListeners();

        plus1.RemoveClickListeners();
        plus5.RemoveClickListeners();
        plus10.RemoveClickListeners();
        maxLoads.RemoveClickListeners();

        confirmButton.RemoveClickListeners();
        buyOrSell.RemoveAllListeners();

        StockMarket.allPricesUpdated -= UpdateOrder;
        StockMarket.allPricesUpdated -= UpdateUI;
        UnlockStockMarketButton.unlockStockMarketButton -= UnlockWindow;
    }

    private void UpdateUI()
    {
        if (!instanceIsOpen)
            return;

        ResourceTemplate template = playerResources.GetResourceTemplate(resource);
        headerIcon.SetImage(template.icon);
        headerIcon.Color = template.resourceColor;
        headerText.Text = $"{resource.ToNiceString()} | {Round(stockMarket.GetResourcePrice(resource))}";// {GetBonusText()}";
        orderIcon.SetImage(template.icon);
        orderIcon.Color = template.resourceColor;
        sliderFill.Color = template.resourceColor;

        unitsText.Text = $"<b>Amount:</b> {volumeOfOrder * 50}";
        shipmentsText.Text = $"<b>Shipments:</b> {volumeOfOrder}";
        priceText.Text = $"<b>Price:</b> {priceOfOrder}";
        feesText.Text = $"<b>Fees:</b> {fees}";
        //bonusText.transform.parent.gameObject.SetActive(volumeOfOrder >= 5);
        //bonusText.Text = $"<b>Bonus:</b> {bonus}";

        if (buyOrSell.ToggledOn)
        {
            totalText.Text = $"<b>Profit:</b> {priceOfOrder - fees}";
            totalText.Color = ColorManager.GetColor(ColorCode.green);
            marketBackground.Shadow.Color = sellColor;
            confirmButtonText.Text = "Sell";
            paymentText.Text = "*Profits paid when shipments are complete.";
        }
        else
        {
            totalText.Text = $"<b>Cost:</b> {priceOfOrder + fees}";
            totalText.Color = ColorManager.GetColor(ColorCode.red);
            marketBackground.Shadow.Color = buyColor;
            confirmButtonText.Text = "Buy";
            paymentText.Text = "*Costs paid when order is confirmed.";
        }

        float basePrice = stockMarket.GetResourceBasePrice(resource);
        float upperPrice = Mathf.CeilToInt(basePrice * 2f);
        upperValue.Text = (upperPrice).ToString();
        //float lowerPrice = Round(stockMarket.GetResourceBasePrice(resource) * 0.5f, 2);
        //lowerValue.Text = Round(lowerPrice, 2).ToString();

        UpdateGraphBars(stockMarket.GetPriceHistory(resource), 0, basePrice, upperPrice, template.resourceColor);

        UpdateResourceVisibility();

        if (!buyOrSell.ToggledOn)
        {
            int numberOfLoads = GetMaxBuyLoads();
            loadSlider.Max = numberOfLoads;
            DoSliderChunks(numberOfLoads);
        }
        else
        {
            int numberOfLoads = GetMaxSellLoads();
            loadSlider.Max = numberOfLoads;
            DoSliderChunks(numberOfLoads);
        }
    }

    private void DoSliderChunks(int numberOfLoads)
    {
        if (numberOfLoads > activeSliderChunks.Count)
        {
            for (int i = activeSliderChunks.Count; i < numberOfLoads; i++)
            {
                PoolObject chunk = sliderChunkPool.Pull();
                chunk.transform.SetParent(sliderBackground.transform);
                chunk.transform.localScale = Vector3.one;
                activeSliderChunks.Add(chunk);
            }
        }
        else if (numberOfLoads < activeSliderChunks.Count)
        {
            for (int i = activeSliderChunks.Count - 1; i >= numberOfLoads; i--)
            {
                activeSliderChunks[i].gameObject.SetActive(false);
                activeSliderChunks.RemoveAt(i);
            }
        }
    }

    private void UpdateResourceVisibility()
    {
        //if we are buying show all resources
        if(!buyOrSell.ToggledOn)
        {
            foreach (MarketResourceItemInfo item in items)
            {
                if(item.resource == ResourceType.Terrene)
                    item.transform.gameObject.SetActive(false);
                else
                    item.transform.gameObject.SetActive(true);
            }

            return;
        }

        //if we are selling only show resources we have produced or have in storage
        List<ResourceType> resourceTypes = PlayerResources.producedResources.Select(x => x.type).ToList();
        List<ResourceType> storedResources = PlayerResources.resourceStored.Where(x => x.amount > 0).Select(x => x.type).ToList();

        //toggle items to sell
        foreach (MarketResourceItemInfo item in items)
        {
            if (resourceTypes.Contains(item.resource) || storedResources.Contains(item.resource))
                item.transform.gameObject.SetActive(true);
            else
            {
                item.transform.gameObject.SetActive(false);
                if(resource == item.resource) //if we can't sell current resource change current resource to food
                    SetResource(ResourceType.Food);
            }
        }
    }

    private string GetBonusText()
    {
        if (volumeOfOrder >= 10)
            return " 10% Volume Bonus";
        else if (volumeOfOrder >= 5)
            return " 5% Volume Bonus";
        else
            return "";
    }

    private float GetTimeForOrder()
    {
        if (volumeOfOrder == 1)
            return 0;
        else
        {
            int numberOfSupplyShips = Mathf.Max(1,FindObjectsOfType<SupplyShipBehavior>().Count());
            return volumeOfOrder * GameConstants.timePerShipment / numberOfSupplyShips;
        }
    }

    private void UpdateGraphBars(float[] priceHistory, float min, float basePrice, float max, Color resourceColor)
    {
        resourceColor.a = 0.2f;
        barParent.Shadow.Color = resourceColor;

        for (int i = 0; i < priceHistory.Length; i++)
        {
            float percent = (priceHistory[i] - 0) / (max - min);
            bars[i].Size.Y.Percent = percent;
        }
    }

    /// <summary>
    /// round so the value is 3 digits total
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private float Round(float value)
    {
        if(value < 10)
            return (float)Math.Round(value, 2);
        else if(value < 100)
            return (float)Math.Round(value, 1);
        else
            return Mathf.RoundToInt(value);
    }


    private void SetOrderVolume(float sliderValue)
    {
        SetOrderVolume(Mathf.RoundToInt(sliderValue));
    }

    private void SetOrderVolume(int volume)
    {
        volumeOfOrder = volume;
        UpdateOrder();
        UpdateUI();
    }

    private void SetMaxOrderVolume()
    {
        if(buyOrSell.ToggledOn)
        {
            volumeOfOrder = GetMaxSellLoads();
        }
        else
        {
            volumeOfOrder = 1;
        }

        UpdateOrder();
        UpdateUI();
    }

    private int GetMaxSellLoads()
    {
        int amountStored = PlayerResources.GetAmountStored(resource);
        int load = Mathf.FloorToInt(amountStored / 50);
        if (load == 0)
            return 1;
        else if (load > 10)
            return 10;
        else
            return load;
    }

    private int GetMaxBuyLoads()
    {
        int load = Mathf.FloorToInt(HexTechTree.TechCredits / (priceOfResource * 50));
        if(load == 0)
            return 1;
        else if(load > 10)
            return 10;
        else
            return load;
    }

    private void ConfirmTrade()
    {
        if (volumeOfOrder == 0 || !canTrade)
            return;

        if(!directiveMenu.CanAddQuest())
        {
            MessagePanel.ShowMessage($"Maximum number of transactions is {directiveMenu.MaxQuests}", null);
            return;
        }

        int price;
        if (buyOrSell.ToggledOn)
        {
            price = Mathf.RoundToInt(priceOfOrder - fees);

        }
        else
        {
            price = Mathf.RoundToInt(priceOfOrder + fees);
            if(HexTechTree.TechCredits < price)
            {
                MessagePanel.ShowMessage("Insufficient credits to complete the order.", null);
                return;
            }
            else
                HexTechTree.ChangeTechCredits(-price);
        }


        DirectiveQuest quest = ScriptableObject.CreateInstance<DirectiveQuest>();
        quest.Setup(new List<ResourceAmount>() { new ResourceAmount(resource, volumeOfOrder * 50) }, 10 * volumeOfOrder, price);
        if (buyOrSell.ToggledOn) //only use time limits  for selling
        {
            //quest.SetTimeLimit(GetTimeForOrder());
            stockMarket.SellResource(resource, volumeOfOrder * 50);
        }
        else
            stockMarket.SellResource(resource, -volumeOfOrder * 50); 

        quest.buyOrSell = buyOrSell.ToggledOn ? RequestType.sell : RequestType.buy;
        quest.isCorporate = false;
        directiveMenu.TryAddQuest(quest);
        StartCoroutine(TradeDelay());
        SFXManager.PlaySFX(SFXType.newDirective, true);
    }

    private IEnumerator TradeDelay()
    {
        canTrade = false;
        yield return new WaitForSeconds(0.25f);
        canTrade = true;
    }

    private void SetResource(ResourceType resource)
    {
        this.resource = resource;
        loadSlider.Max = GetMaxSellLoads();
        if(loadSlider.Value > loadSlider.Max)
            loadSlider.Value = loadSlider.Max;

        UpdateOrder();
        UpdateUI();
    }

    private void UpdateOrder()
    {
        this.priceOfResource = stockMarket.GetResourcePrice(resource);
        priceOfOrder = Mathf.RoundToInt(volumeOfOrder * 50 * priceOfResource);

        bonus = 0;
        if (volumeOfOrder >= 10)
            bonus = Mathf.RoundToInt(priceOfOrder * 0.1f);
        else if (volumeOfOrder >= 5)
            bonus = Mathf.RoundToInt(priceOfOrder * 0.05f);
        else
            bonus = 0;

        if(buyOrSell.ToggledOn)
            fees = Mathf.RoundToInt((volumeOfOrder * 50 * priceOfResource - bonus) * feeRate);
        else
            fees = Mathf.RoundToInt((volumeOfOrder * 50 * priceOfResource + bonus) * feeRate);
    }

    private void CreateResourceInfoButtons()
    {
        int count = 0;
        foreach (ResourceTemplate resource in PlayerResources.GetResourceList())
        {
            if (resource.type == ResourceType.Workers)
                continue;

            GameObject itemInfo = Instantiate(itemInfoPrefab, resourceInfoParent);
            ItemView itemView = itemInfo.GetComponent<ItemView>();
            MarketResourceItemInfo visuals = itemView.Visuals as MarketResourceItemInfo;
            visuals.transform = itemInfo.transform;
            visuals.index = count;
            count++;

            visuals.resource = resource.type;
            if(resource == null)
            {
                Debug.LogError($"ResourceTemplate not found for {resource}");
                continue;
            }

            if (resource.icon != null)
            {
                visuals.icon.SetImage(resource.icon);
                visuals.icon.Color = resource.resourceColor;
            }

            visuals.SetPrice(resource.type, stockMarket.GetMarket(resource.type));
            StockMarket.resourcePriceUpdated += visuals.SetPrice;
            items.Add(visuals);

            visuals.starToggle.toggled += StarToggled;

            itemInfo.GetComponent<Button>().clicked += () => SetResource(resource.type);
        }
    }

    private void StarToggled(Toggle toggle, bool arg2)
    {
        items = items.OrderByDescending(x => x.starToggle.ToggledOn).ThenBy(x => x.index).ToList();
        for (int i = 0; i < items.Count; i++)
        {
            items[i].transform.SetSiblingIndex(i);
        }
    }

    override public void OpenWindow()
    {
        if (!marketMenuUnlocked)
            return;

        base.OpenWindow();
        volumeOfOrder = 1;
        loadSlider.Value = volumeOfOrder;
        loadSlider.Max = GetMaxSellLoads();
        buyOrSell.ToggledOn = true;
        UpdateOrder();
        UpdateUI();
    }

    override public void CloseWindow()
    {
        base.CloseWindow();
    }

    private void UnlockWindow()
    {
        marketMenuUnlocked = true;
    }

    private void SetProgressBarChunks(Data.OnBind<int> evt, ProgressBarChunkVisuals target, int index)
    {
        //nothing to do!
    }

}
