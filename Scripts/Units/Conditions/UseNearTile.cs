using HexGame.Grid;
using HexGame.Resources;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Conditions/Use Near Tile Type")]
public class UseNearTile : UnitCondition
{
    [SerializeField]
    private HexTileType requiredTileType;
    public HexTileType RequiredTileType => requiredTileType;
    [SerializeField, Range(0,10)]
    private int range = 1;
    public int Range => range;
    public override bool CanUse(GameObject gameObject)
    {
        List<Hex3> neighbors = Hex3.GetNeighborsAtDistance(gameObject.transform.position, range);
        HexTile tile;
        foreach (var hex3 in neighbors)
        {
            tile = HexTileManager.GetHexTileAtLocation(hex3);
            if(tile != null && tile.TileType == requiredTileType) 
                return true;
        }
        
        return false;
    }
}
