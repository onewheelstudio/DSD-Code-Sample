using HexGame.Resources;
using HexGame.Units;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitStorageBehavior))]
public class PlaceHolderTileBehavior : UnitBehavior, IHavePopupInfo
{
    [SerializeField]
    private List<ResourceAmount> neededResources = new List<ResourceAmount>();
    private UnitStorageBehavior usb;
    private HexTile hexTile;
    public HexTileType TileType => hexTile.TileType;

    public static event Action<PlaceHolderTileBehavior, HexGame.Resources.HexTileType> tileComplete;
    public static event Action<List<ResourceAmount>> resourcesUsed;

    private void Awake()
    {
        if (hexTile == null)
            hexTile = this.GetComponent<HexTile>();
    }

    protected void OnValidate()
    {
        UpdateAllowedTypes();
    }

    private void OnDisable()
    {
        StopBehavior();
    }

    [Button]
    public override void StartBehavior()
    {
        _isFunctional = true;

        if (usb == null)
            usb = GetComponent<UnitStorageBehavior>();
        usb.resourceDelivered += AreAllResoucesDelivered;

        foreach (var resource in neededResources)
        {
            usb.MakeDeliveryRequest(resource, true);
        }
    }

    public override void StopBehavior()
    {
        if (usb == null)
            usb = GetComponent<UnitStorageBehavior>();

        usb.resourceDelivered -= AreAllResoucesDelivered;
        _isFunctional = false;
        CargoManager.RemoveAllRequests(usb);
    }

    private void AreAllResoucesDelivered(UnitStorageBehavior usb, ResourceAmount resource)
    {
        foreach (ResourceAmount r in neededResources)
        {
            if (!usb.HasResource(r))
                return;
        }
        TileIsComplete();
    }

    private void TileIsComplete()
    {
        //Debug.Log("Tile is complete", this.gameObject);
        tileComplete?.Invoke(this, hexTile.TileType);
        resourcesUsed?.Invoke(neededResources);
        StopBehavior();
        this.gameObject.SetActive(false);
    }

    public List<PopUpInfo> GetPopupInfo()
    {
        List<PopUpInfo> popUpInfos = new List<PopUpInfo>();
        if(hexTile != null)
            popUpInfos.Add(new PopUpInfo($"Tile: {hexTile.TileType.ToString().ToUpper()}", -1000, PopUpInfo.PopUpInfoType.name));
        popUpInfos.Add(new PopUpInfo($"{Mathf.RoundToInt(PercentComplete() * 100f)}% Complete", 0, PopUpInfo.PopUpInfoType.stats));

        return popUpInfos;
    }

    private float PercentComplete()
    {
        if (usb == null)
            usb = GetComponent<UnitStorageBehavior>();

        float totalStored = usb.TotalStored();
        float totalNeeded = 0f;

        foreach (var resource in neededResources)
        {
            totalNeeded += resource.amount;
        }

        if (Mathf.RoundToInt(totalNeeded) == 0)
            return 1f;

        return totalStored / totalNeeded;
    }

    private void UpdateAllowedTypes()
    {
        if (usb == null)
            usb = GetComponent<UnitStorageBehavior>();

        List<ResourceType> allowedTypes = new List<ResourceType>();
        foreach (var resource in neededResources)
        {
            allowedTypes.Add(resource.type);
        }

        usb.SetAllowedTypes(allowedTypes);
    }

    public HexTileType GetTileType()
    {
        return hexTile.TileType;
    }

    internal void RemovePlaceHolder()
    {
        HexTileManager.RemoveTileAtLocation(this.transform.position.ToHex3());
        this.gameObject.SetActive(false);
        SFXManager.PlaySFX(SFXType.tilePlace);
    }
}
