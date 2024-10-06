using HexGame.Units;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using HexGame.Grid;
using System.Collections.ObjectModel;

public class EnemyCrystalManager : MonoBehaviour
{
    [SerializeField, DisableIf("@true")] private List<EnemyCrystalBehavior> enemyCystals = new List<EnemyCrystalBehavior>();
    public static event Action<EnemyCrystalBehavior> crystalPoweredUp;
    private bool canAddPower = false;
    [SerializeField] private int delayToPowerUp = 10;
    [SerializeField] private int powerLevelPerCrystal = 4;
    private EnemySpawnManager spawnManager;

    private void Awake()
    {
        spawnManager = FindObjectOfType<EnemySpawnManager>();
    }

    private void OnEnable()
    {
        EnemyCrystalBehavior.enemyCrystalPlaced += CrystalAdded;
        EnemyCrystalBehavior.enemyCrystalDestroyed += CrystalRemoved;
        DayNightManager.toggleDay += AddPower;
        UnitManager.unitPlaced += FirstUnitPlaced;

        BuildingTutorialComplete.buildingTutorialComplete += AddFirstPower;
    }

    private void OnDisable()
    {
        EnemyCrystalBehavior.enemyCrystalPlaced -= CrystalAdded;
        EnemyCrystalBehavior.enemyCrystalDestroyed -= CrystalRemoved;
        DayNightManager.toggleDay -= AddPower;
        UnitManager.unitPlaced -= FirstUnitPlaced;

        BuildingTutorialComplete.buildingTutorialComplete -= AddFirstPower;
    }

    private void CrystalAdded(EnemyCrystalBehavior crystal)
    {
        if (!enemyCystals.Contains(crystal))
        {
            enemyCystals.Add(crystal);
            enemyCystals.Sort((x,y) => x.transform.position.sqrMagnitude.CompareTo(y.transform.position.sqrMagnitude)); //sort by distance from origin;
        }
    }

    private void CrystalRemoved(EnemyCrystalBehavior crystal)
    {
        enemyCystals.Remove(crystal);
    }
    private void FirstUnitPlaced(Unit obj)
    {
        AddPower();
        //unsub to only get the first unit
        UnitManager.unitPlaced -= FirstUnitPlaced;
    }
    private void AddFirstPower()
    {
        canAddPower = true;
        AddPower();
    }

    [Button]
    private void AddPower(int dayNumber = 0)
    {
        if(!canAddPower)
            return;

        int numberOfCrystals = Mathf.Max(1, spawnManager.SpawnPower / powerLevelPerCrystal + 1);
        if (enemyCystals.Count == 0)
                return;

        for(int i = 0; i < numberOfCrystals; i++)
        {
            if (i >= enemyCystals.Count)
                break;

            if (enemyCystals[i].TurnOnCrystal())
                crystalPoweredUp?.Invoke(enemyCystals[i]);
        }
    }

    public int NumberOfPoweredCrystals()
    {
        int count = 0;
        foreach (var crystal in enemyCystals)
        {
            if (crystal.PowerLevel > 0)
                count++;
        }

        return count;
    }

    public bool IsCrystalNearBy(Hex3 location, out EnemyCrystalBehavior nearbyCrystal, int range = 2)
    {
        foreach (var crystal in enemyCystals)
        {
            if(Hex3.DistanceBetween(location, crystal.transform.position.ToHex3()) <= range)
            {
                crystal.TryAddFogRevealer();
                crystal.DoNova();
                nearbyCrystal = crystal;
                return true;
            }
        }

        nearbyCrystal = null;
        return false;
    }

    public void DiscoverCrystal(EnemyCrystalBehavior crystal)
    {
        crystal.TurnOnCrystal();
    }

    public ReadOnlyCollection<EnemyCrystalBehavior> GetCrystals()
    {
        return enemyCystals.AsReadOnly();
    }

}
