using Nova;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using NovaSamples.UIControls;

public class TileToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string title;
    [SerializeField] private Sprite icon;
    [SerializeField] private Vector2 offset = new Vector2(75,100);

    public static event Action<List<PopUpInfo>, Sprite, Vector2, TileToolTip> openToolTip;
    public static event Action<TileToolTip> closeToolTip;
    private ResourceTile resourceTile;
    private PlaceHolderTileBehavior placeHolderTile;
    private FogGroundTile fogGroundTile;

    private void Awake()
    {
        resourceTile = GetComponentInParent<ResourceTile>();
        placeHolderTile = GetComponent<PlaceHolderTileBehavior>();
        fogGroundTile = GetComponentInParent<FogGroundTile>();
    }

    public List<PopUpInfo> GetPopupInfo()
    {
        List<PopUpInfo> info = new List<PopUpInfo>()
        {
            new PopUpInfo(GetTitle(), PopUpInfo.PopUpInfoType.name),
            new PopUpInfo(GetDescription(), PopUpInfo.PopUpInfoType.description)
        };

        return info;
    }

    private string GetDescription()
    {
        if(resourceTile)
            return resourceTile.ResourceAmount + " remaining";
        else if(placeHolderTile)
            return $"<i>Right Click to Cancel</i>";
        return "";
    }

    private string GetTitle()
    {
        if (resourceTile)
            return resourceTile.ResourceType.ToNiceString();
        else if (placeHolderTile)
            return $"{placeHolderTile.TileType.ToNiceString()} Building Site";
        else
            return title;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!PCInputManager.MouseOverVisibleUIObject() && fogGroundTile != null && !fogGroundTile.IsDown)
            openToolTip?.Invoke(GetPopupInfo(), icon, offset, this);
        else if (!PCInputManager.MouseOverVisibleUIObject() && placeHolderTile != null)
            openToolTip?.Invoke(GetPopupInfo(), icon, offset, this);

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        closeToolTip?.Invoke(this);
    }
}
