using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Triggers/Open Tech Tree Trigger")]
public class OpenTechTreeTrigger : TriggerBase
{
    public override void DoTrigger()
    {
        FindObjectOfType<HexTechTree>().OpenWindow();
    }
}
