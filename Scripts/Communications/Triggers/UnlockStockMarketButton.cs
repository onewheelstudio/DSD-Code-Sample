using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Triggers/Unlock Stock Market Button")]
public class UnlockStockMarketButton : TriggerBase
{
    public static event Action unlockStockMarketButton;
    public override void DoTrigger()
    {
        unlockStockMarketButton?.Invoke();
    }

}
