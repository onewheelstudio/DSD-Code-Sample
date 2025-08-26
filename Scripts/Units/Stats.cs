using HexGame.Grid;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ManageableData]
[CreateAssetMenu(menuName = "Hex/Unit Stats")]
public class Stats : SerializedScriptableObject, IUpgradeable
{
    public Dictionary<Stat, float> instanceStats = new Dictionary<Stat, float>();
    public Dictionary<Stat, float> stats = new Dictionary<Stat, float>();
    [NonSerialized]
    private List<StatsUpgrade> appliedUpgrades = new List<StatsUpgrade>();
    [NonSerialized]
    private static List<GlobalUpgrade> globalUpgrades = new List<GlobalUpgrade>();
    public List<HexGame.Resources.HexTileType> placementList = new List<HexGame.Resources.HexTileType>();
    public List<PlacementCondition> placementConditions = new List<PlacementCondition>();

    public event Action<Stats, StatsUpgrade> upgradeApplied;
    public static event Action<Stats, StatsUpgrade> UpgradeApplied;

    [Header("Death")]
    [SerializeField] private GameObject deathParticles;
    private static ObjectPool<PoolObject> deathParticlePool;

    public float this[Stat stat]
    {
        get
        {
            return GetStat(stat);
        }
    }
    public float GetStat(Stat stat)
    {
        if (instanceStats.TryGetValue(stat, out float instanceValue))
            return GetUpgradedValue(stat, instanceValue);
        else if (stats.TryGetValue(stat, out float value))
            return GetUpgradedValue(stat, value);
        else
            return GetDefaultValue(stat);
    }

    private float GetDefaultValue(Stat stat)
    {
        return stat switch
        {
            Stat.happiness => 0,
            Stat.speed => 1f * GameConstants.GameSpeed,
            Stat.reloadTime => 0.5f / GameConstants.GameSpeed,
            Stat.reputation => 25f,
            Stat.shield => 0f,
            Stat.workers => 0f,
            _ => 1f,
        };
    }

    public int GetStatAsInt(Stat stat)
    {
        return (int)GetStat(stat);
    }

    public void UnlockUpgrade(StatsUpgrade upgrade)
    {
        if (!appliedUpgrades.Contains(upgrade))
        {
            appliedUpgrades.Add(upgrade);
            upgradeApplied?.Invoke(this, upgrade);
            UpgradeApplied?.Invoke(this, upgrade);
        }
    }

    public static void UnlockGlobalUpgrade(GlobalUpgrade globalUpgrade)
    {
        if (!globalUpgrades.Contains(globalUpgrade))
            globalUpgrades.Add(globalUpgrade);
    }

    public float GetUpgradedValue(Stat stat, float baseValue)
    {
        float newValue = GetLocalUpgrades(stat, baseValue);
        return GetGlobalStatUpgrade(stat, newValue);
    }

    private float GetLocalUpgrades(Stat stat, float baseValue)
    {
        for (int i = 0; i < appliedUpgrades.Count; i++)
        {
            StatsUpgrade upgrade = appliedUpgrades[i];
            if (!upgrade.upgradeToApply.TryGetValue(stat, out float upgradeValue))
                continue;

            if (upgrade.isPercentUpgrade)
                baseValue *= (upgradeValue / 100f) + 1f;
            else
                baseValue += upgradeValue;
        }

        return baseValue;
    }
    private static float GetGlobalStatUpgrade(Stat stat, float value)
    {
        for (int i = 0; i < globalUpgrades.Count; i++)
        {
            GlobalUpgrade globalUpgrade = globalUpgrades[i];
            if (globalUpgrade.statType == stat && !globalUpgrade.isPercent)
                value += globalUpgrade.statValue;
        }

        //apply percent upgrades at the end
        for (int i = 0; i < globalUpgrades.Count; i++)
        {
            GlobalUpgrade globalUpgrade = globalUpgrades[i];
            if (globalUpgrade.statType == stat && globalUpgrade.isPercent)
                value *= (1f + globalUpgrade.statValue / 100f);
        }

        switch (stat)
        {
            case Stat.speed:
                value *= GameConstants.GameSpeed;
                break;
            case Stat.reloadTime:
                value /= GameConstants.GameSpeed;
                break;
        }

        return value;
    }

    [Button]
    public void ResetAppliedUpgrades()
    {
        appliedUpgrades.Clear();
    }

    public void DoDeathParticles(Vector3 position)
    {
        if (deathParticles == null)
            return;

        if (deathParticlePool == null)
            deathParticlePool = new ObjectPool<PoolObject>(deathParticles);

        deathParticlePool.Pull(position);
    }

    public void AddStat(Stat stat, float value)
    {
        if (stats.ContainsKey(stat))
            stats[stat] = value;
        else
            stats.Add(stat, value);
    }

    public bool CanPlaceAtLocation(Hex3 location)
    {
        if(placementList.Count == 0)
            return true;

        return placementConditions.TrueForAll(x => x.CanBePlaced(location));
    }

    public bool HasStat(Stat stat)
    {
        return stats.ContainsKey(stat) || instanceStats.ContainsKey(stat);
    }
}

public enum Stat
{
    hitPoints,
    shield,
    speed,
    movementRange,
    happiness,
    minRange,
    maxRange,
    reloadTime,
    damage,
    aoeRange,
    reputation,
    maxStorage,
    burst,
    workers,
    housing,
    sightDistance,
    charges,
    armor,
}
