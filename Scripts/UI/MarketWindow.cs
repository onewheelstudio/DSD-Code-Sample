using HexGame.Resources;
using HexGame.Units;
using JetBrains.Annotations;
using Nova;
using Nova.Animations;
using NovaSamples.UIControls;
using OWS.Nova;
using OWS.ObjectPooling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.ProbeAdjustmentVolume;

public class MarketWindow : WindowPopup, ISaveData
{
    [SerializeField] private GameObject itemInfoPrefab;
    [SerializeField] private Transform resourceInfoParent;
    private List<MarketResourceItemInfo> items = new List<MarketResourceItemInfo>();

    [SerializeField] private Button confirmButton;
    private bool canTrade = true;
    [SerializeField] private TextBlock confirmButtonText;
    public static event Action<DirectiveQuest> TradeConfirmed;

    [SerializeField] private ToggleSwitch buyOrSell;
    [SerializeField] private Color buyColor;
    [SerializeField] private Color sellColor;
    private UIBlock2D marketBackground;

    [Header("Import Bits")]
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

    [Header("Unlock and Open Bits")]
    [SerializeField] private Button openButton;
    private UIBlock2D openButtonBlock;
    private AnimationHandle animationHandle;

    [Header("Supply Ships")]
    [SerializeField] private ListView supplyShipList;
    [SerializeField] private Button assignShipButton;
    [SerializeField] private UIBlock2D dropDownArrow;
    private TextBlock assignShipText;
    [SerializeField] private Button importButton;
    [SerializeField] private UIBlock2D importIcon;
    [SerializeField] private TextBlock importCost;
    [SerializeField] private GameObject importControls;
    private SupplyShipManager ssm;
    public static Action<ResourceType> marketResourceSet;
    public static Action<int, int> orderPriceSet;

    private void Awake()
    {
        stockMarket = FindFirstObjectByType<StockMarket>();
        directiveMenu = FindFirstObjectByType<DirectiveMenu>();
        playerResources = FindFirstObjectByType<PlayerResources>();
        marketBackground = this.gameObject.GetComponent<UIBlock2D>();
        openButtonBlock = openButton.GetComponent<UIBlock2D>();
        ButtonOff();//ensure this is turned off to start the game

        CheatCodes.AddButton(() => { UnlockWindow(); OpenWindow(); }, "Open Market");

        ssm = FindFirstObjectByType<SupplyShipManager>();
        supplyShipList.AddDataBinder<SupplyShipBehavior, SupplyShipDropdownVisuals>(BindSupplyShips);

        sliderChunkPool = new ObjectPool<PoolObject>(sliderChunkPrefab);
        RegisterDataSaving();

        assignShipText = assignShipButton.GetComponentInChildren<TextBlock>();
    }



    private void Start()
    {
        CreateResourceInfoButtons();//needs to be in start to allow stock market to be initialized
        novaGroup.UpdateInteractables();
        assignShipText.Text = "-None-";
        importButton.gameObject.SetActive(false);

        CloseWindow();
    }

    private new void OnEnable()
    {
        base.OnEnable();

        loadSlider.ValueChanged += SetOrderVolume;

        confirmButton.Clicked += ConfirmTrade;
        buyOrSell.Toggled += (t, b) => UpdateUI();

        StockMarket.allPricesUpdated += UpdateOrder;
        StockMarket.allPricesUpdated += UpdateUI;
        UnlockStockMarketButton.unlockStockMarketButton += UnlockWindow;
        LockDirectiveButton.lockDirectiveButton += ButtonOff;

        SupplyShipBehavior.supplyShipAdded += PopulateSupplyShips;
        SupplyShipBehavior.supplyShipRemoved += PopulateSupplyShips;
        AllowedResourceWindow.AllowedResourcesChanged += PopulateSupplyShips;

        assignShipButton.Clicked += ToggleShipSelection;
        PCInputManager.OnPostClick += CheckClickOnShipAssign;

        SetResource(ResourceType.Food);
        SetOrderVolume(1);
    }


    private new void OnDisable()
    {
        base.OnDisable();
        confirmButton.RemoveClickListeners();

        confirmButton.RemoveClickListeners();
        buyOrSell.RemoveAllListeners();

        StockMarket.allPricesUpdated -= UpdateOrder;
        StockMarket.allPricesUpdated -= UpdateUI;
        UnlockStockMarketButton.unlockStockMarketButton -= UnlockWindow;
        LockDirectiveButton.lockDirectiveButton -= ButtonOff;

        SupplyShipBehavior.supplyShipAdded -= PopulateSupplyShips;
        SupplyShipBehavior.supplyShipRemoved -= PopulateSupplyShips;
        AllowedResourceWindow.AllowedResourcesChanged -= PopulateSupplyShips;

        assignShipButton.Clicked -= ToggleShipSelection;
        PCInputManager.OnPostClick -= CheckClickOnShipAssign;
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

        SetupImportButton(resource, priceOfOrder);

        totalText.Text = $"<b>Cost:</b> {priceOfOrder + fees}";
        totalText.Color = ColorManager.GetColor(ColorCode.red);
        marketBackground.Shadow.Color = buyColor;
        confirmButtonText.Text = "Buy";
        paymentText.Text = "*Costs paid when order is confirmed.";

        float basePrice = stockMarket.GetResourceBasePrice(resource);
        float upperPrice = Mathf.CeilToInt(basePrice * 2f);
        upperValue.Text = (upperPrice).ToString();

        UpdateGraphBars(stockMarket.GetPriceHistory(resource), 0, basePrice, upperPrice, template.resourceColor);

        UpdateResourceVisibility();

        int numberOfLoads = GetMaxBuyLoads();
        loadSlider.Max = numberOfLoads;
        DoSliderChunks(numberOfLoads);

    }

    private void SetupImportButton(ResourceType type, int price)
    {
        var resourceTemplate = playerResources.GetResourceTemplate(type);
        importIcon.SetImage(resourceTemplate.icon);
        importIcon.Color = resourceTemplate.resourceColor;
        //importCost.Text = price.ToString();
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
        //if(!buyOrSell.ToggledOn)
        //{
        foreach (MarketResourceItemInfo item in items)
        {
            if (item.resource == ResourceType.Terrene)
                item.transform.gameObject.SetActive(false);
            else
                item.transform.gameObject.SetActive(true);
        }

        return;
        //}

        //if we are selling only show resources we have produced or have in storage
        List<ResourceType> resourceTypes = new();
        if (PlayerResources.producedResources.Count > 0)
            resourceTypes = PlayerResources.producedResources.Select(x => x.type).ToList();
        List<ResourceType> storedResources = new();
        if (PlayerResources.resourceStored.Count > 0)
            storedResources = PlayerResources.resourceStored.Where(x => x.amount > 0).Select(x => x.type).ToList();

        //toggle items to sell
        foreach (MarketResourceItemInfo item in items)
        {
            if (resourceTypes.Contains(item.resource) || storedResources.Contains(item.resource))
                item.transform.gameObject.SetActive(true);
            else
            {
                item.transform.gameObject.SetActive(false);
                if (resource == item.resource) //if we can't sell current resource change current resource to food
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
            int numberOfSupplyShips = Mathf.Max(1, FindObjectsOfType<SupplyShipBehavior>().Count());
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
        if (value < 10)
            return (float)Math.Round(value, 2);
        else if (value < 100)
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
        if (load == 0)
            return 1;
        else if (load > 10)
            return 10;
        else
            return load;
    }

    private void ConfirmTrade()
    {
        if (volumeOfOrder == 0 || !canTrade)
            return;

        if (!directiveMenu.CanAddQuest())
        {
            MessagePanel.ShowMessage($"Maximum number of transactions is {directiveMenu.MaxQuests}", null);
            return;
        }

        int price;
        //if (buyOrSell.ToggledOn)
        //{
        //    price = Mathf.RoundToInt(priceOfOrder - fees);

        //}
        //else
        //{
        price = Mathf.RoundToInt(priceOfOrder + fees);
        if (HexTechTree.TechCredits < price)
        {
            MessagePanel.ShowMessage("Insufficient credits to complete the order.", null);
            return;
        }
        else
            HexTechTree.ChangeTechCredits(-price);
        //}


        DirectiveQuest quest = ScriptableObject.CreateInstance<DirectiveQuest>();
        quest.Setup(new List<ResourceAmount>() { new ResourceAmount(resource, volumeOfOrder * 50) }, 10 * volumeOfOrder, price);
        //if (buyOrSell.ToggledOn) //only use time limits  for selling
        //{
        //    //quest.SetTimeLimit(GetTimeForOrder());
        //    stockMarket.SellResource(resource, volumeOfOrder * 50);
        //}
        //else
        stockMarket.SellResource(resource, -volumeOfOrder * 50);

        quest.requestType = buyOrSell.ToggledOn ? RequestType.sell : RequestType.buy;
        quest.isCorporate = false;
        directiveMenu.TryAddQuest(quest);
        TradeConfirmed?.Invoke(quest);
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
        if (loadSlider.Value > loadSlider.Max)
            loadSlider.Value = loadSlider.Max;

        UpdateOrder();
        UpdateUI();
        marketResourceSet?.Invoke(resource);
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

        //if(buyOrSell.ToggledOn)
        //    fees = Mathf.RoundToInt((volumeOfOrder * 50 * priceOfResource - bonus) * feeRate);
        //else
        fees = Mathf.RoundToInt((volumeOfOrder * 50 * priceOfResource + bonus) * feeRate);

        orderPriceSet?.Invoke(priceOfOrder, volumeOfOrder);
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
            if (resource == null)
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

            itemInfo.GetComponent<Button>().Clicked += () => SetResource(resource.type);
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

        if (!animationHandle.IsComplete())
        {
            animationHandle.Complete();
            openButtonBlock.Color = Color.white;
        }

        base.OpenWindow();
        PopulateSupplyShips();
        volumeOfOrder = 1;
        loadSlider.Value = volumeOfOrder;
        loadSlider.Max = GetMaxSellLoads();
        buyOrSell.ToggledOn = true;
        UpdateOrder();
        UpdateUI();
        CloseShipSelection();
    }

    override public void CloseWindow()
    {
        CloseShipSelection();
        base.CloseWindow();
    }

    private void UnlockWindow()
    {
        marketMenuUnlocked = true;
        if (!StateOfTheGame.tutorialSkipped && !SaveLoadManager.Loading)
            OpenWindow();
        ButtonOn();
    }

    private void SetProgressBarChunks(Data.OnBind<int> evt, ProgressBarChunkVisuals target, int index)
    {
        //nothing to do!
    }

    private void ButtonOff()
    {
        openButton.GetComponent<Interactable>().ClickBehavior = ClickBehavior.None;
        openButtonBlock.Color = ColorManager.GetColor(ColorCode.buttonGreyOut);
    }

    private void ButtonOn()
    {
        Interactable interactable = openButton.GetComponent<Interactable>();
        if (interactable.ClickBehavior == ClickBehavior.OnRelease)
            return; //we're already on

        openButton.GetComponent<Interactable>().ClickBehavior = ClickBehavior.OnRelease;

        if (!StateOfTheGame.tutorialSkipped && !SaveLoadManager.Loading)
        {
            ButtonHighlightAnimation animation = new ButtonHighlightAnimation()
            {
                startSize = new Vector3(50, 50, 0),
                endSize = new Vector3(50, 50, 0) * 1.1f,
                startColor = ColorManager.GetColor(ColorCode.callOut),
                endColor = ColorManager.GetColor(ColorCode.callOut),
                endAlpha = 0.5f,
                uIBlock = openButtonBlock
            };

            ButtonIndicator.IndicatorButton(openButtonBlock);
            animationHandle = animation.Loop(1f, -1);
        }
        else
        {
            openButtonBlock.Color = Color.white;
        }
    }


    private async void PopulateSupplyShips(ITransportResources resources)
    {
        await Awaitable.NextFrameAsync();
        PopulateSupplyShips();
    }

    private async void PopulateSupplyShips(SupplyShipBehavior behavior)
    {
        await Awaitable.NextFrameAsync();
        PopulateSupplyShips();
    }

    private void PopulateSupplyShips()
    {
        if (!supplyShipList.gameObject.activeInHierarchy)
            return;

        var ships = ssm.GetSupplyShips().ToList();
        if(ships.Count == 0)
            assignShipText.Text = "-None-";
        importButton.gameObject.SetActive(ships.Count > 0);
        supplyShipList.SetDataSource(ssm.GetSupplyShips().ToList());
    }

    private void BindSupplyShips(Data.OnBind<SupplyShipBehavior> evt, SupplyShipDropdownVisuals target, int index)
    {
        target.Initialize();
        target.Label.Text = PlayerUnitType.supplyShip.ToNiceString() + " " + (index + 1).ToString();
        target.PopulateAllowedResources(evt.UserData.SSB);
        target.SetLocation(evt.UserData.SSB.Position);
        target.selectShipButton.RemoveAllListeners(); //clean up
        target.selectShipButton.Clicked += () => AssignShipForImport(evt.UserData, target.Label.Text);
        target.selectShipButton.Clicked += CloseShipSelection;
    }

    public void AssignShipForImport(SupplyShipBehavior supplyShip, string shipName)
    {
        assignShipText.Text = shipName;
        importButton.RemoveClickListeners();
        importButton.Clicked += () => supplyShip.ImportResource(resource, volumeOfOrder);
        importButton.Clicked += () => MessagePanel.ShowMessage($"Assigned {shipName} to import {volumeOfOrder * 50} {resource.ToNiceString()}", null);
    }

    private void ToggleShipSelection()
    {
        if (supplyShipList.gameObject.activeInHierarchy)
            CloseShipSelection();
        else
            OpenShipSelection();
    }

    private void OpenShipSelection()
    {
        supplyShipList.gameObject.SetActive(true);
        dropDownArrow.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        PopulateSupplyShips();
    }

    public void CloseShipSelection()
    {
        dropDownArrow.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        supplyShipList.gameObject.SetActive(false);
    }

    private void CheckClickOnShipAssign(UIBlock block)
    {
        if (!supplyShipList.gameObject.activeInHierarchy)
            return;

        if (block == null)
            CloseShipSelection();
        else if (block.transform.IsChildOf(supplyShipList.transform) 
            || block.gameObject == assignShipButton.gameObject
            || block.transform.IsChildOf(assignShipButton.transform))
            return;
        else
            CloseShipSelection();
    }

    private const string MARKET_UNLOCKED = "MarketUnlocked";
    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this);
    }

    public void Save(string savePath, ES3Writer writer)
    {
        writer.Write<bool>(MARKET_UNLOCKED, marketMenuUnlocked);
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if (ES3.KeyExists(MARKET_UNLOCKED, loadPath))
            marketMenuUnlocked = ES3.Load(MARKET_UNLOCKED, loadPath, false);

        if (marketMenuUnlocked)
            UnlockWindow();

        yield return null;
    }
}
