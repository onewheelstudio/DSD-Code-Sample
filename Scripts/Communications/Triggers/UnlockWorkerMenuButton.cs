using System;
using UnityEngine;

[CreateAssetMenu(fileName = "UnlockWorkerMenu", menuName = "Hex/Triggers/UnlockWorkerMenu")]
public class UnlockWorkerMenuButton : TriggerBase
{
    public static event Action WorkerButtonUnlocked;
    public override void DoTrigger()
    {
        WorkerButtonUnlocked?.Invoke();
    }
}


