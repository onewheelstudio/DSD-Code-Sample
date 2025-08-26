using HexGame.Grid;
using HexGame.Resources;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUseCondition
{
    bool CanUse(ResourceProductionBehavior rpb, Hex3 location);
}
