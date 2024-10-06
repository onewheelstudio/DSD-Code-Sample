using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks.Data;

public class SteamStatsAndAchievements : MonoBehaviour
{
    private const string NIGHT_COUNT = "NIGHTS";


    private void OnEnable()
    {
        DayNightManager.toggleDay += AddNewDay;
        UnlockWorkerMenuButton.WorkerButtonUnlocked += TutorialComplete;
        HexTechTree.techCreditEarned += TechCreditEarned;
    }

    private void OnDisable()
    {
        DayNightManager.toggleDay -= AddNewDay;
        UnlockWorkerMenuButton.WorkerButtonUnlocked -= TutorialComplete;
        HexTechTree.techCreditEarned -= TechCreditEarned;
    }



    [Button]
    private void AddNewDay(int obj)
    {
        if (!SteamManager.IsConnected)
            return;

        Steamworks.SteamUserStats.SetStat(NIGHT_COUNT, obj);
        if(obj == 1)
        { 
            Achievement achievement = new Achievement("FIRST_NIGHT");
            TriggerAchievement(achievement);
        }
        if(obj >= 25)
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
        int amountCollected = HexTechTree.TotalTechCreditsCollected;

        //if adding more achievements, make sure to add a check for the achievement to see if it has already been earned
        if(amountCollected >= 100000)
        {
            Achievement achievement = new Achievement("100K_TECH_CREDIT");
            TriggerAchievement(achievement);
        }
    }

    private void TriggerAchievement(Achievement ach)
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
}
