using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Triggers/Tiles Built")]
public class TilesBuiltTrigger : TriggerBase
{
    public static event System.Action tilesBuilt;
    public override void DoTrigger()
    {
        tilesBuilt?.Invoke();
    }
}
