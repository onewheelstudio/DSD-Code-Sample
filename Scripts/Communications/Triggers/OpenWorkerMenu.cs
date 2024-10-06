using System;
using UnityEngine;

[CreateAssetMenu(fileName = "UnlockWorkerMenu", menuName = "Hex/Triggers/Open Worker Menu")]
public class OpenWorkerMenu : TriggerBase
{
    public static event Action WorkerMenuOpen;
    public override void DoTrigger()
    {
        WorkerMenuOpen?.Invoke();
    }
}
