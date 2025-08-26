using HexGame.Grid;
using HexGame.Resources;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Conditions/Use Near Tile Type")]
public class UseNearTile : UnitCondition
{
    [InfoBox("Requires ONE of these tiles with in range")]
    [SerializeField] private HashSet<HexTileType> tiles = new HashSet<HexTileType>();
    public HashSet<HexTileType> RequiredTiles => tiles;
    [SerializeField, Range(0,10)]
    private int range = 1;
    public int Range => range;
    public override bool CanUse(ResourceProductionBehavior rpb, Hex3 location)
    {
        HexTile tile;
        foreach (var neighbor in rpb.GetNeighborsInRange(range))
        {
            if (!HexTileManager.IsTileAtHexLocation(neighbor, out tile))
                continue;

            if (tiles.Contains(tile.TileType))
                return true;
        }

        return false;
    }
}
