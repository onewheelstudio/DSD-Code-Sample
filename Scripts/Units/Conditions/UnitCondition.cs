using HexGame.Grid;
using HexGame.Resources;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UnitCondition : SerializedScriptableObject, IUseCondition
{
    public abstract bool CanUse(ResourceProductionBehavior rpb, Hex3 location);
}



