using System.Collections;
using System.Collections.Generic;
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

    private void OnEnable()
    {
        UnitManager.unitPlaced += (x) => buildingsBuilt++;   
        PlaceHolderTileBehavior.tileComplete += (x, y) => tilesPlaced++;
        UpgradeTile.upgradePurchased += (x) => upgradesUnlocked++;
        WorkerMenu.WorkerHired += () => workersHired++;
        SupplyShipBehavior.LoadSold += (x) => loadsSold++;
    }

    private void OnDisable()
    {
        UnitManager.unitPlaced -= (x) => buildingsBuilt++;
        PlaceHolderTileBehavior.tileComplete -= (x, y) => tilesPlaced++;
        UpgradeTile.upgradePurchased -= (x) => upgradesUnlocked++;
        WorkerMenu.WorkerHired -= () => workersHired++;
        SupplyShipBehavior.LoadSold -= (x) => loadsSold++;
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
}
