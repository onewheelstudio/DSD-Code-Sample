using HexGame.Resources;
using HexGame.Units;
using OWS.ObjectPooling;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public class EnemyLootDrop : UnitBehavior
{
    [Range(0f, 1f)]
    [SerializeField]
    private float chanceToSpawnLoot = 0.25f;
    private static bool initialAmountDropped = false;
    private EnemyUnit enemyUnit;
    public static event Action<Vector3> requestLootDrop;

    private void Awake()
    {
        enemyUnit = GetComponent<EnemyUnit>();
        enemyUnit ??= GetComponent<EnemySubUnit>().ParentUnit;
        enemyUnit.ThisUnitDied += SpawnLoot;
    }

    public override void StartBehavior()
    {
        isFunctional = true;
    }

    public override void StopBehavior()
    {
        isFunctional = false;
    }

    private void OnDestroy()
    {
        enemyUnit.ThisUnitDied -= SpawnLoot;
        initialAmountDropped = false;
    }

    private void SpawnLoot()
    {
        if (!CanSpawnLoot())
            return;

        requestLootDrop?.Invoke(this.transform.position);
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
