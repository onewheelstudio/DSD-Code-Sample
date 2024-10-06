using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "TipCommunication", menuName = "Hex/Communication/TipCommunication")]
public class TipCommunication : CommunicationBase
{
    [PropertyOrder(-1000), InfoBox("This is displayed on the UI")]
    public string tipHint;

    public override void Initiallize()
    {
        if (beforeTrigger)
            beforeTrigger.DoTrigger(); 
        if (nextCommunication)
            CommunicationMenu.AddCommunication(nextCommunication);
    }

    [Button("Add Tip")]
    private void AddCommunication()
    {
        GameTipsWindow.AddTip(this);
    }
}
