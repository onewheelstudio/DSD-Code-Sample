using HexGame.Units;
using Sirenix.OdinInspector;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteamLeaderBoards : MonoBehaviour, ISaveData
{
    [SerializeField] private GameSettings gameSettings;
    private const string REPUTATION_LEADERBOARD = "SingleGameReputation";
    private const string TECH_CREDIT_LEADERBOARD = "SingleGameTechCredits";

    //stats
    private const string LIFETIME_CREDITS = "TechCredits";
    [ShowInInspector]
    private int creditsEarned = 0;
    [ShowInInspector]
    private int lifetimeCredits = 0;

    private void Awake()
    {
        RegisterDataSaving();
    }

    private void OnEnable()
    {
        if (gameSettings.IsDemo)
            return;

        ReputationManager.reputationChanged += ReputationChanged;
        HexTechTree.techCreditEarned += TechCreditEarned;
        DayNightManager.toggleDay += UpdateLeaderBoards;

        Steamworks.SteamUserStats.OnUserStatsReceived += StatsReceived;
    }

    private void OnDisable()
    {
        ReputationManager.reputationChanged -= ReputationChanged;
        HexTechTree.techCreditEarned -= TechCreditEarned;
        DayNightManager.toggleDay -= UpdateLeaderBoards;

        Steamworks.SteamUserStats.OnUserStatsReceived -= StatsReceived;
    }

    private void StatsReceived(SteamId id, Result result)
    {
        lifetimeCredits = Steamworks.SteamUserStats.GetStatInt(LIFETIME_CREDITS);
    }

    [Button]
    private void UpdateLeaderBoards(int dayNumber)
    {
        if (!SteamManager.IsConnected)
            return;

        var repLBUpdate = UpdateLeaderBoard(REPUTATION_LEADERBOARD, ReputationManager.Reputation).GetAwaiter().GetResult();
        if(WasLeaderBoardUpdateSuccessful(repLBUpdate.leaderBoard, repLBUpdate.update) && repLBUpdate.update.NewGlobalRank < repLBUpdate.update.OldGlobalRank)
        {
            int rank = Mathf.Max(1, repLBUpdate.update.NewGlobalRank);
            int total = Mathf.Max(rank, repLBUpdate.leaderBoard.EntryCount);
            MessageData messageData = new MessageData();
            messageData.message = $"Corporate Ranking: {rank} out of {total} in reputation.";
            messageData.messageColor = ColorManager.GetColor(ColorCode.repuation);
            MessagePanel.ShowMessage(messageData);
        }
        
        var creditLBUpdate = UpdateLeaderBoard(TECH_CREDIT_LEADERBOARD, creditsEarned).GetAwaiter().GetResult();
        if(WasLeaderBoardUpdateSuccessful(creditLBUpdate.leaderBoard, creditLBUpdate.update) && creditLBUpdate.update.NewGlobalRank < creditLBUpdate.update.OldGlobalRank)
        {
            int rank = Mathf.Max(1, creditLBUpdate.update.NewGlobalRank);
            int total = Mathf.Max(rank, creditLBUpdate.leaderBoard.EntryCount); 
            MessageData messageData = new MessageData();
            messageData.message = $"Corporate Ranking: {rank} out of {total} in credits earned.";
            messageData.messageColor = ColorManager.GetColor(ColorCode.techCredit);
            MessagePanel.ShowMessage(messageData);
        }

        var creditsLifeTimeUpdate = UpdateLeaderBoard(LIFETIME_CREDITS, lifetimeCredits).GetAwaiter().GetResult();
        if(WasLeaderBoardUpdateSuccessful(creditsLifeTimeUpdate.leaderBoard, creditsLifeTimeUpdate.update) && creditsLifeTimeUpdate.update.Score <= lifetimeCredits)
        {
            if (creditsLifeTimeUpdate.leaderBoard.EntryCount < 50)
                return;

            float percentile = (float) creditsLifeTimeUpdate.update.NewGlobalRank / (float)creditsLifeTimeUpdate.leaderBoard.EntryCount;
            if(percentile < 0.1f && !SteamStatsAndAchievements.IsAchievementUnlocked("Top10Percent_Credits"))
            {
                Achievement achievement = new Achievement("Top10Percent_Credits");
                SteamStatsAndAchievements.TriggerAchievement(achievement);

                if (creditsLifeTimeUpdate.update.NewGlobalRank < creditsLifeTimeUpdate.update.OldGlobalRank)
                {
                    int rank = Mathf.Max(1, creditsLifeTimeUpdate.update.NewGlobalRank);
                    int total = Mathf.Max(rank, creditsLifeTimeUpdate.leaderBoard.EntryCount);
                    MessageData messageData = new MessageData();
                    messageData.message = $"Corporate Ranking: {rank} out of {total} in lifetime credits earned.";
                    messageData.messageColor = ColorManager.GetColor(ColorCode.techCredit);
                    MessagePanel.ShowMessage(messageData);
                }
            }
        }
    }

    private void ReputationChanged(int obj)
    {
        
    }

    private void TechCreditEarned(int credits)
    {
        creditsEarned += credits;
        lifetimeCredits += credits;
        SteamUserStats.AddStat(LIFETIME_CREDITS, credits);
    }

    private async Awaitable<(Leaderboard leaderBoard, LeaderboardUpdate update)> UpdateLeaderBoard(string leaderboard, int score)
    {
        var lbResult = await Steamworks.SteamUserStats.FindOrCreateLeaderboardAsync(leaderboard, LeaderboardSort.Descending, LeaderboardDisplay.Numeric);
        Leaderboard leaderBoard;

        if (lbResult.HasValue)
            leaderBoard = lbResult.Value;
        else
            return (new Leaderboard(), new LeaderboardUpdate());

        var updateResults = await leaderBoard.SubmitScoreAsync(score);
        if (updateResults.HasValue)
            return (leaderBoard, updateResults.Value);
        else
            return (leaderBoard, new LeaderboardUpdate());
    }

    private bool WasLeaderBoardUpdateSuccessful(Leaderboard leaderBoard, LeaderboardUpdate update)
    {
        if (string.IsNullOrEmpty(leaderBoard.Name))
        {
            //Debug.LogError($"Leaderboard update failed.");
            return false;
        }
        else if (update.Score <= 0)
            return false;
        else
            return true;
    }

    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this);
    }

    private const string LEADERBOARD_DATA = "LeaderboardData";
    public void Save(string savePath, ES3Writer writer)
    {
        writer.Write<int>(LEADERBOARD_DATA, creditsEarned);
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if (ES3.KeyExists(LEADERBOARD_DATA, loadPath))
        {
            creditsEarned = ES3.Load<int>(LEADERBOARD_DATA, loadPath);
        }

        yield return null;
    }
}
