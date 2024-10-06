using HexGame.Resources;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StockMarket : MonoBehaviour
{
    [SerializeField, ListDrawerSettings(NumberOfItemsPerPage = 1)]
    private List<ResourceMarket> resourceMarkets;

    private DayNightManager dayNightManager;
    private float dayLength;
    private WaitForSeconds wait = new WaitForSeconds(20f);
    public static event Action allPricesUpdated;
    public static event Action<ResourceType, ResourceMarket> resourcePriceUpdated;
    [SerializeField] private Transform barParent;
    private PlayerResources playerResources;
    [SerializeField, Range(0f, 1f)]
    private float chanceForEvent = 0.5f;

    private void Awake()
    {
        dayNightManager = FindObjectOfType<DayNightManager>();
        dayLength = dayNightManager.DayLength / 60f;
        playerResources = FindObjectOfType<PlayerResources>();

        CreateResources();

        int barCount = 45;
        if(barParent == null)
            barParent = GameObject.Find("Bar Parent")?.transform;

        if(barParent)
            barCount = barParent.childCount;

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
    }

    private void OnDisable()
    {
        DayNightManager.toggleDay -= MarketEvent;
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
        market.displayPrice = market.currentPrice * (market.demand - market.supply) / market.demand;
        market.AddNewPrice(market.displayPrice);

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
    public void SellResource(ResourceType resource, int amount)
    {
        foreach (ResourceMarket market in resourceMarkets)
        {
            if (market.resourceType == resource)
            {
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
                return market.displayPrice;
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



    [System.Serializable]
    public class ResourceMarket
    {
        public ResourceType resourceType;
        public float basePrice = 10f;
        public float dailyVoliatility = 0.05f;
        public float currentPrice = 10f;
        public float displayPrice;
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
    }
}
