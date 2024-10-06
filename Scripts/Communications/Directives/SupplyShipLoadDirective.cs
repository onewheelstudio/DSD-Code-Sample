using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Directives/Supply Ship Load Directive")]
public class SupplyShipLoadDirective : DirectiveBase
{
    [SerializeField] private int numberOfLoads = 1;
    private int numberOfLoadsCompleted = 0;

    public override List<string> DisplayText()
    {
        return new List<string>() { $"Launch Supply Ship {numberOfLoadsCompleted}/{numberOfLoads}" };

    }

    public override void Initialize()
    {
        SupplyShipBehavior.supplyShipLaunched += SupplyShipLaunched;
        if (OnStartCommunication != null)
            CommunicationMenu.AddCommunication(OnStartCommunication);
    }

    public override List<bool> IsComplete()
    {
        return new List<bool>() { numberOfLoadsCompleted >= numberOfLoads };
    }

    public override void OnComplete()
    {
        SupplyShipBehavior.supplyShipLaunched -= SupplyShipLaunched;
        if (OnCompleteCommunication != null)
            CommunicationMenu.AddCommunication(OnCompleteCommunication);
    }

    private void SupplyShipLaunched(SupplyShipBehavior behavior)
    {
        numberOfLoadsCompleted++;
        DirectiveUpdated();
    }
}
