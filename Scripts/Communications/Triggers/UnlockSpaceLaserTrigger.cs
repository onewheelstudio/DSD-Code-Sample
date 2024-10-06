using System;
using UnityEngine;

[CreateAssetMenu(fileName = "UnlockSpaceLaserTrigger", menuName = "Hex/Triggers/Unlock SpaceLaser Trigger")]
public class UnlockSpaceLaserTrigger : TriggerBase
{
    public static event Action UnlockSpaceLaser;    
    public override void DoTrigger()
    {
        UnlockSpaceLaser?.Invoke();
    }
}
