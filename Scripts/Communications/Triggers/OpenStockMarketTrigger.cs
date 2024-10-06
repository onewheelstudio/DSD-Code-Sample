using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Triggers/Open Stock Market Trigger")]
public class OpenStockMarketTrigger : TriggerBase
{
    public override void DoTrigger()
    {
        FindObjectOfType<MarketWindow>().OpenWindow();
    }
}
