using HexGame.Resources;
using HexGame.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Directives/Deliver Directive")]
public class DeliverDirective : DirectiveQuest
{
    [SerializeField] private List<ResourceAmount> resourcesToDeliver;
    [NonSerialized] private List<ResourceAmount> deliveredResources = new();
    [SerializeField] private PlayerUnitType deliverToUnit;
    [NonSerialized] List<PlayerUnit> units = new();

    public override void Initialize()
    {
        base.Initialize();
        units = UnitManager.GetPlayerUnitByType(deliverToUnit);
        for (int i = 0; i < units.Count; i++)
        {
            PlayerUnit unit = units[i];
            UnitStorageBehavior usb = unit.GetComponent<UnitStorageBehavior>();
            usb.resourceDelivered += OnResourceDelivered;
        }
    }

    public override List<string> DisplayText()
    {
        List<string> displayText = new List<string>();
        for (int i = 0; i < resourcesToDeliver.Count; i++)
        {
            ResourceAmount requiredAmount = resourcesToDeliver[i];
            ResourceAmount deliveredAmount = deliveredResources.FirstOrDefault(x => x.type == requiredAmount.type);

            displayText.Add($"Deliver {requiredAmount.type.ToNiceString()}: {deliveredAmount.amount}/{requiredAmount.amount}");
        }

        return displayText;
    }

    public override List<bool> IsComplete()
    {
        List<bool> complete = new List<bool>();
        for (int i = 0; i < resourcesToDeliver.Count; i++)
        {
            ResourceAmount requiredAmount = resourcesToDeliver[i];
            ResourceAmount deliveredAmount = deliveredResources.FirstOrDefault(x => x.type == requiredAmount.type);

            complete.Add(deliveredAmount.amount >= requiredAmount.amount);
        }

        return complete;
    }

    public override void OnComplete()
    {
        for (int i = 0; i < units.Count; i++)
        {
            if(units[i] == null)
                continue;

            PlayerUnit unit = units[i];
            UnitStorageBehavior usb = unit.GetComponent<UnitStorageBehavior>();
            usb.resourceDelivered -= OnResourceDelivered;
        }

        CommunicationMenu.AddCommunication(OnCompleteCommunication);
    }

    private void OnResourceDelivered(UnitStorageBehavior behavior, ResourceAmount amount)
    {
        for(int i = 0; i < resourcesToDeliver.Count; i++)
        {
            if (resourcesToDeliver[i].type != amount.type)
                continue;

            int resourceIndex = -1;
            for(int j = 0; j < deliveredResources.Count; j++)
            {
                if (deliveredResources[j].type != amount.type)
                    continue;

                resourceIndex = j;
                break;
            }

            if(resourceIndex >= 0)
                deliveredResources[resourceIndex] += amount;
            else
                deliveredResources.Add(amount);

            DirectiveUpdated();
            return;
        }
    }
}
