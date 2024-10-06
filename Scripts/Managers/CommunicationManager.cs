using HexGame.Resources;
using HexGame.Units;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class CommunicationManager : MonoBehaviour
{
    [Header("Markers")]
    [SerializeField] private GameObject redMarkerPrefab;

    [Header("Getting Started")]
    [SerializeField] private CommunicationBase startTutorial;

    [Header("Start Day Night Cycle")]
    [SerializeField] private CommunicationBase startDayNight;
    [SerializeField] private CommunicationBase newCrystalPoweredUp;
    [SerializeField] private CommunicationBase firstEnemyLootDropped;
    [SerializeField] private CommunicationBase createLandWithResource;
    private EnemyCrystalManager ecm;
    private int terreneCollected = 0;

    [Header("Supply Ship")]
    [SerializeField] private CommunicationBase buildSupplyShip;
    private bool buildShipPlayed = false;

    [Header("Warnings")]
    [SerializeField] private CommunicationBase warningEnemySpawning;


    [Header("Game Tips")]
    [SerializeField] private TipCommunication buildingDamaged;
    [SerializeField] private TipCommunication addingConnections;
    private bool addingConnectionsShown = false;
    [SerializeField] private TipCommunication shuttleRange;
    private bool shuttleRangeShown = false;
    [SerializeField] private TipCommunication workerNeeded;
    private int workersNeededLastShownOn = 0;

    [Header("Tech Tree & Credits")]
    [SerializeField] private CommunicationBase firstTechCredit;

    [Header("Worker Menu")]
    [SerializeField] private CommunicationBase workerMenu;
    private bool workerMenuShown = false;

    private void Awake()
    {
        Time.timeScale = 1f; //why is this needed?
        ecm = FindFirstObjectByType<EnemyCrystalManager>();
    }

    private void OnEnable()
    {
        StateOfTheGame.GameStarted += StartCommunciations;
        //DayNightManager.transitionToNight += StartEnemySpawnCycle;
        BuildingTutorialComplete.buildingTutorialComplete += StartEnemySpawnCycle;
        EnemyCrystalBehavior.newCrystalPoweredUp += CrystalPoweringUp;
        DayNightManager.toggleNight += TransitioningToNight;
        EnemyLootDrop.enemyLootDropped += EnemyLootDropped;
        CollectionBehavior.collected += CollectedTerrene;
        //PlayerUnit.playerUnitDamaged += PlayerUnitDamaged;
        TilesBuiltTrigger.tilesBuilt += TilesBuilt;
        HexTechTree.firstTechCreditCollected += FirstTechCreditCollected;
        UnitManager.unitPlaced += UnitAdded;
        DayNightManager.toggleDay += CheckWorkers;
        WorkerManager.workerStateChanged += CheckWorkers;
    }

    private void OnDisable()
    {
        StateOfTheGame.GameStarted -= StartCommunciations;
        //DayNightManager.transitionToNight -= StartEnemySpawnCycle;
        BuildingTutorialComplete.buildingTutorialComplete -= StartEnemySpawnCycle;
        EnemyCrystalBehavior.newCrystalPoweredUp -= CrystalPoweringUp;
        DayNightManager.toggleNight -= TransitioningToNight;
        TilesBuiltTrigger.tilesBuilt -= TilesBuilt;
        EnemyLootDrop.enemyLootDropped -= EnemyLootDropped;
        CollectionBehavior.collected -= CollectedTerrene;
        PlayerUnit.playerUnitDamaged -= PlayerUnitDamaged;
        HexTechTree.firstTechCreditCollected -= FirstTechCreditCollected;
        UnitManager.unitPlaced -= UnitAdded;
        DayNightManager.toggleDay -= CheckWorkers;
        WorkerManager.workerStateChanged -= CheckWorkers;
        //some events are unsubscribed in the functions
    }

    private void CheckWorkers(int dayNumber)
    {
        if(dayNumber > workersNeededLastShownOn + 2 && WorkerManager.workersNeeded > 0)
        {
            GameTipsWindow.AddTip(workerNeeded);
            workersNeededLastShownOn = dayNumber;
        }

        if(WorkerManager.availableWorkers <= 3 && !workerMenuShown)
        {
            CommunicationMenu.AddCommunication(workerMenu);
            workerMenuShown = true;
        }
    }

    private void CheckWorkers()
    {
        if (WorkerManager.availableWorkers <= 3 && !workerMenuShown)
        {
            CommunicationMenu.AddCommunication(workerMenu);
            workerMenuShown = true;
        }
    }

    #region Start of Game
    private void StartCommunciations()
    {
        CommunicationMenu.AddCommunication(startTutorial);
    }
    #endregion

    #region Start Day Night and Enemy Spawning
    private void StartEnemySpawnCycle()
    {
        //DayNightManager.transitionToNight -= StartEnemySpawnCycle;
        BuildingTutorialComplete.buildingTutorialComplete -= StartEnemySpawnCycle;
        CommunicationMenu.AddCommunication(startDayNight, false);
    }

    private void CrystalPoweringUp(EnemyCrystalBehavior behavior)
    {
        //other message is shown when first crystal is powered up
        if(ecm.NumberOfPoweredCrystals() > 1)
            CommunicationMenu.AddCommunication(newCrystalPoweredUp, false);
        Instantiate(redMarkerPrefab, behavior.transform.position, Quaternion.identity);
    } 
    private void TransitioningToDay(int dayNumber)
    {
        if(buildShipPlayed && dayNumber > 3)
        {
            if (!addingConnectionsShown)
            {
                GameTipsWindow.AddTip(addingConnections);
                addingConnectionsShown = true;
            }

            if (!shuttleRangeShown)
            {
                GameTipsWindow.AddTip(shuttleRange);
                shuttleRangeShown = true;
            }
        }
    }
    private void UnitAdded(Unit unit)
    {
        if(unit is PlayerUnit playerUnit && playerUnit.unitType == PlayerUnitType.supplyShip)
        {
            buildShipPlayed = true;
            UnitManager.unitPlaced -= UnitAdded;
        }
    }

    private void TransitioningToNight(int dayNumber)
    {
        if (dayNumber == 2)
            CommunicationMenu.AddCommunication(warningEnemySpawning, false);
    }

    private void EnemyLootDropped(EnemyLootDrop drop, GameObject loot)
    {
        EnemyLootDrop.enemyLootDropped -= EnemyLootDropped;
        CommunicationMenu.AddCommunication(firstEnemyLootDropped);
    }

    private void CollectedTerrene(ResourcePickup resourcePickup)
    {
        if (resourcePickup.resourceType != ResourceType.Terrene)
            return;

        terreneCollected += resourcePickup.amount;

        if(terreneCollected >= 5)
        {
            CommunicationMenu.AddCommunication(createLandWithResource);
            CollectionBehavior.collected -= CollectedTerrene;

        }
    }
    #endregion

    private void TilesBuilt()
    {
        CommunicationMenu.AddCommunication(buildSupplyShip);
        DayNightManager.toggleDay += TransitioningToDay;
    }

    #region Building Damaged
    [Button]
    private void PlayerUnitDamaged(PlayerUnit unit)
    {
        if (unit.unitType == PlayerUnitType.infantry)
            return;

        GameTipsWindow.AddTip(buildingDamaged);
        PlayerUnit.playerUnitDamaged -= PlayerUnitDamaged;
    }
    #endregion

    private void FirstTechCreditCollected()
    {
        CommunicationMenu.AddCommunication(firstTechCredit);
        HexTechTree.firstTechCreditCollected -= FirstTechCreditCollected;

    }
}
