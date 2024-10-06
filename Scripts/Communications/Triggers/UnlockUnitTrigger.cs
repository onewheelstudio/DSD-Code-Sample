using HexGame.Units;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Triggers/Unlock Unit")]
public class UnlockUnitTrigger : TriggerBase
{
    [SerializeField] private List<PlayerUnitType> unitsToUnlock;
    public static event Action<PlayerUnitType> unitUnlocked;
    [Button]
    public override void DoTrigger()
    {
        unitsToUnlock.ForEach(u => unitUnlocked?.Invoke(u));
    }
}
