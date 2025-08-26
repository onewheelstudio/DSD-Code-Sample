using HexGame.Resources;
using Nova.Animations;
using Nova;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NovaSamples.UIControls;

public class AutoTraderWindow : WindowPopup, ISaveData
{
    [SerializeField] private Transform resourcelist;
    [SerializeField] private TradeResourceUI resourcePrefab;
    [SerializeField] private Dictionary<ResourceType, TradeResourceUI> tradeResources = new();
    private bool autoTraderUnlocked = false;
    [SerializeField] private Button openButton;
    private UIBlock2D openButtonBlock;
    private AnimationHandle animationHandle;

    private DirectiveMenu directiveMenu;
    private StockMarket stockMarket;
    public static event Action<DirectiveQuest> TradeConfirmed;
    private DirectiveQuest currentQuest;
    private bool canPlaceTrade => activeAutoTrades.Count < maxAutoTrades && autoTraderUnlocked;
    private Queue<ResourceType> tradeQueue = new Queue<ResourceType>();
    private WaitForSeconds tradeDelay = new WaitForSeconds(1f);

    private SupplyShipManager supplyShipManager;
    private int maxAutoTrades
    {
        get
        {
            if(supplyShipManager == null)
                supplyShipManager = FindFirstObjectByType<SupplyShipManager>();

            return Mathf.Max(1, supplyShipManager.SupplyShipCount / 2); //1 auto trader per 2 supply ships
        }
    }
    private List<DirectiveQuest> activeAutoTrades = new List<DirectiveQuest>();

    private void Awake()
    {
        CreateTradeInfo();
        directiveMenu = FindFirstObjectByType<DirectiveMenu>();
        stockMarket = FindFirstObjectByType<StockMarket>();
        openButtonBlock = openButton.GetComponent<UIBlock2D>();
        ButtonOff();
        RegisterDataSaving();
    }

    private void Start()
    {
        novaGroup.UpdateInteractables();
        CloseWindow();
    }

    private new void OnEnable()
    {
        base.OnEnable();
        PlayerResources.resourceChange += ResourceChanged;
        TradeInfo.TradeResourceChanged += TradeInfoChanged;
        UnlockAutoTrader.OnUnlockAutoTrader += UnlockWindow;
    }

    private new void OnDisable()
    {
        base.OnDisable();
        PlayerResources.resourceChange -= ResourceChanged;
        TradeInfo.TradeResourceChanged -= TradeInfoChanged;
        UnlockAutoTrader.OnUnlockAutoTrader -= UnlockWindow;
        StopAllCoroutines();
    }



    private void ResourceChanged(ResourceType type, int amount)
    {
        TradeInfoChanged(type);
    }
    private void TradeInfoChanged(ResourceType type)
    {
        if (tradeQueue.Contains(type))
            return;

        if (CanTradeResource(type))
            tradeQueue.Enqueue(type);
    }

    private void QuestChanged(DirectiveQuest quest)
    {
        activeAutoTrades.Remove(quest);
    }

    private void CreateTradeInfo()
    {
        foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
        {
            if (resource == ResourceType.Workers)
                continue;
            TradeResourceUI newResource = Instantiate(resourcePrefab, resourcelist);
            newResource.SetUpTradeResource(resource);
            newResource.ToggleTrading(null, false);
            tradeResources.Add(resource, newResource);
        }
    }

    private void UnlockWindow()
    {
        autoTraderUnlocked = true;
        StartCoroutine(DoTrades());
        ButtonOn();
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

        if (!StateOfTheGame.tutorialSkipped || !SaveLoadManager.Loading)
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

            animationHandle = animation.Loop(1f, -1);
        }
        else
        {
            openButtonBlock.Color = Color.white;
        }
    }

    public override void ToggleWindow()
    {
        if (!autoTraderUnlocked)
            return;
        base.ToggleWindow();
    }

    public override void OpenWindow()
    {
        if (!autoTraderUnlocked)
            return;

        if (!animationHandle.IsComplete())
        {
            animationHandle.Complete();
            openButtonBlock.Color = Color.white;
        }

        base.OpenWindow();
    }

    private IEnumerator DoTrades()
    {
        while (true)
        {
            yield return null;
            yield return new WaitUntil(() => canPlaceTrade);
            CheckForTrades();
            if (tradeQueue.Count == 0)
            {
                continue;
            }
            
            if (!canPlaceTrade)
                continue;

            ResourceType resource = tradeQueue.Dequeue();

            if (!CanTradeResource(resource))
            {
                tradeQueue.Enqueue(resource);
                continue;
            }

            TryMakeTrade(tradeResources[resource].TradeInfo);

            yield return tradeDelay;
        }
    }

    private void CheckForTrades()
    {
        foreach(var trade in tradeResources)
        {
            TradeInfo tradeInfo = trade.Value.TradeInfo;
            if (!tradeInfo.useAutoTrader)
                continue;

            if (tradeQueue.Contains(tradeInfo.resource))
                continue;

            if (CanTradeResource(tradeInfo.resource))
                tradeQueue.Enqueue(tradeInfo.resource);
        }
    }

    private bool CanTradeResource(ResourceType resource)
    {
        if(!tradeResources.TryGetValue(resource, out var TradeResourceUI))
            return false;

        if (!TradeResourceUI.TradeInfo.useAutoTrader)
            return false;

        TradeInfo tradeInfo = TradeResourceUI.TradeInfo;
        int amountStored = PlayerResources.GetAmountStored(tradeInfo.resource);
        TradeResourceUI.ToggleNotification(false);

        if (tradeInfo.sell && amountStored >= tradeInfo.stockPile + SupplyShipManager.supplyShipCapacity)
        {
            return true;
        }
        else if (!tradeInfo.sell && amountStored <= tradeInfo.stockPile && TradeResourceUI.CanAffordToBuy())
        {
            return true;
        }
        
        return false;
    }

    [Button]
    private bool TryMakeTrade(TradeInfo tradeInfo)
    {
        float priceOfResource = stockMarket.GetResourcePrice(tradeInfo.resource);
        int price = Mathf.RoundToInt(SupplyShipManager.supplyShipCapacity * priceOfResource);

        //setup quest
        DirectiveQuest quest = ScriptableObject.CreateInstance<DirectiveQuest>();
        quest.Setup(new List<ResourceAmount>() { new ResourceAmount(tradeInfo.resource, SupplyShipManager.supplyShipCapacity) }, 0, price);
        quest.requestType = tradeInfo.sell ? RequestType.sell : RequestType.buy;
        quest.isCorporate = false;
        quest.isAutoTrader = true;
        quest.Completed += QuestChanged;
        quest.failed += QuestChanged;

        //are we able to add a quest?
        if (!directiveMenu.TryAddQuest(quest))
        {
            quest.Completed -= QuestChanged;
            quest.failed -= QuestChanged;
            return false;
        }

        //Have stock market adjust prices for the trade
        if (tradeInfo.sell)
            stockMarket.SellResource(tradeInfo.resource, SupplyShipManager.supplyShipCapacity);
        else
        {
            //only pay for buying - selling price is done when complete
            HexTechTree.ChangeTechCredits(-price); 
            stockMarket.SellResource(tradeInfo.resource, -SupplyShipManager.supplyShipCapacity);
        }

        activeAutoTrades.Add(quest);
        TradeConfirmed?.Invoke(quest);
        SFXManager.PlaySFX(SFXType.newDirective, true);
        return true;
    }

    private const string AUTO_TRADER_DATA = "AutoTraderData";
    private const string AUTO_TRADER_UNLOCKED = "AutoTraderUnlocked";
    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this, 3); //must load after player resources
    }

    public void Save(string savePath, ES3Writer writer)
    {
        List<TradeInfo> saveTradeInfo = new List<TradeInfo>();
        foreach (var trade in tradeResources)
        {
            saveTradeInfo.Add(trade.Value.TradeInfo);
        }

        writer.Write<List<TradeInfo>>(AUTO_TRADER_DATA, saveTradeInfo);
        writer.Write<bool>(AUTO_TRADER_UNLOCKED, autoTraderUnlocked); 
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if(ES3.KeyExists(AUTO_TRADER_DATA, loadPath))
        {
            List<TradeInfo> loadTradeInfo = ES3.Load<List<TradeInfo>>(AUTO_TRADER_DATA, loadPath);
            foreach (var trade in loadTradeInfo)
            {
                tradeResources[trade.resource].SetUpTradeResource(trade);
            }
        }
        if(ES3.KeyExists(AUTO_TRADER_UNLOCKED, loadPath))
        {
            bool unlocked = ES3.Load<bool>(AUTO_TRADER_UNLOCKED, loadPath);
            if (unlocked)
            {
                UnlockWindow();
            }
        }
        return null;
    }

    public struct TradeData
    {
        public TradeData(TradeInfo tradeInfo)
        {
            this.resource = tradeInfo.resource;
            this.useAutoTrader = tradeInfo.useAutoTrader;
            this.sell = tradeInfo.sell;
            this.stockPile = tradeInfo.stockPile;
        }

        public ResourceType resource;
        public bool useAutoTrader;
        public bool sell;
        public int stockPile;
    }
}
