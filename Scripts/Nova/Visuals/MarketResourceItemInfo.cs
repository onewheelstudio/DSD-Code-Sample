using HexGame.Resources;
using Nova;
using NovaSamples.UIControls;
using System;
using UnityEngine;

public class MarketResourceItemInfo : ButtonVisuals
{
    [Header("Market Stuff")]
    public UIBlock2D icon;
    public TextBlock value;
    public UIBlock2D upArrow;
    public UIBlock2D downArrow;
    public ResourceType resource;
    public Toggle starToggle;
    [HideInInspector]
    public Transform transform;
    [HideInInspector]    
    public int index;

    public void SetPrice(ResourceType resource, StockMarket.ResourceMarket market)
    {
        if (this.resource != resource || market == null)
            return;

        if(value == null)
            return;

        value.Text = Round(market.displayPrice).ToString();
        if (market.displayPrice > market.basePrice * 1.2f)
        {
            value.Color = ColorManager.GetColor(ColorCode.green);
            upArrow.gameObject.SetActive(true);
            downArrow.gameObject.SetActive(false);
        }
        else if (market.displayPrice < market.basePrice * 0.8f)
        {
            value.Color = ColorManager.GetColor(ColorCode.red);
            upArrow.gameObject.SetActive(false);
            downArrow.gameObject.SetActive(true);
        }
        else
        {
            value.Color = Color.white;
            upArrow.gameObject.SetActive(false);
            downArrow.gameObject.SetActive(false);
        }
    }

    private float Round(float value)
    {
        if (value < 10)
            return (float)Math.Round(value, 2);
        else if (value < 100)
            return (float)Math.Round(value, 1);
        else
            return Mathf.RoundToInt(value);
    }
}
