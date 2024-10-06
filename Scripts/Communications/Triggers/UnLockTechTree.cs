using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Triggers/UnLockTechTree")]
public class UnLockTechTree : TriggerBase
{
    public static event Action unLockTechTree;
    public override void DoTrigger()
    {
        unLockTechTree?.Invoke();
    }
}
