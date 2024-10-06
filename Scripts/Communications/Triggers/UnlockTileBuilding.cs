using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Triggers/Unlock Tile Building")]
public class UnlockTileBuilding : TriggerBase
{
    public static event Action unlockTileBuilding;
    public override void DoTrigger()
    {
        unlockTileBuilding?.Invoke();
    }
}
