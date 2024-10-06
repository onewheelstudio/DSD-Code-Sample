using HexGame.Grid;
using HexGame.Units;
using OWS.ObjectPooling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Manageable]
public class EnemySpawnManager : MonoBehaviour
{
    private List<EnemyCrystalBehavior> crystalList = new List<EnemyCrystalBehavior>();
    private List<EnemyUnit> spawnedEnemies = new List<EnemyUnit>();
    [SerializeField] private List<SpawnCurve> spawnCurves = new List<SpawnCurve>();
    [SerializeField] private List<EnemyPool> enemyPoolList = new List<EnemyPool>();

    public static event Action AllEnemiesKilled;
    [SerializeField] private GameObject markLocationPrefab;
    [SerializeField] private CommunicationBase eliteSerpentSpawn;
    private bool playedEliteSerpent = false;

    [SerializeField] private int spawnPower = 0;
    public int SpawnPower { get => spawnPower; } 

    private void OnEnable()
    {
        UnitManager.unitPlaced += FirstUnitPlaced;

        EnemyCrystalBehavior.enemyCrystalPlaced += CrystalAdded;
        EnemyCrystalBehavior.enemyCrystalDestroyed += CrystalRemoved;
        EnemyUnit.enemyUnitSpawned += UnitSpawned;
        EnemyUnit.enemyUnitKilled += UnitKilled;


        EnemyCrystalBehavior.enemyLanding += PlaceIndicator;
        DayNightManager.toggleDay += CalculateSpawn;
        DayNightManager.transitionToNight += ClearSpawn;

        CheatCodes.AddButton(BuildingTutorialComplete.TriggerEndOfTutorial, "Start Spawn");

        StartCoroutine(SpawnEnemies());
        //enemyPoolList.ForEach(x => x.pool = new ObjectPool<EnemyUnit>(x.prefab,20));
    }

    private IEnumerator SpawnEnemies()
    {
        for (int i = 0; i < enemyPoolList.Count; i++)
        {
            enemyPoolList[i].pool = new ObjectPool<EnemyUnit>(enemyPoolList[i].prefab,20);
            yield return null;
        }
    }

    private void OnDisable()
    {
        UnitManager.unitPlaced -= FirstUnitPlaced;

        EnemyCrystalBehavior.enemyCrystalPlaced -= CrystalAdded;
        EnemyCrystalBehavior.enemyCrystalDestroyed -= CrystalRemoved;
        EnemyUnit.enemyUnitSpawned -= UnitSpawned;
        EnemyUnit.enemyUnitKilled -= UnitKilled;

        EnemyCrystalBehavior.enemyLanding -= PlaceIndicator;

        DayNightManager.toggleDay -= CalculateSpawn;
        DayNightManager.transitionToNight -= ClearSpawn;
    }

    private void ClearSpawn(int dayNumber, float delay)
    {
        spawnedEnemies.Clear();
    }

    private void CalculateSpawn(int dayNumber)
    {
        spawnPower = Mathf.FloorToInt(ReputationManager.Reputation / 250f);
    }

    private void PlaceIndicator(Hex3 position)
    {
        GameObject marker = Instantiate(markLocationPrefab, position.ToVector3() + Vector3.up * 0.01f, Quaternion.identity);
        EnemyIndicator.AddIndicatorObject(marker, IndicatorType.marker);
    }

    private void UnitSpawned(EnemyUnit unit)
    {
        if (unit.GetComponent<EnemyCrystalBehavior>())
            return;

        if (!spawnedEnemies.Contains(unit))
            spawnedEnemies.Add(unit);
    }

    private void UnitKilled(EnemyUnit unit)
    {
        spawnedEnemies.Remove(unit);

        if (spawnedEnemies.Count == 0)
            AllEnemiesKilled?.Invoke();
    }

    private void FirstUnitPlaced(Unit obj)
    {
        UnitManager.unitPlaced -= FirstUnitPlaced;
    }

    private Hex3 FindEmptyHexAtRange(Hex3 center, int distance)
    {
        List<Hex3> tiles = Hex3.GetNeighborsAtDistance(center, distance);

        for (int i = 0; i < tiles.Count; i++)
        {
            Hex3 location = tiles[HexTileManager.GetNextInt(0,tiles.Count)];
            if (!UnitManager.PlayerUnitAtLocation(location))
                return location;

            tiles.Remove(location);
        }

        if (distance > 50)
            return Hex3.Zero;

        return FindEmptyHexAtRange(center, distance + 1);
    }

    private void CrystalRemoved(EnemyCrystalBehavior crystal)
    {
        crystalList.Remove(crystal);
    }

    private void CrystalAdded(EnemyCrystalBehavior crystal)
    {
        crystalList.Add(crystal);
    }

    public int EnemiesRemaining()
    {
        return spawnedEnemies.Count;
    }
    public List<Wave> GetSpawnWaves(int powerLevel)
    {
        int spawLevel = Mathf.Max(1, powerLevel);

        List<Wave> waves = spawnCurves.Where(x => x.powerLevel <= spawLevel)
                          .OrderByDescending(x => x.powerLevel)
                          .First().waves;

        if (!playedEliteSerpent)
        {
            foreach (var wave in waves)
            {
                if (wave.type == EnemyUnitType.serpentElite && !playedEliteSerpent)
                {
                    playedEliteSerpent = true;
                    CommunicationMenu.AddCommunication(eliteSerpentSpawn, false);
                    break;
                }
            }
        }

        return waves;
    }
    public EnemyUnit GetEnemy(EnemyUnitType type)
    {
        return enemyPoolList.Find(x => x.type == type).pool.Pull();
    }

    [System.Serializable]
    private class EnemyPool
    {
        public EnemyUnitType type;
        public GameObject prefab;
        public ObjectPool<EnemyUnit> pool;
    }

    [System.Serializable]
    private struct SpawnCurve
    {
        public int powerLevel;
        public List<Wave> waves;
    }
}
