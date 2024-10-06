using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Triggers/LockTechTree")]
public class LockTechTree : TriggerBase
{
    public static event Action lockTechTree;
    public override void DoTrigger()
    {
        lockTechTree?.Invoke();
    }
}
