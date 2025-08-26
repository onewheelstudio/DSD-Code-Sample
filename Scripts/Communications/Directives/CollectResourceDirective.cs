using HexGame.Resources;
using HexGame.Units;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Directives/Collect Resource Directive")]
public class CollectResourceDirective : DirectiveBase
{
    [SerializeField] private ResourceAmount resourceAmount;
    private int amountCollected;

    public override List<string> DisplayText()
    {
        return new List<string>() { $"Collect {resourceAmount.type.ToNiceString()}: {amountCollected}/{resourceAmount.amount}" };
    }

    public override void Initialize()
    {
        base.Initialize();
        ResourceProductionBehavior.resourceProduced += ResourceUpdated;
        CollectionBehavior.terreneStored += ResourceUpdated;
        //amountCollected = 0;
        CommunicationMenu.AddCommunication(OnStartCommunication);
    }

    private void ResourceUpdated(ResourceProductionBehavior behavior, ResourceAmount amount)
    {
        if (amount.type != resourceAmount.type)
            return;

        amountCollected += amount.amount;
        DirectiveUpdated();
    }
    private void ResourceUpdated(ResourceAmount amount)
    {
        if (amount.type != resourceAmount.type)
            return;

        amountCollected += amount.amount;
        DirectiveUpdated();
    }

    public override List<bool> IsComplete()
    {
        return new List<bool>() { amountCollected >= resourceAmount.amount };
    }

    public override void OnComplete()
    {
        ResourceProductionBehavior.resourceProduced -= ResourceUpdated;
        CollectionBehavior.terreneStored -= ResourceUpdated;
        CommunicationMenu.AddCommunication(OnCompleteCommunication);
        OnCompleteTrigger.ForEach(t => t.DoTrigger());
    }


}
