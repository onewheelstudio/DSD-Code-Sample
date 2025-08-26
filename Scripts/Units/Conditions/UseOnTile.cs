using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Resources;
using HexGame.Grid;

[CreateAssetMenu(menuName = "Hex/Conditions/Use Tile Type")]
public class UseOnTile : UnitCondition
{
    [SerializeField]
    private HexTileType requiredTileType;
    public override bool CanUse(ResourceProductionBehavior rpb, Hex3 location)
    {
        HexTile tile = HexTileManager.GetHexTileAtLocation(location);
        return tile.TileType == requiredTileType;
    }
}
