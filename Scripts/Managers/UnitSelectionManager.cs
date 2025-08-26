using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using NovaSamples.UIControls;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class UnitSelectionManager : MonoBehaviour
{
    [SerializeField] private GameObject selectionMarker;
    static private PlayerUnit _selectedUnit;
    private static IMove currentMove;
    public static PlayerUnit selectedUnit => _selectedUnit;
    public static PlayerUnitType selectedUnitType => _selectedUnit.unitType;
    private PlayerUnit playerUnitUnderMouse;
    public static event Action<PlayerUnit> unitSelected;
    public static event Action<PlayerUnit> unitUnSelected;

    private UIControlActions uiControls;
    public static bool changingConnections = false;
    public static event Action<PlayerUnit> unitClicked;
    public static event Action<PlayerUnit> hoverOverUnit;
    public static event Action<PlayerUnit> unHoverOverUnit;
    private HexTileManager htm;
    private CursorManager cursorManager;
    private SpaceLaser spaceLaser;

    public static event Action<Hex3, Hex3, PlayerUnit> UnitMoved;

    [Header("Special Bits")]
    [SerializeField] private UnitMovementConnection movementConnection;
    private EnemyCrystalManager ecm;

    private void Awake()
    {
        uiControls = new UIControlActions();
        htm = FindFirstObjectByType<HexTileManager>();   
        cursorManager = FindFirstObjectByType<CursorManager>();  
        spaceLaser = FindFirstObjectByType<SpaceLaser>();
        ecm = FindFirstObjectByType<EnemyCrystalManager>();
    }

    private void OnEnable()
    {
        uiControls.UI.RightClick.canceled += RightClickReleased;
        uiControls.UI.RightClick.started += RightClick;
        uiControls.UI.LeftClick.performed += LeftClick;

        uiControls.UI.MoveUnit.performed += ToggleMove;
        uiControls.UI.CloseWindow.performed += UnitUnSelected;
        uiControls.Enable();
        UnitInfoWindow.moveButtonClicked += ToggleUnitReadyToMove;
        GroupControlManager.MoveToUnit += UnitSelected;
        ControlsManager.UIControlsUpdated += UpdateMovementBindings;
        WindowPopup.SomeWindowOpened += UnitUnSelected;
        SpaceLaser.SpaceLaserIsAttacking += UnitUnSelected;
    }



    private void OnDisable()
    {
        uiControls.UI.RightClick.canceled -= RightClickReleased;
        uiControls.UI.RightClick.started -= RightClick;
        uiControls.UI.LeftClick.performed -= LeftClick;

        uiControls.UI.MoveUnit.performed -= ToggleMove;
        uiControls.UI.CloseWindow.performed -= UnitUnSelected;
        uiControls.Disable();

        UnitInfoWindow.moveButtonClicked -= ToggleUnitReadyToMove;
        GroupControlManager.MoveToUnit -= UnitSelected;
        ControlsManager.UIControlsUpdated -= UpdateMovementBindings;
        WindowPopup.SomeWindowOpened -= UnitUnSelected;
        SpaceLaser.SpaceLaserIsAttacking -= UnitUnSelected;
    }



    private void UpdateMovementBindings(string rebinds)
    {
        uiControls.LoadBindingOverridesFromJson(rebinds);
    }

    private void Update()
    {
        //if (movementConnection.gameObject.activeInHierarchy && currentMove != null)
        Hex3 mouseLocation = HelperFunctions.GetMouseHex3OnPlane();
        if (currentMove != null && currentMove.ReadyToMove)
        {
            movementConnection.gameObject.SetActive(true);
            cursorManager.CursorOff();

            movementConnection.SetStatus(IsValidPlacement(mouseLocation, false) && !currentMove.UnitsAreMoving);
            if(mouseLocation != movementConnection.destination.ToHex3())
            {
                HexTile tile = HexTileManager.GetHexTileAtLocation(mouseLocation);
                if(tile != null && tile.TileType == HexTileType.hill)
                {
                    Vector3 location = mouseLocation.ToVector3() + new Vector3(0f, UnitManager.HillOffset, 0f);
                    movementConnection.SetPositions(selectedUnit.transform.position, location);
                }
                else
                    movementConnection.SetPositions(selectedUnit.transform.position, mouseLocation);
            }
        }
        else if(currentMove == null && movementConnection.gameObject.activeInHierarchy)
        {
            movementConnection.gameObject.SetActive(false);
        }

        //if (_selectedUnit != null)
        //    return;

        //if(UnitManager.TryGetPlayerUnitAtLocation(mouseLocation, out PlayerUnit playerUnit))
        //{
        //    if (playerUnit == playerUnitUnderMouse)
        //        return;

        //    playerUnitUnderMouse = playerUnit;
        //    hoverOverUnit?.Invoke(playerUnit);
        //}
        //else
        //{
        //    if(playerUnitUnderMouse != null)
        //        unHoverOverUnit?.Invoke(playerUnitUnderMouse);
        //    playerUnitUnderMouse = null;
        //}
    }

    private void LeftClick(InputAction.CallbackContext context)
    {
        if (PCInputManager.MouseOverVisibleUIObject())
            return;

        if (changingConnections && UnitManager.PlayerUnitAtMouseLocation())
            UnitClickedOn();
        else if (Keyboard.current.shiftKey.isPressed && UnitManager.PlayerUnitAtMouseLocation() && selectedUnit != null)
            AddConnectionToSelected();
        else if (Keyboard.current.shiftKey.isPressed && selectedUnit != null && PlaceHolderAtLocation(out HexTile hexTile))
            AddConnectionToSelected(hexTile);
        else if (UnitManager.PlayerUnitAtMouseLocation())
            SelectUnit();
        else if (currentMove != null)
            DoMove(context);
        else
            UnitUnSelected();
    }



    private void RightClick(InputAction.CallbackContext context)
    {
        if (PlaceHolderAtLocation(out HexTile hexTile) && !htm.IsPlacingTile)
            hexTile.GetComponent<PlaceHolderTileBehavior>().RemovePlaceHolder();
    }


    private void RightClickReleased(InputAction.CallbackContext context)
    {
        if (_selectedUnit == null)
            return;
        else if (Keyboard.current.shiftKey.isPressed && UnitManager.PlayerUnitAtMouseLocation() && selectedUnit != null)
            RemoveConnectionFromSelected();
        else if (Keyboard.current.shiftKey.isPressed && selectedUnit != null && PlaceHolderAtLocation(out HexTile hexTile))
            RemoveConnectionFromSelected(hexTile);
        else if (currentMove != null)
            CancelMove();
        else
            UnitUnSelected(context);
    }



    private bool PlaceHolderAtLocation(out HexTile hexTile)
    {
        if (HexTileManager.IsTileAtHexLocation(HelperFunctions.GetMouseHex3OnPlane(), out hexTile))
        {
            return hexTile.isPlaceHolder;
        }
        return false;
    }

    private void UnitClickedOn()
    {
        if (UnitManager.TryGetPlayerUnitAtLocation(HelperFunctions.GetMouseHex3OnPlane(), out PlayerUnit playerUnit))
        {
            unitClicked?.Invoke(playerUnit);
        }
    }

    private void AddConnectionToSelected()
    {
        if (!ConnectionDisplayManager.ConnectionsUnlocked)
            return;

        if (UnitManager.TryGetPlayerUnitAtLocation(HelperFunctions.GetMouseHex3OnPlane(), out PlayerUnit playerUnit))
        {
            selectedUnit.GetComponent<UnitStorageBehavior>().AddDeliverConnection(playerUnit.GetComponent<UnitStorageBehavior>());
            SFXManager.PlaySFX(SFXType.click);
        }
    }

    private void AddConnectionToSelected(HexTile hexTile)
    {
        if (!ConnectionDisplayManager.ConnectionsUnlocked)
            return;

        selectedUnit.GetComponent<UnitStorageBehavior>().AddDeliverConnection(hexTile.GetComponent<UnitStorageBehavior>());
        SFXManager.PlaySFX(SFXType.click);
    }

    private void RemoveConnectionFromSelected()
    {
        if (!ConnectionDisplayManager.ConnectionsUnlocked)
            return;

        if (UnitManager.TryGetPlayerUnitAtLocation(HelperFunctions.GetMouseHex3OnPlane(), out PlayerUnit playerUnit))
        {
            selectedUnit.GetComponent<UnitStorageBehavior>().RemoveConnectionFromList(playerUnit.GetComponent<UnitStorageBehavior>());
            SFXManager.PlaySFX(SFXType.click);
        }
    }

    private void RemoveConnectionFromSelected(HexTile hexTile)
    {
        {
            if (!ConnectionDisplayManager.ConnectionsUnlocked)
                return;

            selectedUnit.GetComponent<UnitStorageBehavior>().RemoveConnectionFromList(hexTile.GetComponent<UnitStorageBehavior>());
            SFXManager.PlaySFX(SFXType.click);
        }
    }

    private void SelectUnit()
    {
        if (spaceLaser.IsAttacking)
            return;

        if(currentMove != null)
        {
            currentMove.CancelMove();
        }

        if(UnitManager.TryGetPlayerUnitAtLocation(HelperFunctions.GetMouseHex3OnPlane(), out PlayerUnit playerUnit))
        {
            UnitSelected(playerUnit.gameObject);

            if (playerUnit == null)
                return;

            if (playerUnit.TryGetComponent(out IMove move))
            {
                currentMove = move;
                movementConnection.gameObject.SetActive(!move.UnitsAreMoving);
                movementConnection.transform.position = playerUnit.transform.position;
                move.StartMove();
                cursorManager.CursorOff();
            }
        }
    }

    private void UnitUnSelected(WindowPopup window)
    {
        if (window is InfoToolTipWindow)
            return;

        CancelMove();
    }
    private void UnitUnSelected(InputAction.CallbackContext context)
    {
        if(Mouse.current.delta.ReadValue().sqrMagnitude < 5 && context.duration < 0.25f)
            UnitUnSelected();
    }

    private void UnitUnSelected()
    {
        currentMove?.CancelMove();
        currentMove = null;
        unitUnSelected?.Invoke(_selectedUnit);
        if(selectionMarker)
            selectionMarker?.SetActive(false);
        _selectedUnit = null;
    }

    public void SetUnitSelected(GameObject playerUnitObject)
    {
        UnitSelected(playerUnitObject);

        //if we selected a mobile unit we want to start to move
        PlayerUnit playerUnit = playerUnitObject.GetComponent<PlayerUnit>();
        if (playerUnit == null)
            return;

        if (playerUnit.TryGetComponent(out IMove move))
        {
            currentMove = move;
            movementConnection.gameObject.SetActive(!move.UnitsAreMoving);
            movementConnection.transform.position = playerUnit.transform.position;
            move.StartMove();
            cursorManager.CursorOff();
        }
    }

    private void UnitSelected(Transform transform)
    {
        if(transform == null)
            return;

        UnitSelected(transform.gameObject);
    }

    private void UnitSelected(GameObject playerUnit)
    {
        if(_selectedUnit == null || _selectedUnit.gameObject != playerUnit)
        {
            if (_selectedUnit != null)
                UnitUnSelected();

            _selectedUnit = playerUnit.GetComponent<PlayerUnit>();
            selectionMarker.SetActive(true);
            selectionMarker.transform.position = playerUnit.transform.position + Vector3.up * 0.05f;
            unitSelected?.Invoke(_selectedUnit);
        }
    }

    public void ClearSelection()
    {
        UnitUnSelected();
    }

    public static bool IsUnitSelected(GameObject gameObject)
    {
        return selectedUnit != null && selectedUnit.gameObject == gameObject;   
    }

    #region Unit Movement
    private void ToggleMove(InputAction.CallbackContext context)
    {
        ToggleUnitReadyToMove();
    }

    public void ToggleUnitReadyToMove()
    {
        if (selectedUnit == null)
            return;

        if (TryGetIMove(out IMove move))
        { 
            currentMove = move;
            move.ToggleReadyToMove();
            movementConnection.gameObject.SetActive(true);
            movementConnection.transform.position = selectedUnit.transform.position;
            cursorManager.CursorOff();
            return;
        }
        else
        {
            movementConnection.gameObject.SetActive(false);
            cursorManager.CursorOn();
            return;
        }
    }

    private void DoMove(InputAction.CallbackContext context)
    {
        DoUnitMove();
    }
    private void DoUnitMove()
    {
        if (currentMove == null || selectedUnit == null)
            return;

        Hex3 location = HelperFunctions.GetMouseHex3OnPlane();

        if(!IsValidPlacement(location) || currentMove.UnitsAreMoving)
        {
            SFXManager.PlaySFX(SFXType.error);
            return;
        }

        UnitMoved?.Invoke(selectedUnit.Location, location, selectedUnit);
        movementConnection.gameObject.SetActive(false);
        movementConnection.transform.position = location;
        currentMove.DoMove(location);
        cursorManager.CursorOn();
        selectionMarker.transform.position = location.ToVector3() + Vector3.up * 0.05f;
        //currentMove = null;
    }
    private void CancelMove()
    {
        if (currentMove == null || selectedUnit == null)
            return;

        currentMove.CancelMove();
        movementConnection.gameObject.SetActive(false);
        cursorManager.CursorOn();
        ClearSelection();
    }

    private bool TryGetIMove(out IMove move)
    {
        move = selectedUnit.GetComponent<IMove>();
        return move != null;
    }

    public bool IsValidPlacement(Hex3 location, bool showMessages = true)
    {
        HexTile tile = HexTileManager.GetHexTileAtLocation(location);
        if (tile == null)
            return false;

        if (tile.isPlaceHolder)
            return false;

        if (!_selectedUnit.PlacementListContains(tile.TileType))
        {
            if(showMessages)
                MessagePanel.ShowMessage($"Can not move onto {tile.TileType.ToNiceString()}.", this.gameObject);
            return false;
        }

        if (tile.TryGetComponent(out FogGroundTile fgt) && !fgt.HasBeenRevealed)
        {
            if(showMessages)
                MessagePanel.ShowMessage($"Can not move onto unrevealed terrain.", this.gameObject);
            return false;
        }

        if (UnitManager.TryGetPlayerUnitAtLocation(location, out PlayerUnit playerUnit))
            return false;

        if(ecm.IsCrystalNearBy(location, out EnemyCrystalBehavior nearbyCrystal))
            return false;

        return true;
    }
    #endregion

}
