using Sirenix.OdinInspector;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Communication/Play Now Communication")]
public class PlayNowCommunication : CommunicationBase
{
    public override void Initiallize()
    {
        if(beforeTrigger)
            beforeTrigger.DoTrigger();
        if (nextCommunication)
            CommunicationMenu.AddCommunication(nextCommunication);
    }

    public override void Complete()
    {
        base.Complete();

        if (trigger)
            trigger.DoTrigger();
    }

}
