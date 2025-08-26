using HexGame.Resources;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Directives/Sell Resource")]
public class SellResourceDirective : DirectiveQuest
{
    [SerializeField] private int loadsToSell;
    [NonSerialized] private int loadsSold;

    public override void Initialize()
    {
        base.Initialize();
        CommunicationMenu.AddCommunication(OnStartCommunication);
        SupplyShipBehavior.LoadShipped += TradeConfirmed;
    }

    private void TradeConfirmed(SupplyShipBehavior behavior, RequestType request, List<ResourceAmount> resource)
    {
        if (request == RequestType.sell)
        {
            loadsSold++;
            DirectiveUpdated();
        }
    }

    public override List<string> DisplayText()
    {
        int loadsRemaining = loadsToSell - loadsSold;
        if (loadsRemaining <= 0) //this shouldn't happen but it did once...
            DirectiveUpdated();

        if(loadsRemaining == 1)
            return new List<string> {$"Sell {loadsRemaining} load of any resource from the market."};
        else
            return new List<string> {$"Sell {loadsRemaining} loads of any resources from the market."};

    }

    public override List<bool> IsComplete()
    {
        return new List<bool> {loadsSold >= loadsToSell};
    }

    public override void OnComplete()
    {
        SupplyShipBehavior.LoadShipped -= TradeConfirmed;
        base.OnComplete();
    }

    public void SetLoadsToSell(int count)
    {
        loadsToSell = count;
    }


}
