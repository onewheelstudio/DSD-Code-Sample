using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using HexGame.Units;

public class HeadquarterBehavior : UnitBehavior
{
    public static event Action<HeadquarterBehavior> onHeadquartersCreated;
    public static event Action<HeadquarterBehavior> onHeadquartersDestroyed;

    public override void StartBehavior()
    {
        onHeadquartersCreated?.Invoke(this);
        _isFunctional = true;
    }

    public override void StopBehavior()
    {
        onHeadquartersDestroyed?.Invoke(this);
        _isFunctional = false;
    }
}
