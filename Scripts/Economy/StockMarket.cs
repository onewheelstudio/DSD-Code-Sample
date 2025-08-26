using HexGame.Resources;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StockMarket : MonoBehaviour, ISaveData
{
    [SerializeField, ListDrawerSettings(NumberOfItemsPerPage = 1)]
    private List<ResourceMarket> resourceMarkets;

    private DayNightManager dayNightManager;
    private float dayLength;
    private WaitForSeconds wait = new WaitForSeconds(20f);
    public static event Action allPricesUpdated;
    public static event Action<ResourceType, ResourceMarket> resourcePriceUpdated;
    /// <summary>
    /// Returns the credits earned from the sale.
    /// </summary>
    public static event Action<int> resourceSold;
    [SerializeField] private Transform barParent;
    private PlayerResources playerResources;
    [SerializeField, Range(0f, 1f)]
    private float chanceForEvent = 0.5f;

    private void Awake()
    {
        RegisterDataSaving();
        dayNightManager = FindFirstObjectByType<DayNightManager>();
        dayLength = dayNightManager.DayLength / 60f;
        playerResources = FindFirstObjectByType<PlayerResources>();


        int barCount = 45;
        if(barParent == null)
            barParent = GameObject.Find("Bar Parent")?.transform;

        if(barParent)
            barCount = barParent.childCount;

        if (SaveLoadManager.Loading)
            return;

        CreateResources();
        foreach (ResourceMarket market in resourceMarkets)
        {
            ResourceTemplate resoureTemplate = playerResources.GetResourceTemplate(market.resourceType);
            if(resoureTemplate == null)
                continue;

            market.basePrice = resoureTemplate.baseCost * HexTileManager.GetNextFloat(0.8f,1.2f);
            market.currentPrice = market.basePrice;
            market.direction = HexTileManager.GetNextInt(0, 2) == 0 ? -1 : 1;
            market.priceHistory = new float[barCount];
            market.dailyVoliatility = HexTileManager.GetNextFloat(0.05f, 0.25f);

            for (int i = 0; i < barCount; i++)
            {
                DoMarketCycle(market);
            }
        }
    }

    private void OnEnable()
    {
        DayNightManager.toggleDay += MarketEvent;
        SupplyShipBehavior.LoadShipped += LoadShipped;
        PriceChangeTrigger.OnPriceChange += PriceChangeUntilDayNumber;
    }

    private void OnDisable()
    {
        DayNightManager.toggleDay -= MarketEvent;
        SupplyShipBehavior.LoadShipped -= LoadShipped;
        PriceChangeTrigger.OnPriceChange -= PriceChangeUntilDayNumber;
    }


    private void Start()
    {
        StartCoroutine(UpdatePrices());
    }

    [Button]
    private void CreateResources()
    {
        resourceMarkets.Clear();
        foreach (ResourceType resource in System.Enum.GetValues(typeof(ResourceType)))
        {
            if(resource == ResourceType.Workers)
                continue;

            ResourceMarket market = new ResourceMarket();
            market.resourceType = resource;
            resourceMarkets.Add(market);
        }
    }

    private IEnumerator UpdatePrices()
    {
        while(true)
        {
            foreach (ResourceMarket market in resourceMarkets)
            {
                DoMarketCycle(market);
                yield return null;
            }
            yield return wait;
            allPricesUpdated?.Invoke();
        }
    }

    private void DoMarketCycle(ResourceMarket market)
    {
        float change = market.currentPrice * HexTileManager.GetNextFloat(0f, market.dailyVoliatility / dayLength);
        market.direction = HexTileManager.GetNextFloat(0f, 1f) < market.chanceToFlip ? -1 * market.direction : market.direction;
        market.currentPrice += change * market.direction;
        market.DisplayPrice = market.currentPrice * (market.demand - market.supply) / market.demand;
        market.AddNewPrice(market.DisplayPrice);

        market.supply = Mathf.Lerp(market.supply, 0, market.consumptionRate);

        float minPrice = market.basePrice * 0.5f;
        float maxPrice = market.basePrice * 1.5f;

        if(DayNightManager.DayNumber < 5)
        {
            minPrice = market.basePrice * 0.85f;
            maxPrice = market.basePrice * 1.25f;
        }

        if (market.currentPrice < minPrice)
        {
            market.currentPrice = minPrice;
            market.direction = 1;
        }
        else if (market.currentPrice > maxPrice)
        {
            market.currentPrice = maxPrice;
            market.direction = -1;
        }

        resourcePriceUpdated?.Invoke(market.resourceType, market);
    }

    [Button]
    public void SellResource(ResourceAmount resource) => SellResource(resource.type, resource.amount);
    public void SellResource(ResourceType resource, int amount)
    {
        foreach (ResourceMarket market in resourceMarkets)
        {
            if (market.resourceType == resource)
            {
                int credits = Mathf.RoundToInt(GetResourcePrice(resource) * amount);
                resourceSold?.Invoke(credits);

                market.supply += amount;
                if(market.supply > market.demand)
                    market.supply = market.demand;
                DoMarketCycle(market);
                allPricesUpdated?.Invoke();
                return;
            }
        }
    }

    internal float GetResourcePrice(ResourceType resource)
    {
        foreach (ResourceMarket market in resourceMarkets)
        {
            if (market.resourceType == resource)
                return market.DisplayPrice;
        }

        return 0;
    }

    public float GetResourceBasePrice(ResourceType resource)
    {
        foreach (ResourceMarket market in resourceMarkets)
        {
            if (market.resourceType == resource)
                return market.basePrice;
        }

        return 0;
    }

    internal float[] GetPriceHistory(ResourceType resource)
    {
        foreach (ResourceMarket market in resourceMarkets)
        {
            if (market.resourceType == resource)
                return market.priceHistory;
        }

        return null;
    }

    public ResourceMarket GetMarket(ResourceType resource)
    {
        foreach (ResourceMarket market in resourceMarkets)
        {
            if (market.resourceType == resource)
            {
                return market;
            }
        }

        return null;
    }

    private void MarketEvent(int obj)
    {
        List<ResourceMarket> marketsForEvents = GetRandomMarket(HexTileManager.GetNextInt(1, 3));

        for (int i = 0; i < marketsForEvents.Count; i++)
        {

            bool priceIncrease = HexTileManager.GetNextFloat(0f, 1f) > 0.5f;
            bool permanentEvent = HexTileManager.GetNextFloat(0f, 1f) < 0.25f;
            ResourceMarket market = marketsForEvents[i];

            if (priceIncrease)
            {
                if (permanentEvent)
                {
                    float priceChange = market.basePrice * HexTileManager.GetNextFloat(0.1f, 0.25f);
                    market.basePrice += priceChange;
                    //MessagePanel.ShowMessage($"Prices for {market.resourceType.ToNiceString()} increase by {Round(priceChange)} due to a supply shortage", null);
                }
                else
                {
                    int supplyChange = (int)(market.demand * HexTileManager.GetNextFloat(0.1f, 0.25f));
                    market.supply -= supplyChange;
                    //MessagePanel.ShowMessage($"Prices for {market.resourceType.ToNiceString()} temporarily increase due to a insufficient supply.", null);
                }
            }
            else
            {
                if (permanentEvent)
                {
                    float priceChange = market.basePrice * HexTileManager.GetNextFloat(0.1f, 0.25f);
                    market.basePrice -= priceChange;
                    //MessagePanel.ShowMessage($"Prices for {market.resourceType.ToNiceString()} decrease by {Round(priceChange)} due to a demand shortage", null);
                }
                else
                {
                    int supplyChange = (int)(market.demand * HexTileManager.GetNextFloat(0.1f, 0.25f));
                    market.supply += supplyChange;
                    //MessagePanel.ShowMessage($"Prices for {market.resourceType.ToNiceString()} temporarily decrease due to excess supply.", null);
                }
            }
        }
            allPricesUpdated?.Invoke();
    }

    private List<ResourceMarket> GetRandomMarket(int count)
    { 
        return resourceMarkets.OrderBy(x => Guid.NewGuid())
                              .Take(count)
                              .ToList();
    }

    private float Round(float value)
    {
        if (value < 10)
            return (float)Math.Round(value, 2);
        else if (value < 100)
            return (float)Math.Round(value, 1);
        else
            return Mathf.RoundToInt(value);
    }

    private void LoadShipped(SupplyShipBehavior behavior, RequestType type1, List<ResourceAmount> type2)
    {
    }

    /// <summary>
    /// Adjusts the display price of a resource until the start of the given day.
    /// </summary>
    /// <param name="resource"></param>
    /// <param name="percentChange"></param>
    /// <param name="dayNumber"></param>
    private void PriceChangeUntilDayNumber(ResourceType resource, float percentChange, int dayNumber)
    {
        foreach (ResourceMarket market in resourceMarkets)
        {
            if (market.resourceType == resource)
            {
                MarketModifier modifier = new MarketModifier();
                modifier.market = market;
                modifier.startDay = DayNightManager.DayNumber;
                modifier.endDay = dayNumber + DayNightManager.DayNumber;
                modifier.percentChange = percentChange;
                market.AddPriceModifier(modifier);
                break;
            }
        }
    }

    private const string MARKET_PATH = "StockMarket";
    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this);
    }

    public void Save(string savePath, ES3Writer writer)
    {
        writer.Write<List<ResourceMarket>>(MARKET_PATH, resourceMarkets);
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if(ES3.KeyExists(MARKET_PATH, loadPath))
        {
            postUpdateMessage?.Invoke("Building Stock Market");
            resourceMarkets = ES3.Load<List<ResourceMarket>>(MARKET_PATH, loadPath);
        }
        yield return null;
    }

    [System.Serializable]
    public class ResourceMarket
    {
        public ResourceType resourceType;
        public float basePrice = 10f;
        public float dailyVoliatility = 0.05f;
        public float currentPrice = 10f;
        private float displayPrice;
        public float DisplayPrice
        {
            set
            {
                displayPrice = value;
            }
            get
            {
                return displayPrice * GetPriceModifiers();
            }
        }

        private List<MarketModifier> priceModifiers = new List<MarketModifier>();
        public int direction = 0;
        [Range(0.05f, 0.5f)]
        public float chanceToFlip = 0.1f;

        [Header("Supply and Demand")]
        public float demand = 1000;
        public float supply = 0;
        [Range(0.1f, 0.5f)]
        public float consumptionRate = 0.1f;

        public float[] priceHistory = new float[20];

        public void AddNewPrice(float price)
        {
            for(int i = 0; i < priceHistory.Length; i++)
            {
                if(i == priceHistory.Length - 1)
                    priceHistory[i] = price;
                else
                    priceHistory[i] = priceHistory[i + 1];
            }
        }

        private float GetPriceModifiers()
        {
            float priceAdditions = 1;
            for (int i = priceModifiers.Count - 1; i >= 0; i--)
            {
                if(!priceModifiers[i].IsValid)
                {
                    priceModifiers.RemoveAt(i);
                    continue;
                }
                else
                    priceAdditions += priceModifiers[i].PercentChange;
            }
            return priceAdditions;
        }

        public void AddPriceModifier(MarketModifier modifier)
        {
            priceModifiers.Add(modifier);
            AddNewPrice(DisplayPrice);

            MessageData message = new MessageData();
            message.message = $"Prices for {resourceType.ToNiceString(true)} {(modifier.percentChange > 0 ? "increased" : "decreased")} from {Mathf.RoundToInt(displayPrice)} to {Mathf.RoundToInt(DisplayPrice)} for {modifier.endDay - modifier.startDay} {(modifier.endDay - modifier.startDay > 1 ? "days" : "day")}.";
            message.messageColor = ColorManager.GetColor(ColorCode.techCredit);
            MessagePanel.ShowMessage(message);
        }

        public void RemovePriceModifier(MarketModifier modifier)
        {
            priceModifiers.Remove(modifier);
            AddNewPrice(DisplayPrice);
        }
    }

    public class MarketModifier
    {
        public ResourceMarket market;
        public float percentChange;
        public float PercentChange => DayNightManager.DayNumber <= endDay ? percentChange : 0;
        public int startDay;
        public int endDay;
        public bool IsValid => DayNightManager.DayNumber <= endDay;
    }
}
