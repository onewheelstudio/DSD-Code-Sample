using HexGame.Resources;
using System.Collections.Generic;

public class FuelSupplyShipDirective : DirectiveBase
{
    private SupplyShipBehavior supplyShip;
    private ResourceAmount fueldRequired;
    private ResourceAmount fuelLoaded;

    public override List<string> DisplayText()
    {
        return new List<string>() { $"Fuel Supply Ship {fuelLoaded.amount} / {fueldRequired.amount}" };
    }

    public void SetSupplyShip(SupplyShipBehavior supplyShip)
    {
        this.supplyShip = supplyShip;
        this.supplyShip.fuelReceived += FuelReceived;
    }

    public override void Initialize()
    {
        fueldRequired = new ResourceAmount(ResourceType.Energy, SupplyShipBehavior.GetFuelAmount());
    }

    public override List<bool> IsComplete()
    {
        return new List<bool>(){ fuelLoaded.amount >= fueldRequired.amount };
    }

    public override void OnComplete()
    {
        this.supplyShip.fuelReceived -= FuelReceived;
    }

    private void FuelReceived(ResourceAmount amount)
    {
        if (amount.type != fueldRequired.type)
            return;

        fuelLoaded.amount += amount.amount;
        DirectiveUpdated();
    }
}
