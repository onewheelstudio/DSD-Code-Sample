using HexGame.Grid;
using HexGame.Resources;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlacementCondition : ScriptableObject
{
    [InfoBox("Requires ONE of these tiles with in range")]
    public List<HexTileType> tileTypes;
    public int range = 1;

    public abstract bool CanBePlaced(Hex3 location);
}


