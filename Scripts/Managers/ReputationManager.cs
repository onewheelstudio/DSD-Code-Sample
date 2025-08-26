using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReputationManager : MonoBehaviour, ISaveData
{
    private static int reputation;
    public static int Reputation => reputation;

    private static CorporateTier tier = CorporateTier.NONE;
    public static CorporateTier Tier => tier;

    public static event Action<CorporateTier> tierChanged;
    public static event Action<int> reputationChanged;

    private void Awake()
    {
        reputation = 0;
        tier = CorporateTier.NONE;
        RegisterDataSaving();
    }

    public static void ChangeReputation(int amount)
    {
        reputation += amount;
        if(reputation < 0)
            reputation = 0;

        reputationChanged?.Invoke(reputation);
    }

    public static void LoseReputation(int repReward)
    {
        ChangeReputation(-repReward * 2);
    }

    public static void CalculateTier()
    {
        CorporateTier tier = CorporateTier.NONE;
        if (reputation < 0)
            tier = CorporateTier.NONE;
        else if (reputation < 100)
            tier = CorporateTier.Manager;
        else if (reputation < 200)
            tier = CorporateTier.Director;
        else if (reputation < 300)
            tier = CorporateTier.Executive;
        else
            tier = CorporateTier.CEO;

        if(tier != ReputationManager.tier)
        {
            ReputationManager.tier = tier;
            tierChanged?.Invoke(tier);
            MessagePanel.ShowMessage($"Corporate standing improved to {tier.ToString()}", null);
        }
    }

    private const string REP_SAVE_STRING = "RepData";
    private const string CORP_TIER_STRING = "CorpTier";

    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this);
    }

    public void Save(string savePath, ES3Writer writer)
    {
        writer.Write<int>(REP_SAVE_STRING, reputation);
        writer.Write<int>(CORP_TIER_STRING, (int)tier);
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if (ES3.KeyExists(REP_SAVE_STRING, loadPath))
            reputation = ES3.Load<int>(REP_SAVE_STRING, 0);
        if (ES3.KeyExists(CORP_TIER_STRING, loadPath))
            tier = (CorporateTier)ES3.Load<int>(CORP_TIER_STRING, 0);

        reputationChanged?.Invoke(reputation);
        yield return null;
    }

    public enum CorporateTier
    {
        NONE,
        Manager = 1,
        Director = 2,
        Executive = 3,
        CEO = 4,
    }
}
