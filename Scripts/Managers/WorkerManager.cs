using HexGame.Resources;
using HexGame.Units;
using Nova;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using UnityEngine;

[BurstCompile]
public class WorkerManager : MonoBehaviour
{
    [SerializeField] private int workersToAdd = 20;
    public static event Action<int> workersAdded;
    public static event Action<int> workersRemoved;
    public static event Action<ResourceType> workersNeedResource;
    public static event Action workerStateChanged;
    public static event Action<ResourceAmount> workerConsumedResource;
    public static float foodperworker = 1.5f;
    public static readonly float baseFoodPerWorker = 1.5f;
    private static List<UnitStorageBehavior> housingStorage = new List<UnitStorageBehavior>();
    private static List<HousingBehavior> housing = new List<HousingBehavior>();
    private static List<HappinessBuilding> happyBuildings = new List<HappinessBuilding>();
    
    private static int daysWithoutFood = 0;
    private static int daysWithoutWater = 0;
    private bool noFoodTracker = false;
    private bool noWaterTracker = false;
    public static float globalWorkerEfficiency = 1f;
    public static int happiness;
    public static int wagePerWorker = 5;
    public static event Action<int, int> wagesPaid;
    private static bool paidWagesToday = false;
    private static int wageDeficit = 0;

    [SerializeField] private TipCommunication workersNoFoodOrWater;

    [ShowInInspector]
    public static int workersNeeded => GetWorkersNeeded();

    [ShowInInspector]
    public static int availableWorkers { get => GetAvailableWorkers(); }
    [ShowInInspector]
    public static int totalWorkers { get; private set; }
    [ShowInInspector]
    public static int housingCapacity { get => housingStorage.Sum(x => x.GetIntStat(Stat.housing)); }

    [SerializeField] private Button workerMenuButton;


    private void Awake()
    {
        happyBuildings.Clear();
    }

    private void OnEnable()
    {
        HousingBehavior.housingAdded += HousingAdded;
        HousingBehavior.housingRemoved += HousingRemoved;
        HousingBehavior.housingAdded += FirstHousingPlaced;
        ResourceStart.workersAdded += WorkersAdded;
        DayNightManager.toggleDay += ResetSupplyTracker;
        DayNightManager.toggleDay += PayWorkers;
        DayNightManager.toggleDay += (d) => workerStateChanged?.Invoke();
        WorkerManager.workerStateChanged += UpdateGlobalEfficiency;
        UnlockWorkerMenuButton.WorkerButtonUnlocked += UnlockWorkerButton;
        Stats.UpgradeApplied += UpgradeApplied;
        StartCoroutine(ConsumeFood());
    }



    private void OnDisable()
    {
        StopAllCoroutines();
        HousingBehavior.housingAdded -= HousingAdded;
        HousingBehavior.housingRemoved -= HousingRemoved;
        HousingBehavior.housingAdded -= FirstHousingPlaced;
        ResourceStart.workersAdded -= WorkersAdded;
        DayNightManager.toggleDay -= ResetSupplyTracker;
        DayNightManager.toggleDay -= PayWorkers;
        DayNightManager.toggleDay -= (d) => workerStateChanged?.Invoke();
        WorkerManager.workerStateChanged -= UpdateGlobalEfficiency;
        UnlockWorkerMenuButton.WorkerButtonUnlocked -= UnlockWorkerButton;
    }


    private void PayWorkers(int DayNumber)
    {
        int totalWages = totalWorkers * wagePerWorker;
        if(HexTechTree.TechCredits >= totalWages)
        {
            paidWagesToday = true;
            wageDeficit = 0;
            HexTechTree.ChangeTechCredits(-totalWages);
            wagesPaid?.Invoke(totalWages, totalWages);
        }
        else
        {
            paidWagesToday = false;
            wageDeficit = totalWages - HexTechTree.TechCredits;
            MessagePanel.ShowMessage("Not enough credits to pay workers.", null);
            HexTechTree.ChangeTechCredits(-HexTechTree.TechCredits);
            wagesPaid?.Invoke(HexTechTree.TechCredits, totalWages);
        }
    }

    private void ResetSupplyTracker(int obj)
    {
        if(noFoodTracker)
        {
            daysWithoutFood++;
            noFoodTracker = false; //new day so reset

            if(daysWithoutFood == 2)
                GameTipsWindow.AddTip(workersNoFoodOrWater);
        }

        if(noWaterTracker)
        {
            daysWithoutWater++;
            noWaterTracker = false; //new day so reset

            if (daysWithoutFood == 2)
                GameTipsWindow.AddTip(workersNoFoodOrWater);
        }

        if(!noFoodTracker && !noWaterTracker)
        {
            daysWithoutFood = 0;
            daysWithoutWater = 0;
            globalWorkerEfficiency = 1f;
        }
        CalculateGlobalWorkerEfficiency();
    }

    private void UpdateGlobalEfficiency()
    {
        CalculateGlobalWorkerEfficiency();
    }

    public static float CalculateGlobalWorkerEfficiency()
    {
        CalculateHappiness(out happiness);

        int happinessPoints = Mathf.CeilToInt( happiness  / 10 ) - 10;
        globalWorkerEfficiency = Mathf.Max(0.25f, 1f + (happinessPoints * 0.05f));

        return globalWorkerEfficiency;
    }

    private void HousingAdded(HousingBehavior house)
    {
        housingStorage.Add(house.GetComponent<UnitStorageBehavior>());
        housing.Add(house);
        workerStateChanged?.Invoke();
    }

    private void HousingRemoved(HousingBehavior house)
    {
        housingStorage.Remove(house.GetComponent<UnitStorageBehavior>());
        housing.Remove(house);
        workerStateChanged?.Invoke();
    }

    private void FirstHousingPlaced(HousingBehavior hq)
    {
        HousingBehavior.housingAdded -= FirstHousingPlaced;
        workerStateChanged?.Invoke();
    }

    private IEnumerator ConsumeFood()
    {
        float foodToComsume;
        float totalDayLength;
        float interval;

        while (true)
        {
            if(totalWorkers <= 0)
            {
                yield return new WaitForSeconds(10f);
                continue;
            }

            //only eat during the day or transitioning
            yield return new WaitUntil(() => !DayNightManager.isNight);

            foodToComsume = totalWorkers * foodperworker;
            totalDayLength = DayNightManager.GetTotalCycleLength();
            interval = totalDayLength / foodToComsume;
            if (interval == float.NaN || interval == 0 || float.IsInfinity(interval))
            {
                yield return null;
                continue;
            }

            if (!PlayerResources.TryUseResource(new ResourceAmount(ResourceType.Food, 1)))
            {
                if(!noFoodTracker)
                    MessagePanel.ShowMessage("Workers don't have food.", null);
                workersNeedResource?.Invoke(ResourceType.Food);
                noFoodTracker = true;
            }
            else
            {
                workerConsumedResource?.Invoke(new ResourceAmount(ResourceType.Food, 1));
            }

            if (!PlayerResources.TryUseResource(new ResourceAmount(ResourceType.Water, 1)))
            {
                if (!noWaterTracker)
                    MessagePanel.ShowMessage("Workers don't have water.", null);
                workersNeedResource?.Invoke(ResourceType.Water);
                noWaterTracker = true;
            }
            else
            {
                workerConsumedResource?.Invoke(new ResourceAmount(ResourceType.Water, 1));
            }

            yield return new WaitForSeconds(interval);
        }
    }

    public void RegisterColonistUI(ResourceUI resourceUI, ResourceType resourceType)
    {
        if (resourceType != ResourceType.Workers)
            return;
    }

    public static bool HireWorker(int number)
    {
        //if housing capacity is zero - we are just starting the game
        if (totalWorkers + number > housingCapacity && number > 0)
        {
            MessagePanel.ShowMessage("Not enough housing for workers.", null);
            return false;
        }

        if(totalWorkers + number < 0)
        {
            MessagePanel.ShowMessage("No workers to fire.", null);
            return false;
        }

        WorkersAdded(number);
        return true;
    }

    public static void WorkersAdded(int number)
    {
        totalWorkers += number;
        DeliverWorkers(number);
    }
    public static void WorkersLost(int number)
    {
        Debug.Log($"***Workers lost: {number}***");
    }

    private IEnumerator LostColonist(int amount)
    {
        int attempts = 0;
        while (amount > 0 && attempts < amount * 3)
        {
            PlayerUnit unit = UnitManager.playerUnits[HexTileManager.GetNextInt(0, UnitManager.playerUnits.Count)];
            if (unit.GetStat(Stat.workers) > 0)
            {
                unit.GetComponent<UnitStorageBehavior>().DestroyResource(new ResourceAmount(ResourceType.Workers, 1));
                WorkerManager.WorkersLost(1);
                amount--;
                attempts++;
                MessagePanel.ShowMessage($"Workers lost at {unit.gameObject.name}.", unit.gameObject);

                //resourceChange?.Invoke(ResourceType.Colonists, 1, GetStorageLimit(ResourceType.Colonists));
            }
            yield return null;
        }
    }
    private static int GetAvailableWorkers()
    {
        return totalWorkers - UnitManager.playerStorage.Sum(x => x.GetWorkerTotal()) - GlobalStorageBehavior.WorkersMoving; ;
    }

    private static int GetWorkersNeeded()
    {
        int totalNeeded = 0;
        foreach (var unit in UnitManager.playerStorage)
        {
            totalNeeded += unit.GetWorkersNeed();
        }
        
        //totalNeeded -= PlayerResources.GetAmountInTransit(ResourceType.Workers);
        totalNeeded -= GlobalStorageBehavior.WorkersMoving;

        return Mathf.Max(totalNeeded - availableWorkers, 0);
    }

    public static int TakeWorkers(int amount)
    {
        if (availableWorkers <= 0)
            return 0;

        workerStateChanged?.Invoke();

        if (availableWorkers - amount < 0)
            return availableWorkers - amount;
        else
            return amount;
    }

    public static void DeliverWorkers(int amount)
    {
        workerStateChanged?.Invoke();
    }

    public static List<HappinessFactor> CalculateHappiness(out int happiness)
    {
        List<HappinessFactor> factors = new List<HappinessFactor>();
        happiness = 120; //starting happiness
        happiness -= totalWorkers;
        factors.Add(new HappinessFactor(-totalWorkers, "Total Population"));

        if (daysWithoutFood > 0)
        {
            factors.Add(new HappinessFactor(-daysWithoutFood * totalWorkers, $"No food for {daysWithoutFood} days"));
            happiness -= Mathf.RoundToInt(daysWithoutFood * totalWorkers * 0.25f);
        }
        
        if (daysWithoutWater > 0)
        {
            factors.Add(new HappinessFactor(-daysWithoutWater * totalWorkers, $"No water for {daysWithoutWater} days"));
            happiness -= Mathf.RoundToInt(daysWithoutWater * totalWorkers * 0.25f);
        }

        if(!paidWagesToday && wageDeficit > 0)
        {
            happiness -= wageDeficit;
            factors.Add(new HappinessFactor(-wageDeficit, $"Wages not fully paid"));
        }

        if(wagePerWorker > 5)
        {
            happiness += Mathf.RoundToInt(0.25f * (wagePerWorker - 5) * totalWorkers);
            factors.Add(new HappinessFactor(Mathf.RoundToInt(0.25f * (wagePerWorker - 5) * totalWorkers), "Higher Wages"));
        }
        else if (wagePerWorker < 5)
        {
            happiness -= Mathf.RoundToInt(0.25f * (5 - wagePerWorker) * totalWorkers);
            factors.Add(new HappinessFactor(Mathf.RoundToInt(0.25f * (wagePerWorker - 5) * totalWorkers), "Lower Wages"));
        }

        if(foodperworker > 1.1f * baseFoodPerWorker && daysWithoutFood == 0)
        {
            happiness += Mathf.RoundToInt(0.35f * totalWorkers);
            factors.Add(new HappinessFactor(Mathf.RoundToInt(0.25f * totalWorkers), "Double Rations"));
        }
        else if(foodperworker < 0.9f * baseFoodPerWorker && daysWithoutFood == 0)
        {
            happiness -= Mathf.RoundToInt(0.35f * totalWorkers);
            factors.Add(new HappinessFactor(-Mathf.RoundToInt(0.25f * totalWorkers), "Half Rations"));
        }

        if (housingCapacity > 0)
        {
            float housingRoom = totalWorkers / housingCapacity;
            if (housingRoom > 0.95f)
            {
                happiness -= Mathf.RoundToInt(0.2f * totalWorkers);
                factors.Add(new HappinessFactor(-Mathf.RoundToInt(0.2f * totalWorkers), "Crowded Housing"));
            }
            else if (housingRoom < 0.5)
            {
                happiness += Mathf.RoundToInt(0.4f * totalWorkers);
                factors.Add(new HappinessFactor(Mathf.RoundToInt(0.2f * totalWorkers), "Spacious Housing"));
            }
            else if (housingRoom < 0.75)
            {
                happiness += Mathf.RoundToInt(0.2f * totalWorkers);
                factors.Add(new HappinessFactor(Mathf.RoundToInt(0.2f * totalWorkers), "Comfortable Housing"));
            }
        }

        foreach (var happyBuilding in happyBuildings)
        {
            int buildingHappiness = (int)happyBuilding.building.GetHappiness();
            if(buildingHappiness == 0)
                continue;
            happiness += (int)happyBuilding.building.GetHappiness();

            //don't repeat factors
            string description = happyBuilding.unitType.ToNiceString() + happyBuilding.building.GetHappinessString();
            bool found = false;
            for(int i = 0; i < factors.Count; i++)
            {
                if(factors[i].description == description)
                {
                    factors[i] = new HappinessFactor(factors[i].value + buildingHappiness, description);
                    found = true;
                    break;
                }
            }

            if(!found)
                factors.Add(new HappinessFactor((int)happyBuilding.building.GetHappiness(), description));
        }

        happiness += Mathf.FloorToInt(ReputationManager.Reputation / 250);
        factors.Add(new HappinessFactor(Mathf.FloorToInt(ReputationManager.Reputation / 250), "Corporate Reputation"));

        return factors;
    }

    private void UnlockWorkerButton()
    {
        Interactable interactable = workerMenuButton.GetComponent<Interactable>();
        if (interactable.ClickBehavior == ClickBehavior.OnRelease)
            return; //we're already on

        interactable.ClickBehavior = ClickBehavior.OnRelease;
        workerMenuButton.GetComponent<Animator>().SetTrigger("Highlight");
    }

    public static void AddHappyBuilding(IHaveHappiness building, PlayerUnit playerUnit)
    {
        happyBuildings.Add(new HappinessBuilding(building, playerUnit));
        workerStateChanged?.Invoke();
    }

    public static void RemoveHappyBuilding(IHaveHappiness building)
    {
        foreach (var happyBuilding in happyBuildings)
        {
            if (happyBuilding.building == building)
            {
                happyBuildings.Remove(happyBuilding);
                workerStateChanged?.Invoke();
                return;
            }
        }
    }
    private void UpgradeApplied(Stats stats, StatsUpgrade upgrade)
    {
        if (stats.stats.ContainsKey(Stat.housing))
            workerStateChanged?.Invoke();
    }
}

public struct HappinessFactor
{
    public HappinessFactor(int value, string description)
    {
        this.value = value;
        this.description = description;
    }
    public int value;
    public string description;
}

public struct HappinessBuilding
{
    public HappinessBuilding(IHaveHappiness building, PlayerUnit playerUnit)
    {
        this.building = building;
        this.unitType = playerUnit.unitType;
    }

    public IHaveHappiness building;
    public PlayerUnitType unitType;
}
