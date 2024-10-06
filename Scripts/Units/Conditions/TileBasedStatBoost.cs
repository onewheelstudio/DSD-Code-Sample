using HexGame.Grid;
using HexGame.Resources;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Conditions/Tile Based Stat Boost")]
public class TileBasedStatBoost : StatBoost
{
    [SerializeField] private HexTileType tileType;
    [SerializeField, Range(0, 3)]
    protected int range = 0;
    public override int Boost(GameObject unit)
    {
        if(range == 0)
        {
            if (HexTileManager.GetHexTileAtLocation(unit.transform.position).TileType == tileType)
                return boost;
            else
                return 0;
        }
        else
        {
            List<Hex3> neighbors = HexTileManager.GetHex3WithInRange(unit.transform.position, 1, range);
            foreach (var hex3 in neighbors)
            {
                HexTile tile = HexTileManager.GetHexTileAtLocation(hex3);
                if (tile == null)
                    continue;
                else if (tile.TileType == tileType)
                    return boost;
            }
        }

        return 0;
    }
}
