using HexGame.Resources;
using HexGame.Units;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GamePlayStats : MonoBehaviour
{
    public float TimePlayed => Time.realtimeSinceStartup / 60f;

    public int BuildingsBuilt => buildingsBuilt;
    private int buildingsBuilt;
    
    public int UpgradesUnlocked => upgradesUnlocked;
    private int upgradesUnlocked;
    
    public int DaysPlayed => DayNightManager.DayNumber;

    public int TilesPlaced => tilesPlaced;
    private int tilesPlaced;

    public int WorkersHired => workersHired;
    private int workersHired;

    public int LoadsSold => loadsSold;
    private int loadsSold;

    private int tilesRevealed = 0;
    public int TilesRevealed => tilesRevealed;


    private List<DirectiveQuest> questsAdded = new();
    private List<DirectiveBase> directivesAdded = new();
    public string QuestsCompleted => GetQuestString();
    public string DirectivesCompleted => GetDirectiveString();

    private Dictionary<PlayerUnitType, int> playerUnitsCreated = new();
    public string PlayerUnitsCreated => GetPlayerUnitString();

    public string AdditionalStats => GetAdditionalStats();

    private int enemiesKilled = 0;
    private int crystalsPoweredUp = 0;

    private int connectionsAdded = 0;
    public int ConnectionsAdded => connectionsAdded;

    private int connectionsRemoved = 0;
    public int ConnectionsRemoved => connectionsRemoved;

    private bool landGenerationStarted = false;
    public bool LandGenerationStarted => landGenerationStarted;
    private bool landGenerationFinished = false;
    public bool LandGenerationFinished => landGenerationFinished;

    private void OnEnable()
    {
        UnitManager.unitPlaced += (x) => buildingsBuilt++;   
        PlaceHolderTileBehavior.tileComplete += (x, y) => tilesPlaced++;
        UpgradeTile.upgradePurchased += (x) => upgradesUnlocked++;
        WorkerMenu.WorkerHired += () => workersHired++;
        SupplyShipBehavior.LoadShipped += LoadSold;

        DirectiveMenu.QuestAdded += QuestAdded;
        DirectiveMenu.DirectiveAdded += DirectiveAdded;

        PlayerUnit.unitCreated += UnitCreated;

        EnemyUnit.enemyUnitKilled += EnemyKilled;
        EnemyCrystalBehavior.newCrystalPoweredUp += EnemyCrystalPoweredUp;

        FogGroundTile.TileRevealed += TileReveled;

        UnitStorageBehavior.connectionAdded += ConnectionAdded;
        UnitStorageBehavior.connectionRemoved += ConnectionRemoved;

        LandmassGenerator.generationStarted += () => landGenerationStarted = true;
        LandmassGenerator.generationComplete += () => landGenerationFinished = true;
    }

    private void LoadSold(SupplyShipBehavior behavior, RequestType type, List<ResourceAmount> resourceType)
    {
        if (type == RequestType.sell)
            loadsSold++;
    }

    private void OnDisable()
    {
        UnitManager.unitPlaced -= (x) => buildingsBuilt++;
        PlaceHolderTileBehavior.tileComplete -= (x, y) => tilesPlaced++;
        UpgradeTile.upgradePurchased -= (x) => upgradesUnlocked++;
        WorkerMenu.WorkerHired -= () => workersHired++;
        SupplyShipBehavior.LoadShipped -= LoadSold;

        DirectiveMenu.QuestAdded -= QuestAdded;
        DirectiveMenu.DirectiveAdded -= DirectiveAdded;

        PlayerUnit.unitCreated -= UnitCreated;

        EnemyUnit.enemyUnitKilled -= EnemyKilled;
        EnemyCrystalBehavior.newCrystalPoweredUp -= EnemyCrystalPoweredUp;

        FogGroundTile.TileRevealed -= TileReveled;

        UnitStorageBehavior.connectionAdded -= ConnectionAdded;
        UnitStorageBehavior.connectionRemoved -= ConnectionRemoved;
    }

    private void TileReveled(FogGroundTile tile)
    {
        tilesRevealed++;
    }

    private void UnitCreated(Unit unit)
    {
        if (unit is PlayerUnit playerUnit)
        {
            if(playerUnit.unitType == PlayerUnitType.cargoShuttle || playerUnit.unitType == PlayerUnitType.buildingSpot)
                return;

            if (playerUnitsCreated.TryGetValue(playerUnit.unitType, out int count))
            {
                playerUnitsCreated[playerUnit.unitType] = count + 1;
            }
            else
            {
                playerUnitsCreated.Add(playerUnit.unitType, 1);
            }
        }
    }

    private string GetPlayerUnitString()
    {
        string playerUnitString = "";
        foreach (var unit in playerUnitsCreated)
        {
            playerUnitString += $"{unit.Key} : {unit.Value}\n";
        }

        return playerUnitString;
    }

    private void QuestAdded(DirectiveQuest quest)
    {
        if (questsAdded.Contains(quest))
            return;

        questsAdded.Add(quest);
    }

    private string GetQuestString()
    {
        string questString = "";
        foreach (var quest in questsAdded)
        {
            questString += quest.name + $" Completed: {quest.IsComplete().All(q => q == true)} \n";
        }

        return questString;
    }

    private void DirectiveAdded(DirectiveBase directive)
    {
        if (directivesAdded.Contains(directive))
            return;

        directivesAdded.Add(directive);
    }

    private string GetDirectiveString()
    {
        string directiveString = "";
        foreach (var directive in directivesAdded)
        {
            directiveString += directive.name + $" Completed: {directive.IsComplete().All(d => d == true)} \n";
        }

        return directiveString;
    }

    private void ConnectionRemoved(UnitStorageBehavior behavior1, UnitStorageBehavior behavior2)
    {
        connectionsAdded++;
    }

    private void ConnectionAdded(UnitStorageBehavior behavior1, UnitStorageBehavior behavior2)
    {
        connectionsRemoved++;
    }

    public override string ToString()
    {
        return $"Length of Play: {this.TimePlayed}" +
                 $"\nBuildings Added: {this.BuildingsBuilt}" +
                 $"\nUpgrades Unlocked: {this.UpgradesUnlocked}" +
                 $"\nDays Played: {this.DaysPlayed}" +
                 $"\nTiles Placed: {this.TilesPlaced}" +
                 $"\nWorkers Hired: {this.WorkersHired}" +
                 $"\nLoads Sold: {this.LoadsSold}";
    }

    private string GetAdditionalStats()
    {
        string additionalStats = "";
        additionalStats += $"Credits: {HexTechTree.TotalTechCreditsCollected}\n";
        additionalStats += $"Rep: {ReputationManager.Reputation}\n";
        additionalStats += $"Enemies Killed: {enemiesKilled}\n";
        additionalStats += $"Crystals Powered Up: {crystalsPoweredUp}\n";
        additionalStats += $"Tutorial Skipped: {StateOfTheGame.tutorialSkipped}\n";
        additionalStats += $"Tiles Revealed: {TilesRevealed}\n";
        additionalStats += $"Connections Added: {ConnectionsAdded}\n";
        additionalStats += $"Connections Removed: {ConnectionsRemoved}\n";
        additionalStats += $"Seed: {FindFirstObjectByType<HexTileManager>().RandomizeSeed}\n";
        if(FindFirstObjectByType<SaveLoadManager>() != null) //can happen when testing from Unity
            additionalStats += $"Loaded Game: {SaveLoadManager.loadedGame}\n";
        if(ES3.FileExists(GameConstants.preferencesPath))
            additionalStats += $"Voice Volume: {ES3.Load<float>("voiceVolume", GameConstants.preferencesPath, 1f)}\n";
        additionalStats += $"Land Generation: {landGenerationStarted} : {landGenerationFinished}";


        return additionalStats;
    }

    private void EnemyCrystalPoweredUp(EnemyCrystalBehavior behavior)
    {
        this.crystalsPoweredUp++;
    }

    private void EnemyKilled(EnemyUnit unit)
    {
        this.enemiesKilled++;
    }
}
