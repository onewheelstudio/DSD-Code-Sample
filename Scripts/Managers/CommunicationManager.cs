using HexGame.Units;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

public class CommunicationManager : MonoBehaviour, ISaveData
{
    [SerializeField] private GameSettings gameSettings;

    [Header("Markers")]
    [SerializeField] private GameObject redMarkerPrefab;

    [Header("Getting Started")]
    [SerializeField] private CommunicationBase startTutorial;
    private bool startTutorialPlayed = false;

    [Header("Start Day Night Cycle")]
    [SerializeField] private CommunicationBase startDayNight;
    [SerializeField] private CommunicationBase newCrystalPoweredUp;
    [SerializeField] private CommunicationBase firstEnemyLootDropped;
    [SerializeField] private CommunicationBase tutorialSkipped;
    private bool lootDroppedShown;
    private EnemyCrystalManager ecm;
    private int terreneCollected = 0;

    [Header("Supply Ship")]
    [SerializeField] private CommunicationBase buildSupplyShip;
    private bool buildShipPlayed = false;

    [Header("Warnings")]
    [SerializeField] private CommunicationBase warningEnemySpawning;

    [Header("Game Tips")]
    [SerializeField] private TipCommunication buildingDamaged;
    [SerializeField] private TipCommunication shuttleRange;
    private bool shuttleRangeShown = false;
    [SerializeField] private TipCommunication workerNeeded;
    private int workersNeededLastShownOn = 0;

    [Header("Tech Tree & Credits")]
    [SerializeField] private CommunicationBase firstTechCredit;

    [Header("Worker Menu")]
    [SerializeField] private CommunicationBase workerMenu;
    private bool workerMenuShown = false;

    [Header("Special Projects")]
    [SerializeField] private CommunicationBase buildOrbitalLift;
    private bool orbitalLiftShown = false;


    private void Awake()
    {
        Time.timeScale = 1f; //why is this needed?
        ecm = FindFirstObjectByType<EnemyCrystalManager>();
        RegisterDataSaving();
    }

    private void OnEnable()
    {
        StateOfTheGame.GameStarted += StartCommunciations;
        StateOfTheGame.TutorialSkipped += TutorialSkipped;
        BuildingTutorialComplete.buildingTutorialComplete += StartEnemySpawnCycle;
        EnemyCrystalBehavior.newCrystalPoweredUp += CrystalPoweringUp;
        DayNightManager.toggleNight += TransitioningToNight;
        LootManager.lootAdded += EnemyLootDropped;
        TilesBuiltTrigger.tilesBuilt += TilesBuilt;
        HexTechTree.firstTechCreditCollected += FirstTechCreditCollected;
        UnitManager.unitPlaced += UnitAdded;

        DayNightManager.toggleDay += StartSpaceLasers;
        DayNightManager.toggleDay += CheckWorkers;
        DayNightManager.toggleDay += StartSpecialProjects;

        WorkerManager.workerStateChanged += CheckWorkers;

        CollectionBehavior.collected += CollectedTerrene;
    }

    private void OnDisable()
    {
        StateOfTheGame.GameStarted -= StartCommunciations;
        StateOfTheGame.TutorialSkipped -= TutorialSkipped;
        BuildingTutorialComplete.buildingTutorialComplete -= StartEnemySpawnCycle;
        EnemyCrystalBehavior.newCrystalPoweredUp -= CrystalPoweringUp;
        DayNightManager.toggleNight -= TransitioningToNight;
        TilesBuiltTrigger.tilesBuilt -= TilesBuilt;
        LootManager.lootAdded -= EnemyLootDropped;
        PlayerUnit.playerUnitDamaged -= PlayerUnitDamaged;
        HexTechTree.firstTechCreditCollected -= FirstTechCreditCollected;
        UnitManager.unitPlaced -= UnitAdded;

        DayNightManager.toggleDay -= StartSpaceLasers;
        DayNightManager.toggleDay -= CheckWorkers;
        DayNightManager.toggleDay -= StartSpecialProjects;
        WorkerManager.workerStateChanged -= CheckWorkers;
        //some events are unsubscribed in the functions

        CollectionBehavior.collected -= CollectedTerrene;
    }


    private void TutorialSkipped()
    {
        StateOfTheGame.GameStarted -= StartCommunciations;
        BuildingTutorialComplete.buildingTutorialComplete -= StartEnemySpawnCycle;
        TilesBuiltTrigger.tilesBuilt -= TilesBuilt;
        LootManager.lootAdded -= EnemyLootDropped;
        PlayerUnit.playerUnitDamaged -= PlayerUnitDamaged;
        HexTechTree.firstTechCreditCollected -= FirstTechCreditCollected;
        UnitManager.unitPlaced -= UnitAdded;
        DayNightManager.toggleDay -= CheckWorkers;
        WorkerManager.workerStateChanged -= CheckWorkers;
    }


    [Button]
    private void StartSpecialProjects(int dayNumber)
    {
        if(gameSettings.IsDemo)
            return;

        if(dayNumber > 0 && ReputationManager.Reputation > SpecialProjectManager.RepToBuildLift && !orbitalLiftShown)
        {
            CommunicationMenu.AddCommunication(buildOrbitalLift);
            orbitalLiftShown = true;
        }
    }

    private void StartSpaceLasers(int dayNumber)
    {
        //CommunicationMenu.AddCommunication(spaceLasersStart);
        DayNightManager.toggleDay -= StartSpaceLasers;
    }

    private void CheckWorkers(int dayNumber)
    {
        if(dayNumber > workersNeededLastShownOn + 2 && WorkerManager.workersNeeded > 0)
        {
            GameTipsWindow.AddTip(workerNeeded);
            workersNeededLastShownOn = dayNumber;
        }

        if(WorkerManager.AvailableWorkers <= 3 && !workerMenuShown)
        {
            CommunicationMenu.AddCommunication(workerMenu);
            workerMenuShown = true;
        }
    }

    private void CheckWorkers()
    {
        if (SaveLoadManager.Loading)
            return;

        if (WorkerManager.AvailableWorkers <= 3 && !workerMenuShown && DayNightManager.DayNumber > 1)
        {
            CommunicationMenu.AddCommunication(workerMenu);
            workerMenuShown = true;
        }
    }

    #region Start of Game
    private void StartCommunciations()
    {
        if (startTutorialPlayed || SaveLoadManager.Loading || StateOfTheGame.tutorialSkipped)
            return;

        CommunicationMenu.AddCommunication(startTutorial);
        startTutorialPlayed = true;
    }
    #endregion

    #region Start Day Night and Enemy Spawning
    private void StartEnemySpawnCycle()
    {
        BuildingTutorialComplete.buildingTutorialComplete -= StartEnemySpawnCycle;
        //communication is getting played from EnemySpawnManager
        //CommunicationMenu.AddCommunication(startDayNight, false);
    }

    private void CrystalPoweringUp(EnemyCrystalBehavior behavior)
    {
        //other message is shown when first crystal is powered up
        if(ecm.NumberOfPoweredCrystals() > 1)
            StartCoroutine(PlayCrystalPoweredUp());
        else if (StateOfTheGame.tutorialSkipped && ecm.NumberOfPoweredCrystals() == 1)
            CommunicationMenu.AddCommunication(tutorialSkipped, false);
        Instantiate(redMarkerPrefab, behavior.transform.position, Quaternion.identity);
    } 

    private IEnumerator PlayCrystalPoweredUp()
    {
        yield return new WaitUntil(() => DayNightManager.isDay && DayNightManager.NormalizedTime > 0.75f);
        CommunicationMenu.AddCommunication(newCrystalPoweredUp, false);
    }

    private void TransitioningToDay(int dayNumber)
    {
        if (buildShipPlayed && dayNumber > 3)
        {
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

    private void EnemyLootDropped(LootManager.LootData loot)
    {
        LootManager.lootAdded -= EnemyLootDropped;
        if (terreneCollected > 0 || lootDroppedShown)
            return;
        CommunicationMenu.AddCommunication(firstEnemyLootDropped);
        lootDroppedShown = true;
    }

    private void CollectedTerrene(LootManager.LootData resourcePickup)
    {
        terreneCollected += 1;
    }
    #endregion

    private void TilesBuilt()
    {
        if(!buildShipPlayed)
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

    private const string COMM_SAVE_DATA = "CommData";

    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this);
    }

    public void Save(string savePath, ES3Writer writer)
    {
        CommData commData = new CommData()
        {
            startTutorialPlayed = this.startTutorialPlayed,
            terreneCollected = this.terreneCollected,
            buildShipPlayed = this.buildShipPlayed,
            addingConnectionsShown = true,
            shuttleRangeShown = this.shuttleRangeShown,
            workersNeededLastShownOn = this.workersNeededLastShownOn,
            workerMenuShown = this.workerMenuShown,
            lootDroppedShown = this.lootDroppedShown,
            orbitalLiftShown = this.orbitalLiftShown
        };

        writer.Write<CommData>(COMM_SAVE_DATA, commData);
    }

    public IEnumerator Load(string loadPath, System.Action<string> postUpdateMessage)
    {
        if(ES3.KeyExists(COMM_SAVE_DATA, loadPath))
        {
            CommData commData = ES3.Load<CommData>(COMM_SAVE_DATA, loadPath);
            this.startTutorialPlayed = commData.startTutorialPlayed;
            this.terreneCollected = commData.terreneCollected;
            this.buildShipPlayed = commData.buildShipPlayed;
            this.shuttleRangeShown = commData.shuttleRangeShown;
            this.workersNeededLastShownOn = commData.workersNeededLastShownOn;
            this.workerMenuShown = commData.workerMenuShown;
            this.lootDroppedShown = commData.lootDroppedShown;
            this.orbitalLiftShown = commData.orbitalLiftShown;

            postUpdateMessage?.Invoke("Retrieving Communication Logs");
        }

        yield return null;
    }

    public struct CommData
    {
        public bool startTutorialPlayed;
        public int terreneCollected;
        public bool buildShipPlayed;
        public bool addingConnectionsShown;
        public bool shuttleRangeShown;
        public int workersNeededLastShownOn;
        public bool workerMenuShown;
        public bool lootDroppedShown;
        public bool orbitalLiftShown;
    }
}
