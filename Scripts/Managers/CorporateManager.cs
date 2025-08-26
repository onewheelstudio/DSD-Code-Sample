using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class CorporateManager : MonoBehaviour
{
    [SerializeField] private int minimumInfantry = 2;
    private UnitManager unitManager;
    [SerializeField] private List<CommunicationBase> reinforcementWarnings = new List<CommunicationBase>();
    private int warningIndex = 0;

    private void Awake()
    {
        unitManager = FindObjectOfType<UnitManager>();
    }

    private void OnEnable()
    {
        DayNightManager.toggleDay += CheckInfantry;
    }

    private void OnDisable()
    {
        DayNightManager.toggleDay -= CheckInfantry;
    }

    [Button]
    private void CheckInfantry(int dayNumber)
    {
        if(dayNumber > 20 || warningIndex > reinforcementWarnings.Count)
        {
            DayNightManager.toggleDay -= CheckInfantry;
            return;
        }

        int infantryNeeded = minimumInfantry - UnitManager.GetPlayerUnitByType(PlayerUnitType.infantry).Count;

        if (infantryNeeded <= 0)
            return;

        List<PlayerUnit> hqs = UnitManager.GetPlayerUnitByType(HexGame.Units.PlayerUnitType.hq);
        if(hqs == null || hqs.Count == 0)
            return;

        List<Hex3> emptyLocations = HexTileManager.GetHex3WithInRange(hqs[0].transform.position.ToHex3(),1, 4);
        if(emptyLocations.Count == 0)
            return;
        GameObject newUnit = null;
        for (int i = 0; i < infantryNeeded; i++)
        {
            Hex3 target = Hex3.Zero;
            bool foundLocation = false;
            foreach (Hex3 hex in emptyLocations)
            {
                if (UnitManager.PlayerUnitAtLocation(hex) != null)
                    continue;

                HexTile tile = HexTileManager.GetHexTileAtLocation(hex);
                if(tile == null)
                    continue;
                if(tile.TileType != HexTileType.grass 
                    && tile.TileType != HexTileType.forest
                    && tile.TileType != HexTileType.funkyTree
                    && tile.TileType != HexTileType.aspen)
                    continue;
             
                target = hex;
                foundLocation = true;
                break;
            }

            if(!foundLocation)
                return;

            newUnit = unitManager.InstantiateUnitByType(PlayerUnitType.infantry, target);
        }

        MessagePanel.ShowMessage($"The corporation sent reinforcements - {infantryNeeded} additional infantry.", newUnit);
        if(warningIndex < reinforcementWarnings.Count)
        {
            CommunicationMenu.AddCommunication(reinforcementWarnings[warningIndex], false);
            warningIndex++;
        }
    }
}
