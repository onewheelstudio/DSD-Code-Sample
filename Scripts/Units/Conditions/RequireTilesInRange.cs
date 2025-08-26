using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Conditions/Require Tiles In Range")]
public class RequireTilesInRange : PlacementCondition
{
    [SerializeField] private int numberOfRequiredTiles = 1;

    public override bool CanBePlaced(Hex3 location)
    {
        int count = 0;
        List<Hex3> locations = HexTileManager.GetHex3WithInRange(location, 0, range);
        List<HexTile> tiles = new();
        foreach (Hex3 hex3 in locations)
        {
            HexTile tile = HexTileManager.GetHexTileAtLocation(hex3);
            if (tile == null)
                continue;

            if (tileTypes.Contains(tile.TileType) 
                && !UnitManager.TryGetPlayerUnitAtLocation(hex3, out PlayerUnit playerUnit))
            {
                count++;
            }
        }

        return count >= numberOfRequiredTiles;
    }
}
