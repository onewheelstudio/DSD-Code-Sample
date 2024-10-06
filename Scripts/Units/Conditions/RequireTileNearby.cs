using HexGame.Grid;
using HexGame.Resources;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Conditions/Require Placement Condition")]
public class RequireTileNearby : PlacementCondition
{
    public override bool CanBePlaced(Hex3 location)
    {
        if (range == 0)
            return true;

        HexTile tile;
        foreach (var hex3 in Hex3.GetNeighborsAtDistance(location, range))
        {
            tile = HexTileManager.GetHexTileAtLocation(hex3);
            if (tile == null)
                continue;

            if (tileTypes.Contains(tile.TileType))
                return true;
        }

        return false;
    }
}
