using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Resources;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Hex/Conditions/Productivity Condition")]
public class TileBasedProductivity : ProductivityCondition
{
    public HexTileType tileType;
    public override float ProductivityMultiplier(GameObject unit)
    {
        if (HexTileManager.GetHexTileAtLocation(unit.transform.position).TileType == tileType)
            return boost;
        else
            return 1f;
    }
}
