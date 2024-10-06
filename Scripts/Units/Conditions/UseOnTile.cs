using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Resources;

[CreateAssetMenu(menuName = "Hex/Conditions/Use Tile Type")]
public class UseOnTile : UnitCondition
{
    [SerializeField]
    private HexTileType requiredTileType;
    public override bool CanUse(GameObject gameObject)
    {
        HexTile tile = HexTileManager.GetHexTileAtLocation(gameObject.transform.position);
        return tile.TileType == requiredTileType;
    }
}
