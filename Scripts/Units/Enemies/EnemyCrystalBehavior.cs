using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using OWS.ObjectPooling;
using Pathfinding;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCrystalBehavior : UnitBehavior
{
    [SerializeField] private GameObject fogRevealerPrefab;

    public static event Action<EnemyCrystalBehavior> enemyCrystalPlaced;
    public static event Action<EnemyCrystalBehavior> enemyCrystalDestroyed;
    public static event Action<EnemyCrystalBehavior> newCrystalPoweredUp;
    private List<Seeker> seekers = new List<Seeker>();
    private HexTileManager htm;
    private EnemySpawner spawner;

    public static event Action<Hex3> enemyLanding;
    public static event Action<PlayerUnit> enemyTargetSet;
    public static event Action<EnemyCrystalBehavior> NoTargetFound;

    //do you want implement some visuals??
    private GameObject fogRevealer;
    private static int maxPower = 1;
    private int startingSpawnPower = 0;
    public int StartingSpawnPower => startingSpawnPower;
    private bool placedHill = false;
    [SerializeField, ProgressBar(0,"@maxPower")] private int powerLevel = 0;

    [Header("Nova")]
    private static ObjectPool<PoolObject> novaPool;
    [SerializeField] private GameObject novaParticalPrefab;
    private static EnemySpawnManager esm;

    [Header("Models")]
    [SerializeField] private GameObject crystalOn;
    [SerializeField] private GameObject crystalOff;
    public static event Action<EnemyCrystalBehavior> SpawnCanceled;

    public int PowerLevel { get => powerLevel;}

    private void Awake()
    {
        htm = FindObjectOfType<HexTileManager>();
        spawner = GetComponent<EnemySpawner>();
        novaPool = new ObjectPool<PoolObject>(novaParticalPrefab);
        if(esm == null)
            esm = FindObjectOfType<EnemySpawnManager>();

        crystalOff.SetActive(true);
        crystalOn.SetActive(false);
    }

    private void OnEnable()
    {
        DayNightManager.transitionToNight += SpawnEnemy;
    }

    private void OnDisable()
    {
        DayNightManager.transitionToNight -= SpawnEnemy;
    }

    public override void StartBehavior()
    {
        _isFunctional = true;
        enemyCrystalPlaced?.Invoke(this);
    }

    public override void StopBehavior()
    {
        //turn off fog stuff here
        _isFunctional = false;
        enemyCrystalDestroyed?.Invoke(this);
    }

    [Button]
    public bool TurnOnCrystal()
    {
        EnemyIndicator.AddIndicatorObject(this.gameObject, IndicatorType.crystal);

        if (powerLevel == 0)
        {
            crystalOff.SetActive(false);
            crystalOn.SetActive(true);

            TryAddFogRevealer();
            startingSpawnPower = esm.SpawnPower - 1;
            powerLevel++;
            newCrystalPoweredUp?.Invoke(this);
            DoNova();

            MessageData messageData = new MessageData();
            messageData.messageObject = this.gameObject;
            messageData.messageColor = ColorManager.GetColor(ColorCode.offPriority);
            messageData.message = "Enemy Structure Powering Up";
            MessagePanel.ShowMessage(messageData).SetDisplayTime(20f);
            return true;
        }
        else
            return false;
    }
    public void TryAddFogRevealer()
    {
        if(fogRevealer != null)
            return;

        fogRevealer = Instantiate(fogRevealerPrefab, this.transform.position, Quaternion.identity, this.transform);
        this.gameObject.GetComponent<FogUnit>().UpdateRevealStatus();
    }

    public void SpawnEnemy(int daynumber, float transitionTime)
    {
        if (powerLevel == 0)
            return;
        
        PlayerUnit target = EnemyTargeting.GetHighestValueTarget(this.transform.position);
        if (target == null)
        {
            NoTargetFound?.Invoke(this);
            return;
        }
        GeneratePathFromSpawnPoint(this.transform.position, target);
    }

    private void GeneratePathFromSpawnPoint(Hex3 spawnPoint, PlayerUnit target)
    {
        Seeker seeker = GetSeeker();
        spawnPoint.s = -spawnPoint.q - spawnPoint.r; //making it easier to test can be removed.
        seeker.StartPath(spawnPoint, target.transform.position, (x) => OnPathComplete(x,target));
    }

    private Seeker GetSeeker()
    {
        foreach (var seeker in seekers)
        {
            if (seeker.IsDone())
                return seeker;
        }

        Seeker newSeeker = this.gameObject.AddComponent<Seeker>();
        newSeeker.traversableTags = (1 << 5) | (1 << 0) | (1 << 2) |(1<< 11) | (1<<16) | (1<<15) |(1<<14)|(1<<13); //flat, forest, empty
        newSeeker.startEndModifier = new StartEndModifier() { exactEndPoint = StartEndModifier.Exactness.SnapToNode };
        seekers.Add(newSeeker);
        return newSeeker;
    }

    private void OnPathComplete(Path p, PlayerUnit target)
    {
        if (p.error)
        { 
            Debug.LogError("Couldn't find a path...");
            SpawnCanceled?.Invoke(this);
        }
        else
        {
            StartCoroutine(DoSpawn(p.vectorPath, target));
        }
    }

    private IEnumerator DoSpawn(List<Vector3> path, PlayerUnit target)
    {
        GetLandingMarkerPosition(path, target); //marker gets placed after this
        yield return SpawnTilesForPath(path);
        Vector3 spawnPosition = htm.GetRandomEmptyNeighbor(path[0]);
        if(!DayNightManager.isNight)
            yield return new WaitUntil(() => DayNightManager.isNight);
        if(!LootManager.LootCollected)
            yield return new WaitUntil(() => LootManager.LootCollected);
        DoNova();
        yield return new WaitForSeconds(1f);
        PlaceEnemySpawner(spawnPosition);
        enemyTargetSet?.Invoke(target);
    }

    //gets the first location near a player building to give a warning of where enemy is landing
    private Vector3 GetLandingMarkerPosition(List<Vector3> path, PlayerUnit target)
    {
        Vector3 markerLocation = Vector3.zero;

        for(int i = path.Count - 1; i >= 0; i--)
        {
            if (UnitManager.PlayerUnitAtLocation(path[i]) != null)
                continue;

            HexTile tile = HexTileManager.GetHexTileAtLocation(path[i]);

            if(tile == null)
            {
                markerLocation = path[i];
                break;
            }

            if (tile.TileType == HexTileType.mountain || tile.TileType == HexTileType.water)
                continue;

            markerLocation = path[i];
            break;
        }

        //if no hidden tile mark the start of the path
        enemyLanding?.Invoke(markerLocation);
        return markerLocation;
    }

    private IEnumerator SpawnTilesForPath(List<Vector3> path)
    {
        foreach (var point in path)
        {
            htm.PlaceAtLocation(point, HexTileType.grass);
            yield return null;
        }

        //add "landing area"
        foreach (var neighbor in htm.GetEmptyNeighborLocations(path[0]))
        {
            if (!path.Contains(neighbor))
            {
                htm.PlaceAtLocation(neighbor, HexTileType.grass);
                path.Add(neighbor);
                yield return null;
            }
        }


        //early exit so we only generate extra blocks the first time
        if (powerLevel > 1 || powerLevel % 2 == 0 )
            yield break;

        //if (!placedHill)
        //{
        //    Hex3 hillLocation = htm.GetRandomEmptyNeighbor(path[^2]);
        //    placedHill = htm.PlaceAtLocation(htm.GetRandomEmptyNeighbor(hillLocation), HexTileType.hill);
        //}

        int randomsToAdd = path.Count / 3;
        for (int i = 0; i < randomsToAdd; i++)
        {
            Vector3 randomLocation = path[HexTileManager.GetNextInt(0, path.Count)];
            int j = HexTileManager.GetNextInt(1, 10);
            //if (j == 1)
            //    htm.PlaceAtLocation(htm.GetRandomEmptyNeighbor(randomLocation), HexTileType.hill);
            //else 
            if (j == 2 || j == 3)
                htm.PlaceAtLocation(htm.GetRandomEmptyNeighbor(randomLocation), HexTileType.forest);
            else if (j == 4 || j == 5)
                htm.PlaceAtLocation(htm.GetRandomEmptyNeighbor(randomLocation), HexTileType.funkyTree);
            else
                htm.PlaceAtLocation(htm.GetRandomEmptyNeighbor(randomLocation), HexTileType.grass);
        }
    }

    private void PlaceEnemySpawner(Vector3 position)
    {
        int powerLevel = Mathf.Max(1, esm.SpawnPower - startingSpawnPower);
        this.spawner.DoSpawn(powerLevel, position);
    }


    private float timeLastNova;
    public void DoNova()
    {
        if(Time.timeSinceLevelLoad - timeLastNova < 1f)
            return;

        timeLastNova = Time.timeSinceLevelLoad;
        Vector3 position = this.transform.position;
        position.y = 0.75f;
        novaPool.Pull(position);
    }
    public EnemyCrystalData GetSaveData()
    {
        EnemyCrystalData crystalData = new EnemyCrystalData
        {
            position = this.transform.position,
            powerLevel = PowerLevel,
            startPowerLevel = startingSpawnPower
        };

        return crystalData;
    }

    public void LoadSaveData(EnemyCrystalData crystalData)
    {
        this.transform.position = crystalData.position;
        powerLevel = crystalData.powerLevel;
        startingSpawnPower = crystalData.startPowerLevel;

        if (powerLevel > 0)
        {
            EnemyIndicator.AddIndicatorObject(this.gameObject, IndicatorType.crystal);
            TryAddFogRevealer();
            crystalOff.SetActive(false);
            crystalOn.SetActive(true);
        }
    }
    public struct EnemyCrystalData
    {
        public Vector3 position;
        public int powerLevel;
        public int startPowerLevel;
    }
}