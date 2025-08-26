using HexGame.Resources;
using Nova;
using NovaSamples.UIControls;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SupplyShipDropdownVisuals : ButtonVisuals
{
    public ListView allowedResources;
    private static PlayerResources PlayerResources
    {
        get
        {
            if (playerResources == null)
                playerResources = GameObject.FindFirstObjectByType<PlayerResources>();
            return playerResources;
        }
    }
    private static PlayerResources playerResources;
    private ResourceType importType;
    private int numShipments = 0;
    private bool initialized = false;

    public Button selectShipButton;
    [SerializeField] private Button moveToButton;
    public static event Action<Vector3> MoveToPosition;
    private Vector3 shipPosition;

    public void Initialize()
    {
        if (initialized)
            return;

        allowedResources.AddDataBinder<ResourceType, ResourceIconDisplayVisuals>(BindAllowedResources);
        moveToButton.RemoveAllListeners();
        moveToButton.Clicked += GoToLocation;
        initialized = true;
    }

    public void PopulateAllowedResources(ITransportResources transportResources)
    {
        List<ResourceType> resources = transportResources.GetAllowedResources().ToList();
        allowedResources.SetDataSource(resources);
    }

    private void BindAllowedResources(Data.OnBind<ResourceType> evt, ResourceIconDisplayVisuals target, int index)
    {
        var resourceTemplate = PlayerResources.GetResourceTemplate(evt.UserData);
        target.Background.SetImage(resourceTemplate.icon);
        target.Background.Color = resourceTemplate.resourceColor;
        target.toolTip.SetToolTipInfo(evt.UserData.ToNiceString(), resourceTemplate.icon);
        //target.Label.Text = "Supply Ship " + (index + 1).ToString();
    }

    public void SetLocation(Vector3 position)
    {
        this.shipPosition = position;
    }

    public void GoToLocation()
    {
        MoveToPosition?.Invoke(shipPosition);
    }

    public void Close()
    {

    }
}
