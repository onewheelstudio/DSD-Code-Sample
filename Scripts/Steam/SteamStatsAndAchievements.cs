using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks.Data;
using Steamworks;
using HexGame.Resources;
using UnityEngine.Rendering;

public class SteamStatsAndAchievements : MonoBehaviour
{
    private const string NIGHT_COUNT = "Nights";
    //stats
    private const string TECH_CREDITS = "TechCredits";
    private const string SHIPMENTS = "Shipments";
    private const string TILES_PLACED = "TilesPlaced";

    //achievements
    private const string SHIPMENTS_100 = "100_Shipments";
    private const string SHIPMENTS_1000 = "1000_Shipments";
    [ShowInInspector]
    private int shipmentCount = 0;
    private const string TILES_PLACED_100 = "100_TilesPlaced";
    private const string TILES_PLACED_1000 = "1000_TilesPlaced";
    [ShowInInspector]
    private int tilesPlaced = 0;

    private const string WORKERS_100 = "Workers_100";
    private const string Workers_500 = "Workers_500";
    private const string Workers_1000 = "Workers_1000";
    private const string WORKERS = "Workers";

    [ShowInInspector]
    private int dailyIncome = 0;
    private bool statsUpdated = false;
    private bool waitingForStats = false;

    [SerializeField] private GameSettings gameSettings;

    private void Awake()
    {
        RequestStatUpdate();
    }

    private void OnEnable()
    {
        if (!SteamManager.IsConnected)
            return;

        Steamworks.SteamUserStats.OnUserStatsReceived += StatsReceived;

        DayNightManager.toggleDay += AddNewDay;
        DayNightManager.toggleDay += ResetDailyIncome;
        UnlockWorkerMenuButton.WorkerButtonUnlocked += TutorialComplete;
        HexTechTree.techCreditEarned += TechCreditEarned;

        if (gameSettings.IsDemo)
            return;

        HexTechTree.techCreditEarned += CheckDailyIncome;
        HexTechTree.TierUnlockComplete += TierUnlockComplete;

        SupplyShipBehavior.requestComplete += ShipmentComplete;
        PlaceHolderTileBehavior.tileComplete += TilesPlaced;

        WorkerManager.workersAdded += WorkersAdded;
    }

    private void OnDisable()
    {
        Steamworks.SteamUserStats.OnUserStatsReceived -= StatsReceived;

        DayNightManager.toggleDay -= AddNewDay;
        DayNightManager.toggleDay -= ResetDailyIncome;
        UnlockWorkerMenuButton.WorkerButtonUnlocked -= TutorialComplete;
        HexTechTree.techCreditEarned -= TechCreditEarned;

        if (gameSettings.IsDemo)
            return;

        HexTechTree.techCreditEarned -= CheckDailyIncome;
        HexTechTree.TierUnlockComplete -= TierUnlockComplete;

        SupplyShipBehavior.requestComplete -= ShipmentComplete;
        PlaceHolderTileBehavior.tileComplete -= TilesPlaced;

        WorkerManager.workersAdded -= WorkersAdded;

    }



    private void ResetDailyIncome(int obj)
    {
        dailyIncome = 0;
    }

    private void CheckDailyIncome(int amountEarned)
    {
        dailyIncome += amountEarned;

        if (dailyIncome > 1_000_000)
        {
            Achievement achievement = new Achievement("SingleDay_1_000_000");
            TriggerAchievement(achievement);
        }
        else if(dailyIncome > 100_000)
        {
            Achievement achievement = new Achievement("SingleDay_100_000");
            TriggerAchievement(achievement);
        }
    }

    private void AddNewDay(int dayNumber)
    {
        if (!SteamManager.IsConnected)
            return;

        Steamworks.SteamUserStats.SetStat(NIGHT_COUNT, dayNumber);
        if(dayNumber == 1)
        { 
            Achievement achievement = new Achievement("FIRST_NIGHT");
            TriggerAchievement(achievement);
        }
        else if(dayNumber >= 25)
        {
            Achievement achievement = new Achievement("25_NIGHTS");
            TriggerAchievement(achievement);
        }
    }

    private void TutorialComplete()
    {
        Achievement achievement = new Achievement("TUTORIAL_COMPLETE");
        TriggerAchievement(achievement);
    }

    private void TechCreditEarned(int amountEarned)
    {
        if (amountEarned <= 0)
            return;

        Steamworks.SteamUserStats.AddStat(TECH_CREDITS, amountEarned);
        int amountCollected = HexTechTree.TotalTechCreditsCollected;

        //if adding more achievements, make sure to add a check for the achievement to see if it has already been earned
        if (amountCollected >= 100_000 && gameSettings.IsDemo)
        {
            Achievement achievement = new Achievement("100K_TECH_CREDIT");
            TriggerAchievement(achievement);
        }

        if (gameSettings.IsDemo)
            return;

        if (amountCollected >= 100_000)
        {
            Achievement achievement = new Achievement("Earn_100_000");
            TriggerAchievement(achievement);
        }
        else if (amountCollected > 1_000_000)
        {

            Achievement achievement = new Achievement("Earn_1_000_000");
            TriggerAchievement(achievement);
        }
        else if(amountCollected > 10_000_000)
        {
            Achievement achievement = new Achievement("Earn_10_000_000");
            TriggerAchievement(achievement);
        }
        else if(amountCollected > 100_000_000)
        {
            Achievement achievement = new Achievement("Earn_100_000_000");
            TriggerAchievement(achievement);
        }
        
        //this is a lifetime stat
        if(SteamUserStats.GetStatInt(TECH_CREDITS) > 1_000_000_000)
        {
            Achievement achievement = new Achievement("Earn_1_000_000_000");
            TriggerAchievement(achievement);
        }
    }

    private void WorkersAdded(int workersAdded)
    {
        if (gameSettings.IsDemo)
            return;

        int totalWorkers = WorkerManager.TotalWorkers;
        SteamUserStats.SetStat(SHIPMENTS, totalWorkers);
        
        if (totalWorkers % 20 == 0 && totalWorkers > 20)
            ShowProgress(WORKERS_100, totalWorkers, 100);
        else if (totalWorkers % 100 == 0)
            ShowProgress(Workers_500, totalWorkers, 500);
        else if(totalWorkers % 250 == 0)
            ShowProgress(Workers_500, totalWorkers, 500);


        if(totalWorkers >= 100 && !IsAchievementUnlocked(WORKERS_100))
        {
            Achievement achievement = new Achievement(WORKERS_100);
            TriggerAchievement(achievement);
        }
        else if(totalWorkers >= 500 && !IsAchievementUnlocked(Workers_500))
        {
            Achievement achievement = new Achievement(Workers_500);
            TriggerAchievement(achievement);
        }
        else if(totalWorkers >= 1000 && !IsAchievementUnlocked(Workers_1000))
        {
            Achievement achievement = new Achievement(Workers_1000);
            TriggerAchievement(achievement);
        }
    }

    private void TierUnlockComplete(int tier)
    {
        string achievementName = $"TechTier_{tier}";
        Achievement achievement = new Achievement(achievementName);
        TriggerAchievement(achievement);
    }

    public static void TriggerAchievement(Achievement ach)
    {
        if (!SteamManager.IsConnected)
            return;

        ach.Trigger();
    }

    [Button]
    public void ResetAchievements()
    {
        Steamworks.SteamUserStats.ResetAll(true);
    }

    private void RequestStatUpdate()
    {
        try
        {
            statsUpdated = false;
            waitingForStats = true;
            Steamworks.SteamUserStats.RequestCurrentStats();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            Debug.LogError("Unable to update Stats.");
        }
    }

    private void StatsReceived(SteamId id, Result result)
    {
        statsUpdated = result == Result.OK;
        waitingForStats = false;
        shipmentCount = Steamworks.SteamUserStats.GetStatInt(SHIPMENTS);
        tilesPlaced = Steamworks.SteamUserStats.GetStatInt(TILES_PLACED);
    }

    [Button]
    public static bool IsAchievementUnlocked(string achievementName)
    {
        if (!SteamManager.IsConnected)
            return false;

        foreach (var ach in Steamworks.SteamUserStats.Achievements)
        {
            if (ach.Identifier == achievementName)
            {
                return ach.State;
            }
        }

        return false;
    }

    private async Awaitable<bool> IsAchievementUnlockedAsync(string achievementName, bool forceStatUpdate = false)
    {
        if (!SteamManager.IsConnected)
            return false;

        if(!statsUpdated || forceStatUpdate)
            RequestStatUpdate();

        while (waitingForStats)
        {
            await Awaitable.NextFrameAsync();
        }

        foreach (var ach in Steamworks.SteamUserStats.Achievements)
        {
            if(ach.Identifier == achievementName)
            {
                return ach.State;
            }
        }

        return false;
    }

    private void ShipmentComplete(RequestType type)
    {
        shipmentCount++;
        SteamUserStats.AddStat(SHIPMENTS, 1);

        if (shipmentCount % 10 == 0 || shipmentCount == 1)
            ShowProgress(SHIPMENTS_100, shipmentCount, 100);

        if(shipmentCount % 100 == 0)
            ShowProgress(SHIPMENTS_1000, shipmentCount, 1000);

        if(shipmentCount >= 100 && !IsAchievementUnlocked(SHIPMENTS_100))
        {
            Achievement achievement = new Achievement(SHIPMENTS_100);
            TriggerAchievement(achievement);
        }
        else if(shipmentCount >= 1000 && !IsAchievementUnlocked(SHIPMENTS_1000))
        {
            Achievement achievement = new Achievement(SHIPMENTS_1000);
            TriggerAchievement(achievement);
        }
    }

    private void TilesPlaced(PlaceHolderTileBehavior behavior, HexTileType type)
    {
        tilesPlaced++;
        SteamUserStats.AddStat(TILES_PLACED, 1);

        if(tilesPlaced % 10 == 0 || tilesPlaced == 1)
            ShowProgress(TILES_PLACED_100, tilesPlaced, 100);

        if(tilesPlaced % 100 == 0)
            ShowProgress(TILES_PLACED_1000, tilesPlaced, 1000);

        if(tilesPlaced >= 100 && !IsAchievementUnlocked(TILES_PLACED_100))
        {
            Achievement achievement = new Achievement(TILES_PLACED_100);
            TriggerAchievement(achievement);
        }
        else if(tilesPlaced >= 1000 && !IsAchievementUnlocked(TILES_PLACED_1000))
        {
            Achievement achievement = new Achievement(TILES_PLACED_1000);
            TriggerAchievement(achievement);
        }
    }

    private void ShowProgress(string achievement, int curValue, int maxValue)
    {
        if(curValue >= maxValue)
            return;

        SteamUserStats.IndicateAchievementProgress(achievement, curValue, maxValue);
    }
}
