using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Triggers/Unlock Auto Trader Trigger")]
public class UnlockAutoTraderTrigger : TriggerBase
{
    public static event Action UnlockAutoTrader;
    public override void DoTrigger()
    {
        UnlockAutoTrader?.Invoke();
    }
}
