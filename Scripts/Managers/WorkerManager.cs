using HexGame.Resources;
using HexGame.Units;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

[BurstCompile]
public class WorkerManager : MonoBehaviour, ISaveData
{
    public static event Action<int> workersAdded;
    public static event Action<int> workersRemoved;
    public static event Action<ResourceType> workersNeedResource;
    public static event Action workerStateChanged;
    public static event Action<ResourceAmount> workerConsumedResource;
    private static List<UnitStorageBehavior> housingStorage = new List<UnitStorageBehavior>();
    private static List<HousingBehavior> housing = new List<HousingBehavior>();
    private static List<HappinessBuilding> happyBuildings = new List<HappinessBuilding>();
    
    private static int daysWithoutFood = 0;
    private static int daysWithoutWater = 0;
    private bool noFoodTracker = false;
    private bool noWaterTracker = false;
    public static float globalWorkerEfficiency = 1f;
    public static event Action<float> EfficiencyChanged;
    public static int happiness;
    public static event Action<int, int> wagesPaid;
    private static bool paidWagesToday = false;
    private static int wageDeficit = 0;

    public static int wages = 12;
    public static readonly int maxWages = 24;
    public static readonly int baseWages = 12;

    private static float doubleWageBonus = 0.35f;
    private static float halfWagePenalty = 0.66f;
    private static float zeroWagePenalty = 1f;

    public static float rations = 1.5f;
    public static readonly float maxRations = 3f;
    public static readonly float baseRations = 1.5f;

    private static float doubleRationsBouns = 0.35f;
    private static float halfRationsPenalty = 0.66f;
    private static float zeroRationsPenalty = 1f;

    private static float penaltyPerWorker = 0.02f;
    private static List<WorkerRequest> workerRequests = new List<WorkerRequest>();

    [SerializeField] private TipCommunication workersNoFoodOrWater;

    [ShowInInspector]
    public static int workersNeeded => GetWorkersNeeded();

    [ShowInInspector]
    public static int AvailableWorkers { get => availableWorkers; }
    private static int availableWorkers = 0;
    [ShowInInspector]
    public static int TotalWorkers => totalWorkers;
    private static int totalWorkers = 0;
    [ShowInInspector]
    public static int housingCapacity => GetHousingCapacity();

    private void Awake()
    {
        daysWithoutFood = 0;
        daysWithoutWater = 0;
        globalWorkerEfficiency = 1f;
        paidWagesToday = true;
        wageDeficit = 0;
        totalWorkers = 0;

        RegisterDataSaving();
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
        Stats.UpgradeApplied += UpgradeApplied;

        SaveLoadManager.LoadComplete += workerStateChanged;
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
        SaveLoadManager.LoadComplete -= workerStateChanged;

        happyBuildings.Clear();
        housingStorage.Clear();
        housing.Clear();
    }

    private void Update()
    {
        if (workerRequests.Count > 0)
            TryProcessWorkerRequest();
    }
    private void PayWorkers(int DayNumber)
    {
        int totalWages = TotalWorkers * wages;
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
        if (TotalWorkers <= 0)
            return 0f;

        CalculateHappiness(out happiness);
        globalWorkerEfficiency = 1f + (float)happiness / (float)(TotalWorkers * 2);
        globalWorkerEfficiency = Mathf.Max(0.25f, globalWorkerEfficiency);
        EfficiencyChanged?.Invoke(globalWorkerEfficiency);
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
            if(TotalWorkers <= 0)
            {
                yield return new WaitForSeconds(10f);
                continue;
            }

            //only eat during the day or transitioning
            yield return new WaitUntil(() => !DayNightManager.isNight);

            foodToComsume = TotalWorkers * rations;
            totalDayLength = DayNightManager.GetTotalCycleLength();
            interval = totalDayLength / foodToComsume;
            if (interval == float.NaN || interval == 0 || float.IsInfinity(interval))
            {
                yield return null;
                continue;
            }

            if (!TryUseResource(new ResourceAmount(ResourceType.Food, 1)))
            {
                if(!noFoodTracker)
                    MessagePanel.ShowMessage("Some workers don't have food.", null);
                workersNeedResource?.Invoke(ResourceType.Food);
                noFoodTracker = true;
            }
            else
            {
                workerConsumedResource?.Invoke(new ResourceAmount(ResourceType.Food, 1));
            }

            if (!TryUseResource(new ResourceAmount(ResourceType.Water, 1)))
            {
                if (!noWaterTracker)
                    MessagePanel.ShowMessage("Some workers don't have water.", null);
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

    int foodIndex = 0;
    int waterIndex = 0;
    private bool TryUseResource(ResourceAmount resourceAmount)
    {
        if (housing.Count <= 0)
            return false;

        if (resourceAmount.type == ResourceType.Food)
        {
            foodIndex++;
            if (foodIndex >= housing.Count)
                foodIndex = 0;
            return housing[foodIndex].TryConsume(resourceAmount);
        }
        else if (resourceAmount.type == ResourceType.Water)
        {
            waterIndex++;
            if (waterIndex >= housing.Count)
                waterIndex = 0;
            return housing[waterIndex].TryConsume(resourceAmount);
        }
        else
        {
            Debug.LogError($"WorkerManager tried to consume {resourceAmount.type} but it is not supported.");
            return false;
        }
    }

    public static bool HireWorker(int number)
    {
        //if housing capacity is zero - we are just starting the game
        if (TotalWorkers + number > housingCapacity && number > 0)
        {
            MessagePanel.ShowMessage("Crowded housing.", null);
        }

        if(TotalWorkers + number < 0)
        {
            MessagePanel.ShowMessage("No workers to fire.", null);
            return false;
        }

        if(number > 0)
            WorkersAdded(number);
        else
            WorkersLost(-number);
        return true;
    }

    public static void WorkersAdded(int number)
    {
        if (number < 0)
        {
            Debug.LogError("Don't use this to remove workers.");
            return;
        }

        totalWorkers += number;
        availableWorkers += number;
        workersAdded?.Invoke(number);
        UpdateWorkerCounts();
    }


    public static void WorkersLost(int number)
    {
        totalWorkers -= number;
        if (number < 0)
            workersAdded?.Invoke(number);
        else if (number > 0)
            workersRemoved?.Invoke(number);
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
        int unitWorkers = 0;
        for (int i = 0; i < UnitManager.playerStorage.Count; i++)
        {
            unitWorkers += UnitManager.playerStorage[i].GetWorkerTotal();
        }
        return TotalWorkers - unitWorkers;
    }

    private static int GetWorkersNeeded()
    {
        int totalNeeded = 0;
        foreach (var unit in UnitManager.playerStorage)
        {
            if (unit == null)
                continue;
            totalNeeded += unit.GetWorkersNeed();
        }

        return Mathf.Max(totalNeeded - AvailableWorkers, 0);
    }

    public static int TakeWorkers(int amount)
    {
        if (AvailableWorkers <= 0)
            return 0;

        //workerStateChanged?.Invoke();

        if (AvailableWorkers - amount < 0)
            return AvailableWorkers - amount;
        else
            return amount;
    }

    public static void UpdateWorkerCounts()
    {
        workerStateChanged?.Invoke();
    }

    public static List<HappinessFactor> CalculateHappiness(out int happiness)
    {
        List<HappinessFactor> factors = new List<HappinessFactor>();
        happiness = 0; //starting happiness
        happiness -= HappinessPerWorker(TotalWorkers * 2);
        factors.Add(new HappinessFactor(-TotalWorkers * 2, "Total Population"));

        if (daysWithoutFood > 0)
        {
            factors.Add(new HappinessFactor(-daysWithoutFood * TotalWorkers, $"No food for {daysWithoutFood} days"));
            happiness -= Mathf.RoundToInt(daysWithoutFood * TotalWorkers * 0.25f);
        }
        
        if (daysWithoutWater > 0)
        {
            factors.Add(new HappinessFactor(-daysWithoutWater * TotalWorkers, $"No water for {daysWithoutWater} days"));
            happiness -= Mathf.RoundToInt(daysWithoutWater * TotalWorkers * 0.25f);
        }

        //if(daysWithoutFood == 0 && daysWithoutWater == 0)
        //{
        //    happiness += Mathf.RoundToInt(TotalWorkers);
        //    factors.Add(new HappinessFactor(Mathf.RoundToInt(TotalWorkers), "Food and Water Provided"));
        //}

        //if (!paidWagesToday && wageDeficit > 0)
        //{
        //    happiness -= wageDeficit;
        //    factors.Add(new HappinessFactor(-wageDeficit, $"Wages not fully paid"));
        //}
        //else
        //{
        //    happiness += TotalWorkers;
        //    factors.Add(new HappinessFactor(Mathf.RoundToInt(TotalWorkers), $"Wages fully paid"));
        //}

        if(wages < 2f)
        {
            float value = ((float)baseWages - (float)wages) / (float)baseWages;
            int happyDelta = Mathf.RoundToInt(TotalWorkers - zeroWagePenalty * TotalWorkers * value);
            happiness += happyDelta;
            factors.Add(new HappinessFactor(happyDelta, "Extremely Low Wages"));
        }
        else if(wages >= baseWages)
        {
            float value = ((float)wages - (float)baseWages) / ((float)maxWages - (float)baseWages);
            int happyDelta = Mathf.RoundToInt(doubleWageBonus * TotalWorkers * value);
            happiness += happyDelta;
            factors.Add(new HappinessFactor(happyDelta, "Extra Wages"));

            happiness += TotalWorkers;
            factors.Add(new HappinessFactor(Mathf.RoundToInt(TotalWorkers), $"Wages fully paid"));
        }
        else if (wages < baseWages)
        {
            float value = ((float)baseWages - (float)wages) / (float)baseWages;
            int happyDelta = Mathf.RoundToInt(TotalWorkers - halfWagePenalty * TotalWorkers * value);
            happiness += happyDelta;
            factors.Add(new HappinessFactor(happyDelta, "Low Wages"));
        }

        if(rations < baseRations * 0.35f)
        {
            float value = (baseRations - rations) / baseRations;
            int happyDelta = Mathf.RoundToInt(TotalWorkers - zeroRationsPenalty * TotalWorkers * value);
            happiness += happyDelta;
            factors.Add(new HappinessFactor(happyDelta, "Extremely Low Rations"));
        }
        else if(rations >= baseRations && daysWithoutFood == 0)
        {
            float value = (rations - baseRations) / (2f * baseRations);
            int happyDelta = Mathf.RoundToInt(doubleRationsBouns * TotalWorkers * value);
            happiness += happyDelta;
            factors.Add(new HappinessFactor(happyDelta, "Extra Rations"));

            happiness += Mathf.RoundToInt(TotalWorkers);
            factors.Add(new HappinessFactor(Mathf.RoundToInt(TotalWorkers), "Food and Water Provided"));
        }
        else if(rations < baseRations && daysWithoutFood == 0)
        {
            float value = (baseRations - rations) / (baseRations);
            int happyDelta = Mathf.RoundToInt(TotalWorkers - halfRationsPenalty * TotalWorkers * value);
            happiness += happyDelta;
            factors.Add(new HappinessFactor(happyDelta, "Low Rations"));
        }

        if (housingCapacity > 0 || TotalWorkers > 20)
        {
            float housingRoom = (float)TotalWorkers / (float)housingCapacity;
            if(housingRoom > 1f)
            {
                happiness -= Mathf.RoundToInt(4 * (TotalWorkers - housingCapacity));
                factors.Add(new HappinessFactor(-Mathf.RoundToInt(4 * (TotalWorkers - housingCapacity)), "Unhoused Workers"));
            }
            
            if (housingRoom > 0.95f)
            {
                happiness -= Mathf.RoundToInt(0.1f * TotalWorkers);
                factors.Add(new HappinessFactor(-Mathf.RoundToInt(0.1f * TotalWorkers), "Crowded Housing"));
            }
            else if(housingRoom < 0.35)
            {
                int excessCapacity = housingCapacity - TotalWorkers;
                happiness -= Mathf.RoundToInt(0.05f * excessCapacity);
                factors.Add(new HappinessFactor(-Mathf.RoundToInt(0.05f * excessCapacity), "Empty Housing"));
            }
            //else if (housingRoom <= 0.5)
            //{
            //    happiness += Mathf.RoundToInt(0.1f * totalWorkers);
            //    factors.Add(new HappinessFactor(Mathf.RoundToInt(0.1f * totalWorkers), "Spacious Housing"));
            //}
            else if (housingRoom < 0.75)
            {
                happiness += Mathf.RoundToInt(0.05f * TotalWorkers);
                factors.Add(new HappinessFactor(Mathf.RoundToInt(0.05f * TotalWorkers), "Comfortable Housing"));
            }
        }
        else
        {
            happiness -= Mathf.RoundToInt(0.2f * totalWorkers);
            factors.Add(new HappinessFactor(-Mathf.RoundToInt(0.5f * totalWorkers), "No Housing"));
        }

        foreach (var happyBuilding in happyBuildings)
        {
            if(happyBuilding.building == null)
                continue;

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

    [Button]
    private static int HappinessPerWorker(float totalWorkers)
    {
       return  Mathf.FloorToInt(totalWorkers * (1 + Mathf.Round(Mathf.Floor(totalWorkers / 100)) * penaltyPerWorker));
    }

    public static void AddHappyBuilding(IHaveHappiness building, PlayerUnit playerUnit)
    {
        happyBuildings.Add(new HappinessBuilding(building, playerUnit));
        if(!SaveLoadManager.Loading)
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

    private static int GetHousingCapacity()
    {
        int capacity = 0;
        for (int i = housingStorage.Count - 1; i >= 0; i--)
        {
            if (housingStorage[i] == null)
            {
                housingStorage.RemoveAt(i);
                continue;
            }
            capacity += housingStorage[i].GetIntStat(Stat.housing);
        }
        return capacity;
    }

    private const string WORKER_SAVE_PATH = "workerData";
    public void RegisterDataSaving()
    {
        //needs to be called after builings are placed
        SaveLoadManager.RegisterData(this, 4f);
    }

    public void Save(string savePath, ES3Writer writer)
    {
        WorkerData workerData = new WorkerData
        {
            daysWithoutFood = WorkerManager.daysWithoutFood,
            daysWithoutWater = WorkerManager.daysWithoutWater,
            noFoodTracker = WorkerManager.daysWithoutFood > 0,
            noWaterTracker = WorkerManager.daysWithoutWater > 0,
            wagePerWorker = WorkerManager.wages,
            paidWagesToday = WorkerManager.paidWagesToday,
            wageDeficit = WorkerManager.wageDeficit,
            totalWorkers = WorkerManager.TotalWorkers,
            foodPerWorker = WorkerManager.rations,
        };

        writer.Write<WorkerData>(WORKER_SAVE_PATH, workerData);
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if (ES3.KeyExists(WORKER_SAVE_PATH, loadPath))
        {
            WorkerData workerData = ES3.Load<WorkerData>(WORKER_SAVE_PATH, loadPath);

            WorkerManager.daysWithoutFood = workerData.daysWithoutFood;
            WorkerManager.daysWithoutWater = workerData.daysWithoutWater;
            WorkerManager.wages = workerData.wagePerWorker;
            WorkerManager.paidWagesToday = workerData.paidWagesToday;
            WorkerManager.wageDeficit = workerData.wageDeficit;
            WorkerManager.totalWorkers = workerData.totalWorkers;
            noFoodTracker = workerData.daysWithoutFood > 0;
            noWaterTracker = workerData.daysWithoutWater > 0;
            WorkerManager.rations = workerData.foodPerWorker;
            WorkerManager.CalculateGlobalWorkerEfficiency();
        }
        workerStateChanged?.Invoke();
        yield return null;
    }

    internal static void RequestWorkers(int requestAmount, UnitStorageBehavior requestor)
    {
        WorkerRequest wr = new WorkerRequest()
        {
            amount = requestAmount,
            requestor = requestor
        };

        workerRequests.Add(wr);
    }

    private void TryProcessWorkerRequest()
    {
        if(workerRequests.Count == 0 || AvailableWorkers <= 0)
            return;

        var wr = workerRequests[0];
        if(wr.requestor == null)
        {             
            workerRequests.RemoveAt(0);
            return;
        }

        if(AvailableWorkers >= wr.amount)
        {
            availableWorkers -= wr.amount;
            wr.requestor.DeliverResource(new ResourceAmount(ResourceType.Workers, wr.amount));
            workerRequests.RemoveAt(0);
            workerStateChanged?.Invoke();
        }
        else if(AvailableWorkers > 0)
        {
            wr.requestor.DeliverResource(new ResourceAmount(ResourceType.Workers, AvailableWorkers));
            var newRequest = new WorkerRequest()
            {
                amount = wr.amount - AvailableWorkers,
                requestor = wr.requestor
            };
            workerRequests[0] = newRequest;
            availableWorkers = 0;
            workerStateChanged?.Invoke();
        }
    }

    public static void ReturnWorkers(int number)
    {
        availableWorkers += number;
        workerStateChanged?.Invoke();
    }

    public struct WorkerData
    {
        public int daysWithoutFood;
        public int daysWithoutWater;
        public bool noFoodTracker;
        public bool noWaterTracker;
        public int wagePerWorker;
        public bool paidWagesToday;
        public int wageDeficit;
        public int totalWorkers;
        public float foodPerWorker;
    }

    public struct WorkerRequest
    {
        public int amount;
        public UnitStorageBehavior requestor;
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
