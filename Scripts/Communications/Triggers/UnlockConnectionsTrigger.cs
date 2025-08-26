using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Unlock Connections Trigger", menuName = "Hex/Triggers/UnlockConnections")]
[System.Serializable]
public class UnlockConnectionsTrigger : TriggerBase
{
    public static event Action ConnectionsUnlocked;
    public override void DoTrigger()
    {
        ConnectionsUnlocked?.Invoke();
    }
}
