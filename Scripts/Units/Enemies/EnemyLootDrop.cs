using HexGame.Resources;
using HexGame.Units;
using OWS.ObjectPooling;
using System;
using System.Linq;
using UnityEngine;

public class EnemyLootDrop : UnitBehavior
{

    [Range(0f, 1f)]
    [SerializeField]
    private float chanceToSpawnLoot = 0.25f;
    [SerializeField]
    private GameObject lootPrefab;
    private static ObjectPool<PoolObject> lootPool;
    public static event Action<EnemyLootDrop, GameObject> enemyLootDropped;
    private static bool initialAmountDropped = false;

    private void Awake()
    {
        if (lootPool == null)
            lootPool = new ObjectPool<PoolObject>(lootPrefab);
    }

    public override void StartBehavior()
    {
        isFunctional = true;
    }

    public override void StopBehavior()
    {
        isFunctional = false;
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
            SpawnLoot();
    }

    private void SpawnLoot()
    {
        if (!CanSpawnLoot())
            return;

        GameObject loot = lootPool.Pull(new Vector3(this.transform.position.x, 0.25f, this.transform.position.z), this.transform.rotation).gameObject;
        enemyLootDropped?.Invoke(this, loot);
    }

    private bool CanSpawnLoot()
    {

        if (!StateOfTheGame.gameStarted)
            return false;

        if (initialAmountDropped)
            return HexTileManager.GetNextInt(0, 100) < chanceToSpawnLoot * 100;

        //drop 10 units of terrene to allow easy completion of the directive
        int amountCollected = PlayerResources.questResources.FirstOrDefault(x => x.type == ResourceType.Terrene).amount;
        int amountDropped = ResourceUnit.Count / 2;

        if(amountCollected + amountDropped >= 10)
            initialAmountDropped = true;

        return !initialAmountDropped;
    }
}
