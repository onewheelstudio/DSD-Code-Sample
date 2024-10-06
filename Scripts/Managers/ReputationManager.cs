using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReputationManager : MonoBehaviour
{
    [SerializeField, ShowInInspector] private static int reputation;
    public static int Reputation => reputation;

    [SerializeField,ShowInInspector]private static CorporateTier tier = CorporateTier.NONE;
    public static CorporateTier Tier => tier;
    public static event Action<CorporateTier> tierChanged;
    public static event Action<int> reputationChanged;

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

    public enum CorporateTier
    {
        NONE,
        Manager = 1,
        Director = 2,
        Executive = 3,
        CEO = 4,
    }
}
