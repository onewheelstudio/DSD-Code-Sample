using HexGame.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Directives/Supply Depot Directive")]
public class SupplyShipDirective : DirectiveBase
{
    [SerializeField]
    private List<ResourceAmount> requestedAmounts;
    private List<ResourceAmount> receivedAmounts;
    public static event Action<List<ResourceAmount>> resourceRequested;

    public override List<string> DisplayText()
    {
        List<string> text = new List<string>();
        foreach (var resource in requestedAmounts)
        {
            text.Add($"{resource.type} to the Landing Pad: {receivedAmounts.Find(x =>x.type == resource.type).amount}/{resource.amount}");
        }

        return text;
    }

    public override void Initialize()
    {
        SupplyShipBehavior.resourceReceived += ResourceRecieved;
        AddFuel();
        resourceRequested?.Invoke(requestedAmounts);
    }

    private void AddFuel()
    {
        if(!requestedAmounts.Any(x => x.type == ResourceType.Energy))
            requestedAmounts.Add(new ResourceAmount(ResourceType.Energy, SupplyShipBehavior.GetFuelAmount()));
    }

    public override List<bool> IsComplete()
    {
        List<bool> result = new List<bool>();
        foreach (var resource in requestedAmounts)
        {
            result.Add(receivedAmounts.Find(x => x.type == resource.type).amount >= resource.amount);
        }
        return result;
    }

    public override void OnComplete()
    {
        receivedAmounts.Clear();
        SupplyShipBehavior.resourceReceived -= ResourceRecieved;
        CommunicationMenu.AddCommunication(OnCompleteCommunication);
    }

    private void ResourceRecieved(ResourceAmount receivedResource, SubRequest subRequest)
    {
        //is it a resource we care about?
        if (!requestedAmounts.Any(x => x.type == receivedResource.type))
            return;

        //does it exist in the list??
        for (int i = 0; i < receivedAmounts.Count; i++)
        {
            if (receivedAmounts[i].type == receivedResource.type)
            {
                receivedAmounts[i] = new ResourceAmount(receivedResource.type, receivedAmounts[i].amount + receivedResource.amount);
                DirectiveUpdated();
                return;
            }
        }

        //if not add it
        ResourceAmount resource = receivedResource;
        receivedAmounts.Add(resource);

        DirectiveUpdated();
    }
}
