using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Open Tile Menu Trigger", menuName = "Hex/Triggers/OpenTileMenuTrigger")]
public class OpenTileMenuTrigger : TriggerBase
{
    public static event Action OpenTileMenu;
    public override void DoTrigger()
    {
        OpenTileMenu?.Invoke();
    }
}
