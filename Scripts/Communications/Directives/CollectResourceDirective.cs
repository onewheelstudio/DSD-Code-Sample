using HexGame.Resources;
using HexGame.Units;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Directives/Collect Resource Directive")]
public class CollectResourceDirective : DirectiveBase
{
    [SerializeField] private ResourceAmount resourceAmount;
    private int amountCollected;

    private void ResourceUpdated(ResourceAmount incomingResource)
    {
        if(incomingResource.type != resourceAmount.type)
            return;
        
        amountCollected += incomingResource.amount;
        DirectiveUpdated();
    }

    public override List<string> DisplayText()
    {
        return new List<string>() { $"Collect {resourceAmount.type.ToNiceString()}: {amountCollected}/{resourceAmount.amount}" };
    }

    public override void Initialize()
    {
        GlobalStorageBehavior.resourceAdded += ResourceUpdated;
        amountCollected = 0;
        CommunicationMenu.AddCommunication(OnStartCommunication);
    }

    public override List<bool> IsComplete()
    {
        return new List<bool>() { amountCollected >= resourceAmount.amount };
    }

    public override void OnComplete()
    {
        GlobalStorageBehavior.resourceAdded -= ResourceUpdated;
        CommunicationMenu.AddCommunication(OnCompleteCommunication);
        OnCompleteTrigger.ForEach(t => t.DoTrigger());
    }
}
