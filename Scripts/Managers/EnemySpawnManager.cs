using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using OWS.ObjectPooling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Manageable]
public class EnemySpawnManager : MonoBehaviour, ISaveData
{
    private List<EnemyCrystalBehavior> crystalList = new List<EnemyCrystalBehavior>();
    private List<EnemyUnit> spawnedEnemies = new List<EnemyUnit>();
    private List<SpawnedEnemyData> spawnedEnemyData = new List<SpawnedEnemyData>();
    private int crystalsWithNoTarget = 0;
    [SerializeField] private List<SpawnCurve> spawnCurves = new List<SpawnCurve>();
    [SerializeField] private List<EnemyPool> enemyPoolList = new List<EnemyPool>();

    public static event Action AllEnemiesKilled;
    private WaitForSeconds enemyCheckDelay = new WaitForSeconds(5f);
    [SerializeField] private GameObject markLocationPrefab;
    [SerializeField] private CommunicationBase eliteSerpentSpawn;
    private bool playedEliteSerpent = false;

    [SerializeField] private int spawnPower = 0;
    public int SpawnPower { get => spawnPower; }
    [SerializeField] private float RepPerSpawnPowerLevel = 400f;

    private List<LootManager.LootData> activeLoot = new();
    private EnemyCrystalManager ecm;

    [Header("Loot")]
    [SerializeField]
    private GameObject lootPrefab;
    private static ObjectPool<PoolObject> lootPool;
    public static event Action LootLoaded;

    [Header("First Spawn")]
    [SerializeField] private CommunicationBase placingIndicator;

    private void Awake()
    {
        if (lootPool == null)
            lootPool = new ObjectPool<PoolObject>(lootPrefab);
        RegisterDataSaving();
        ecm ??= FindFirstObjectByType<EnemyCrystalManager>();
    }

    private void OnEnable()
    {
        UnitManager.unitPlaced += FirstUnitPlaced;

        EnemyCrystalBehavior.enemyCrystalPlaced += CrystalAdded;
        EnemyCrystalBehavior.enemyCrystalDestroyed += CrystalRemoved;
        EnemyUnit.enemyUnitSpawned += UnitSpawned;
        EnemyUnit.enemyUnitKilled += UnitKilled;

        LootManager.lootAdded += EnemyLootDropped;
        CollectionBehavior.collected += EnemyLootCollected;

        EnemyCrystalBehavior.enemyLanding += PlaceIndicator;
        DayNightManager.toggleDay += CalculateSpawn;
        DayNightManager.transitionToNight += ClearSpawn;

        CheatCodes.AddButton(BuildingTutorialComplete.TriggerEndOfTutorial, "Start Spawn");

        EnemyCrystalBehavior.NoTargetFound += NoTargetForCrystal;
        EnemyCrystalBehavior.SpawnCanceled += NoTargetForCrystal;
        DayNightManager.transitionToNight += CheckEnemyCount;

        DayNightManager.toggleDay += SpawnEnemiesForNight;

        StartCoroutine(CreateEnemyPool());
        //enemyPoolList.ForEach(x => x.pool = new ObjectPool<EnemyUnit>(x.prefab,20));
    }

    private IEnumerator CreateEnemyPool()
    {
        for (int i = 0; i < enemyPoolList.Count; i++)
        {
            enemyPoolList[i].pool = new ObjectPool<EnemyUnit>(enemyPoolList[i].prefab, 20);
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

        LootManager.lootAdded -= EnemyLootDropped;
        CollectionBehavior.collected -= EnemyLootCollected;

        EnemyCrystalBehavior.enemyLanding -= PlaceIndicator;

        DayNightManager.toggleDay -= CalculateSpawn;
        DayNightManager.transitionToNight -= ClearSpawn;

        EnemyCrystalBehavior.NoTargetFound -= NoTargetForCrystal;
        EnemyCrystalBehavior.SpawnCanceled -= NoTargetForCrystal;

        DayNightManager.transitionToNight -= CheckEnemyCount;
        DayNightManager.toggleDay -= SpawnEnemiesForNight;
    }

    private void ClearSpawn(int dayNumber, float delay)
    {
        crystalsWithNoTarget = 0;
        spawnedEnemies.Clear();
        spawnedEnemyData.Clear();
        StartCoroutine(CheckEnemyStatus());
    }

    private IEnumerator CheckEnemyStatus()
    {
        yield return new WaitUntil(() => DayNightManager.isNight);
        yield return new WaitForSeconds(5f); //janky!!

        if(spawnedEnemyData.Count == 0)
        {
            AllEnemiesKilled?.Invoke();
            Debug.LogError("No Enemy Data");
            yield break;
        }

        while (!DayNightManager.isDay)
        {
            yield return null;
            for (int i = spawnedEnemyData.Count - 1; i >= 0; i--)
            {
                yield return null;
                if (i >= spawnedEnemyData.Count)
                    continue;

                SpawnedEnemyData enemyData = spawnedEnemyData[i];

                if (!enemyData.unit.gameObject.activeInHierarchy)
                {
                    spawnedEnemyData.Remove(enemyData);
                    continue;
                }

                //Is there a target and do we have a path to it?
                if (enemyData.targetingBehavior.target != null && enemyData.targetingBehavior.target.gameObject.activeInHierarchy && enemyData.targetingBehavior.HasPath)
                    continue;

                //Has the enemy moved?
                if ((enemyData.unit.transform.position - enemyData.lastPosition).sqrMagnitude > 0.01f)
                {
                    enemyData.lastPosition = enemyData.unit.transform.position;
                    enemyData.timeOfLastPosition = Time.time;
                    continue;
                }

                //Has the emeny been in the same spot for too long?
                if (Time.time - enemyData.timeOfLastPosition > 2f + spawnedEnemyData.Count * Time.deltaTime * 2)
                {
                    spawnedEnemyData.Remove(enemyData);
                    enemyData.unit.SelfDestruct();
                }
            }
        }
    }

    private void CalculateSpawn(int dayNumber)
    {
        spawnPower = Mathf.FloorToInt(ReputationManager.Reputation / RepPerSpawnPowerLevel);
    }

    private void PlaceIndicator(Hex3 position)
    {
        GameObject marker = Instantiate(markLocationPrefab, position.ToVector3() + Vector3.up * 0.01f, Quaternion.identity);
        EnemyIndicator.AddIndicatorObject(marker, IndicatorType.marker);

        if (DayNightManager.DayNumber == 1)
            CommunicationMenu.AddCommunication(placingIndicator);
    }

    private void UnitSpawned(EnemyUnit unit)
    {
        if (unit.GetComponent<EnemyCrystalBehavior>())
            return;

        if (!spawnedEnemies.Contains(unit))
        {
            spawnedEnemies.Add(unit);
            SpawnedEnemyData data = new SpawnedEnemyData();
            data.unit = unit;
            data.targetingBehavior = unit.GetComponent<TargetingBehavior>();
            data.lastPosition = unit.transform.position;
            data.timeOfLastPosition = Time.time;
            spawnedEnemyData.Add(data);
        }
    }

    private void UnitKilled(EnemyUnit unit)
    {
        spawnedEnemies.Remove(unit);

        if (spawnedEnemies.Count == 0)
            AllEnemiesKilled?.Invoke();
    }

    private void CheckEnemyCount(int day, float delay)
    {
        StartCoroutine(CheckForUnits());
    }

    private IEnumerator CheckForUnits()
    {
        yield return new WaitUntil(() => DayNightManager.isNight);
        yield return new WaitUntil(() => LootCollectedOrDelay(Time.timeSinceLevelLoad));
        yield return new WaitForSeconds(3f);
        while(!DayNightManager.isDay)
        {
            yield return enemyCheckDelay;
            if (spawnedEnemies.Count == 0 && !DayNightManager.isDay)
            {
                AllEnemiesKilled?.Invoke();
                Debug.Log("All Enemies Killed");
            }
        }
    }

    private bool LootCollectedOrDelay(float startTime)
    {
        return LootManager.LootCollected || Time.timeSinceLevelLoad > startTime + 20;
    }

    private void NoTargetForCrystal(EnemyCrystalBehavior behavior)
    {
        crystalsWithNoTarget++;
        int activeCrystals = crystalList.Count(c => c.PowerLevel > 0);

        if(crystalsWithNoTarget >= activeCrystals)
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
            Hex3 location = tiles[HexTileManager.GetNextInt(0, tiles.Count)];
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
                    StartCoroutine(SendEliteSerpentCommunication());
                    break;
                }
            }
        }

        return waves;
    }

    private IEnumerator SendEliteSerpentCommunication()
    {
        yield return new WaitUntil(() => DayNightManager.isDay && DayNightManager.NormalizedTime > 0.85f);
        CommunicationMenu.AddCommunication(eliteSerpentSpawn, false);
    }

    public EnemyUnit GetEnemy(EnemyUnitType type)
    {
        foreach (var pool in enemyPoolList)
        {
            if (pool.type == type)
                return pool.pool.Pull();
        }

        return null;
    }

    private void EnemyLootDropped(LootManager.LootData loot)
    {
        activeLoot.Add(loot);
    }

    private void EnemyLootCollected(LootManager.LootData loot)
    {
        activeLoot.Remove(loot);
    }

    public static GameObject PullLoot(Vector3 position, Quaternion rotation)
    {
        return lootPool.Pull(position, rotation).gameObject;
    }

    private const string SPAWN_MANAGER_PATH = "EnemySpawnManager";
    private const string ACTIVE_LOOT_PATH = "ActiveLoot";
    private const string ELITE_SPAWN_PLAYED = "EliteSerpentPlayed";
    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this,2.1f); //after enemy crystal manager to spawn emenies.
    }

    public void Save(string savePath, ES3Writer writer)
    {
        LootManager lootManager = FindFirstObjectByType<LootManager>();

        List<Vector3> lootPositions = new List<Vector3>();
        for (int i = 0; i < activeLoot.Count; i++)
        {
            if (i > lootManager.LootMax) //limiting the number of loot items
                break;

            if (activeLoot[i].position.y > 1f)//don't save loot that has been collected
                continue;
            Vector3 lootPosition = activeLoot[i].position;
            lootPosition.y = 0.25f; //setting this in case the loot was being picked up by a collection tower
            lootPositions.Add(lootPosition);
        }
        
        writer.Write<int>(SPAWN_MANAGER_PATH, spawnPower);
        writer.Write<bool>(ELITE_SPAWN_PLAYED, playedEliteSerpent);
        writer.Write<List<Vector3>>(ACTIVE_LOOT_PATH, lootPositions);
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if (ES3.KeyExists(SPAWN_MANAGER_PATH, loadPath))
        {
            spawnPower = ES3.Load<int>(SPAWN_MANAGER_PATH, loadPath);
        }

        yield return null;

        if (ES3.KeyExists(ELITE_SPAWN_PLAYED, loadPath))
        {
            playedEliteSerpent = ES3.Load<bool>(ELITE_SPAWN_PLAYED, loadPath);
        }
        yield return null;

        if (ES3.KeyExists(ACTIVE_LOOT_PATH, loadPath))
        {
            LootManager lootManager = FindFirstObjectByType<LootManager>();
            if(lootManager == null)
            {
                Debug.LogError("Could not find LootManager in scene");
                yield break;
            }
            List<Vector3> lootPositions = ES3.Load<List<Vector3>>(ACTIVE_LOOT_PATH, loadPath);
            for (int i = 0; i < lootPositions.Count; i++)
            {
                if (i > lootManager.LootMax) //limiting the number of loot items
                    break;
                if (lootPositions[i].y > 1f) //don't load loot that's in the air
                    continue;
                lootManager.AddLoot(lootPositions[i]);
            }

            postUpdateMessage?.Invoke($"Placing {ResourceType.Terrene.ToNiceString()} Drops");

            LootLoaded?.Invoke();
        }

        yield return SpawnEnemiesForNextNight(postUpdateMessage);

        yield return null;

    }

    private void SpawnEnemiesForNight(int dayNumber)
    {
        StartCoroutine(SpawnEnemiesForNextNight());
    }

    private IEnumerator SpawnEnemiesForNextNight(Action<string> postUpdateMessage = null)
    {
        //get total numbers needed
        List<int> spawnPowers = ecm.GetCrystalsStartSpawnPower();
        if(spawnPowers.Count == 0)
            yield break;

        List<EnemyUnitType> typesNeeded = new();
        List<int> numberNeeded = new();

        for (int i = 0; i < spawnPowers.Count; i++)
        {
            int spawnPower = Mathf.Max(1, SpawnPower - spawnPowers[i]);
            List<Wave> wave = GetSpawnWaves(spawnPower - spawnPowers[i]);
            for (int j = 0; j < wave.Count; j++)
            {
                if(typesNeeded.Contains(wave[j].type))
                {
                    int index = typesNeeded.IndexOf(wave[j].type);
                    numberNeeded[index] += wave[j].number;
                }
                else
                {
                    typesNeeded.Add(wave[j].type);
                    numberNeeded.Add(wave[j].number);
                }
            }
        }

        yield return null;

        //compare to the number in the object pools
        for (int i = 0; i < typesNeeded.Count; i++)
        {
            for (int j = 0; j < enemyPoolList.Count; j++)
            {
                EnemyPool pool = enemyPoolList[j];
                if (pool.type != typesNeeded[i])
                    continue;

                if (pool.pool.pooledCount > numberNeeded[i])
                    break;

                int amountToAdd = Mathf.Max(0, numberNeeded[i] - pool.pool.pooledCount);

                for (int k = 0; k < amountToAdd; k++)
                {
                    postUpdateMessage?.Invoke($"Spawning Enemy Units {k} of {amountToAdd}");
                    pool.pool.AddToPool(1); //add to the pool
                    yield return null;
                }
            }
        }
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

    private class SpawnedEnemyData
    {
        public EnemyUnit unit;
        public TargetingBehavior targetingBehavior;
        public Vector3 lastPosition;
        public float timeOfLastPosition;
    }
}
