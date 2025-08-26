using HexGame.Grid;
using HexGame.Resources;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Conditions/Near Tile Productivity")]
public class NearTileProductivity : ProductivityCondition
{
    [SerializeField]
    private HexTileType requiredTileType;
    [SerializeField, Range(0, 10)]
    private int range = 1;
    [NonSerialized] private List<Hex3> neighbors = new();

    public override float ProductivityMultiplier(ResourceProductionBehavior rpb)
    {
        if (rpb == null)
            return 1f;

        if(range == 0)
        {
            HexTile hexTile = HexTileManager.GetHexTileAtLocation(rpb.Position);
            if (hexTile != null && hexTile.TileType == requiredTileType)
                return boost;
        }

        neighbors.Clear();
        Hex3.GetNeighborsAtDistance(rpb.Position, range, ref neighbors);
        HexTile tile;
        foreach (var hex3 in neighbors)
        {
            tile = HexTileManager.GetHexTileAtLocation(hex3);
            if (tile != null && tile.TileType == requiredTileType)
                return boost;
        }

        return 1f;
    }
}
