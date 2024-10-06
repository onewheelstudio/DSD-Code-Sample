using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UnitCondition : ScriptableObject, IUseCondition
{
    public abstract bool CanUse(GameObject gameObject);
}



