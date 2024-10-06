using HexGame.Grid;
using HexGame.Resources;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Conditions/Exclude Placement Condition")]
public class ExcludeTileNearby : PlacementCondition
{
    public override bool CanBePlaced(Hex3 location)
    {
        if (range == 0)
            return true;

        HexTile tile;
        foreach (var hex3 in Hex3.GetNeighborsAtDistance(location, range))
        {
            tile = HexTileManager.GetHexTileAtLocation(hex3);
            if(tile == null)
                continue;

            if (tileTypes.Contains(tile.TileType))
                return false;
        }

        return true;
    }
}