using HexGame.Resources;
using Nova;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ClipMask))]
public class InfoToolTipWindow : WindowPopup
{
    [SerializeField] private UIBlock2D icon;
    [SerializeField] private TextBlock title;
    [SerializeField] private TextBlock info;
    [SerializeField] private ListView statsList;
    private UIBlock2D parentBlock;
    private Camera NovaCamera
    {
        get
        {
            if(novaCamera == null)
                novaCamera = GameObject.FindObjectOfType<Nova.ScreenSpace>().TargetCamera;
            return novaCamera;
        }
    }
    private Camera novaCamera;
    private Camera mainCamera;
    private Camera MainCamera
    {
        get
        {
            if(mainCamera == null)
                mainCamera = Camera.main;
            return mainCamera;
        }
    }

    private ScreenSpace screenSpace;
    private float canvasScale => Screen.width / screenSpace.ReferenceResolution.x;

    private void Awake()
    {
        parentBlock = this.GetComponent<UIBlock2D>();
        screenSpace = this.GetComponentInParent<ScreenSpace>();
        statsList.AddDataBinder<PopUpResourceAmount, UnitInfoButtonVisuals>(DisplayStats);
    }



    private new void OnEnable()
    {
        base.OnEnable();
        InfoToolTip.openToolTip += OpenToolTip;
        InfoToolTip.openToolTipStats += PopulateStats;
        InfoToolTip.closeToolTip += CloseToolTip;

        TileToolTip.openToolTip += OpenToolTip;
        TileToolTip.closeToolTip += CloseToolTip;
        CloseWindow();
    }



    private new void OnDisable()
    {
        base.OnDisable();
        InfoToolTip.openToolTip -= OpenToolTip;
        InfoToolTip.openToolTipStats -= PopulateStats;
        InfoToolTip.closeToolTip -= CloseToolTip;
        TileToolTip.openToolTip -= OpenToolTip;
        TileToolTip.closeToolTip -= CloseToolTip;

    }


    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame
            || Mouse.current.rightButton.wasPressedThisFrame
            || Mouse.current.middleButton.wasPressedThisFrame)
        {
            CloseWindow();
        }
    }

    private void CloseToolTip(InfoToolTip obj)
    {
        CloseWindow();
    }

    private void OpenToolTip(List<PopUpInfo> info, Sprite icon, Vector2 offset, InfoToolTip tooltip)
    {
        title.Text = info.Where(x => x.infoType == PopUpInfo.PopUpInfoType.name).First().info;
        this.info.Text = info.Where(x => x.infoType == PopUpInfo.PopUpInfoType.description).First().info;
        if(icon != null)
            this.icon.SetImage(icon);

        this.icon.gameObject.SetActive(icon != null);

        Vector3 position = NovaCamera.WorldToScreenPoint(tooltip.transform.position);
        Vector2 screenOffset = GetOffset(Mouse.current.position.ReadValue());
        offset += screenOffset;

        parentBlock.Position.Y = (position.y + offset.y) / canvasScale;
        parentBlock.Position.X = (position.x + offset.x) / canvasScale;
        OpenWindow();
    }

    private void PopulateStats(List<PopUpResourceAmount> list)
    {
        if(list == null || list.Count == 0)
        {
            //statsList.gameObject.SetActive(false);
            return;
        }
        else
        {
            //statsList.gameObject.SetActive(true);
            statsList.SetDataSource(list);
        }
    }

    private void DisplayStats(Data.OnBind<PopUpResourceAmount> evt, UnitInfoButtonVisuals target, int index)
    {
        ResourceTemplate resource = GameObject.FindObjectOfType<PlayerResources>().GetResourceTemplate(evt.UserData.resource.type);

        target.icon.SetImage(resource.icon);
        target.label.Text = $"{evt.UserData.resource.amount}";

        target.infoToolTip.SetToolTipInfo(resource.type.ToNiceString(), resource.icon, "");
        target.icon.Color = resource.resourceColor;
    }

    private void CloseToolTip(TileToolTip tip)
    {
        CloseWindow();
    }

    private void OpenToolTip(List<PopUpInfo> info, Sprite icon, Vector2 offset, TileToolTip tip)
    {
        title.Text = info.Where(x => x.infoType == PopUpInfo.PopUpInfoType.name).First().info;
        this.info.Text = info.Where(x => x.infoType == PopUpInfo.PopUpInfoType.description).First().info;
        if (icon != null)
            this.icon.SetImage(icon);

        this.icon.gameObject.SetActive(icon != null);

        Vector3 position = MainCamera.WorldToScreenPoint(tip.transform.position);
        Vector2 screenOffset = GetOffset(Mouse.current.position.ReadValue());
        offset += screenOffset;

        parentBlock.Position.Y = (position.y + offset.y) / canvasScale;
        parentBlock.Position.X = (position.x + offset.x) / canvasScale;
        OpenWindow();
    }

    private Vector2 GetOffset(Vector2 mousePosition)
    {
        Vector2 offset = Vector2.zero;
        Vector3 size = parentBlock.Size.Value;

        if (mousePosition.y > Screen.height * 0.75f)
            offset += new Vector2(0, -size.y - offset.y);
        else
            offset += new Vector2(0, offset.y);

        //not dividing the size by 2 because of the canvas scale
        if (mousePosition.x + size.x * canvasScale > Screen.width * 0.95f)
            offset += new Vector2(-size.x - offset.x, 0);
        else
            offset += new Vector2(offset.x, 0);

        return offset * canvasScale;
    }
}
