using HexGame.Units;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Communication/Unit Complete Communication")]
public class UnitCompleteCommunication : CommunicationBase, ISelfValidator
{
    public override void Complete()
    {
        base.Complete();

        if (trigger)
            trigger.DoTrigger();
    }

    public override void Initiallize()
    {
        if (beforeTrigger)
            beforeTrigger.DoTrigger();
        if (nextCommunication)
            CommunicationMenu.AddCommunication(nextCommunication);
    }

    public void Validate(SelfValidationResult result)
    {
        if(this.directivesToUnlock.Count == 0)
            result.AddError("No Directives to Unlock");
        if(this.AudioClip == null)
            result.AddWarning("No Audio Clip");
    }
}
