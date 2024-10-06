using HexGame.Grid;
using HexGame.Resources;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Conditions/Near Tile Productivity")]
public class NearTileProductivity : ProductivityCondition
{
    [SerializeField]
    private HexTileType requiredTileType;
    [SerializeField, Range(0, 10)]
    private int range = 1;

    public override float ProductivityMultiplier(GameObject unit)
    {
        if(range == 0)
        {
            if (HexTileManager.GetHexTileAtLocation(unit.transform.position).TileType == requiredTileType)
                return boost;
        }

        List<Hex3> neighbors = Hex3.GetNeighborsAtDistance(unit.transform.position, range);
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
