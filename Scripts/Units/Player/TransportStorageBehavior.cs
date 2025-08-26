using HexGame.Resources;
using HexGame.Units;
using UnityEngine.InputSystem;

public class TransportStorageBehavior : UnitStorageBehavior, IStoreResource, ITransportResources, IHaveResources
{
    private new void Awake()
    {
        this.isTransport = true; //avoid boxing/unboxing when checking type
        this.SetRequestPriority(CargoManager.RequestPriority.low);
        base.Awake();
    }

    public override void AddDeliverConnection(UnitStorageBehavior usb, bool suppressWarning = false)
    {
        if (!CanMakeConnection(usb))
            return;

        if (!ConnectionInRange(usb, suppressWarning))
        {
            MessagePanel.ShowMessage("Connection out of range", this.gameObject);
            SFXManager.PlaySFX(SFXType.error);
            return;
        }

        connections.Add(usb);
        connectionAdded?.Invoke(this, usb);
        OrderConnections();

        if (SaveLoadManager.Loading)
            return;

        if (UnitSelectionManager.selectedUnit != null && UnitSelectionManager.selectedUnit.gameObject == this.gameObject)
            ConnectionChanged();

        if (!Keyboard.current.shiftKey.isPressed)
            StopListeningChangingConnection();
    }

    private bool CanMakeConnection(UnitStorageBehavior usb)
    {
        if (connections.Contains(usb))
            return false;

        if (usb == null || usb == this)
            return false;

        if (this.preventUserMadeConnections)
            return false;

        if (this.preventConnections || usb.preventConnections)
        {
            SFXManager.PlaySFX(SFXType.error);
            return false;
        }

        return true;
    }

    private bool ConnectionInRange(UnitStorageBehavior usb, bool suppressWarning = false)
    {
        if (suppressWarning)
            return true;

        if (usb is TransportStorageBehavior tsb)
        {
            return true;
        }
        else
        {
            return HelperFunctions.HexRange(this.transform.position, usb.transform.position) <= CargoManager.transportRange;
        }
    }
    #region Delivery and Pickup
    public override bool DeliverResource(ResourceAmount resource)
    {
        if (!this.gameObject.activeSelf)
            return false;

        if (!deliverTypes.Contains(resource.type) && resource.type != ResourceType.Workers)
            return false;

        //As soon as its delivered we want to keep the resource moving :)
        if(resource.type != ResourceType.Workers)
            RequestPickup(resource);
        return base.DeliverResource(resource);
    }

    public override ResourceAmount PickupResource(ResourceAmount resource)
    {
        ResourceAmount ra = base.PickupResource(resource);
        CheckResourceLevels();
        return ra;
    }

    #endregion

    #region resource management

    public void AddAllowedResource(ResourceType resource)
    {
        deliverTypes.Add(resource);
        pickUpTypes.Add(resource);
        allowedTypes.Add(resource);
    }

    public void RemoveAllowedResource(ResourceType resource)
    {
        deliverTypes.Remove(resource);
        pickUpTypes.Remove(resource);
        allowedTypes.Remove(resource);
    }
    #endregion
}
