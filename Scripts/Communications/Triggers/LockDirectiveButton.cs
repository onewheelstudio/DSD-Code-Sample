using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Triggers/LockDirectiveButton")]
public class LockDirectiveButton : TriggerBase
{
    public static event Action lockDirectiveButton;
    public override void DoTrigger()
    {
        lockDirectiveButton?.Invoke();
    }
}

