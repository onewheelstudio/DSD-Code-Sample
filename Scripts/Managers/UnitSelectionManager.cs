using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using Nova;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnitSelectionManager : MonoBehaviour
{
    [SerializeField] private GameObject selectionMarker;
    static private PlayerUnit _selectedUnit;
    private static IMove currentMove;
    public static PlayerUnit selectedUnit => _selectedUnit;
    public static PlayerUnitType selectedUnitType => _selectedUnit.unitType;
    public static event Action<PlayerUnit> unitSelected;
    public static event Action<PlayerUnit> unitUnSelected;

    private UIControlActions uiControls;
    public static bool addConnection = false;
    public static event Action<PlayerUnit> unitClicked;
    private HexTileManager htm;
    private CursorManager cursorManager;


    [Header("Special Bits")]
    [SerializeField] private UnitMovementConnection movementConnection;

    private void Awake()
    {
        uiControls = new UIControlActions();
        htm = FindObjectOfType<HexTileManager>();   
        cursorManager = FindObjectOfType<CursorManager>();
    }

    private void OnEnable()
    {
        uiControls.UI.RightClick.canceled += RightClick;
        uiControls.UI.LeftClick.performed += LeftClick;

        uiControls.UI.MoveUnit.performed += ToggleMove;
        uiControls.Enable();
        UnitInfoWindow.moveButtonClicked += ToggleUnitReadyToMove;
        GroupControlManager.MoveToUnit += UnitSelected;
        ControlsManager.UIControlsUpdated += UpdateMovementBindings;
    }

    private void OnDisable()
    {
        uiControls.UI.RightClick.canceled -= RightClick;
        uiControls.UI.LeftClick.performed -= LeftClick;

        uiControls.UI.MoveUnit.performed -= ToggleMove;
        uiControls.Enable();

        UnitInfoWindow.moveButtonClicked -= ToggleUnitReadyToMove;
        GroupControlManager.MoveToUnit -= UnitSelected;
        ControlsManager.UIControlsUpdated -= UpdateMovementBindings;
    }

    private void UpdateMovementBindings(string rebinds)
    {
        uiControls.LoadBindingOverridesFromJson(rebinds);
    }

    private void Update()
    {
        if(movementConnection.gameObject.activeInHierarchy && currentMove != null)
        {
            Hex3 mouseLocation = HelperFunctions.GetMouseHex3OnPlane();

            movementConnection.SetStatus(IsValidPlacement(mouseLocation, false) && !currentMove.UnitsAreMoving);
            if(mouseLocation != movementConnection.destination.ToHex3())
            {
                movementConnection.SetPositions(selectedUnit.transform.position, mouseLocation);
            }
        }
        else if(currentMove == null && movementConnection.gameObject.activeInHierarchy)
        {
            movementConnection.gameObject.SetActive(false);
        }
    }

    private void LeftClick(InputAction.CallbackContext context)
    {
        if (PCInputManager.MouseOverVisibleUIObject())
            return;

        if (addConnection && UnitManager.PlayerUnitAtMouseLocation())
            UnitClickedOn();
        else if (Keyboard.current.shiftKey.isPressed && UnitManager.PlayerUnitAtMouseLocation() && selectedUnit != null)
            AddConnectionToSelected();
        else if (UnitManager.PlayerUnitAtMouseLocation())
            SelectUnit();
        else if (currentMove != null)
            DoMove(context);
    }


    private void RightClick(InputAction.CallbackContext context)
    {
        if (PlaceHolderAtLocation(out HexTile hexTile))
            hexTile.GetComponent<PlaceHolderTileBehavior>().RemovePlaceHolder();
        else if (_selectedUnit == null)
            return;
        else if (Keyboard.current.shiftKey.isPressed && UnitManager.PlayerUnitAtMouseLocation() && selectedUnit != null)
            RemoveConnectionFromSelected();
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
        if (UnitManager.TryGetAllPlayerUnitsAtLocation(HelperFunctions.GetMouseHex3OnPlane(), out List<PlayerUnit> playerUnits))
        {
            unitClicked?.Invoke(playerUnits.FirstOrDefault(x => x.unitType != PlayerUnitType.cargoShuttle));
        }
    }

    private void AddConnectionToSelected()
    {
        if (UnitManager.TryGetAllPlayerUnitsAtLocation(HelperFunctions.GetMouseHex3OnPlane(), out List<PlayerUnit> playerUnits))
        {
            PlayerUnit playerUnit = playerUnits.FirstOrDefault(x => x.unitType != PlayerUnitType.cargoShuttle);
            if(playerUnit == null)
                return;

            selectedUnit.GetComponent<UnitStorageBehavior>().AddDeliverConnection(playerUnit.GetComponent<UnitStorageBehavior>());
            SFXManager.PlaySFX(SFXType.click);
        }
    }
    
    private void RemoveConnectionFromSelected()
    {
        if (UnitManager.TryGetAllPlayerUnitsAtLocation(HelperFunctions.GetMouseHex3OnPlane(), out List<PlayerUnit> playerUnits))
        {
            PlayerUnit playerUnit = playerUnits.FirstOrDefault(x => x.unitType != PlayerUnitType.cargoShuttle);
            if(playerUnit == null)
                return;

            selectedUnit.GetComponent<UnitStorageBehavior>().RemoveConnectionFromList(playerUnit.GetComponent<UnitStorageBehavior>());
            SFXManager.PlaySFX(SFXType.click);
        }
    }


    private void SelectUnit()
    {
        if(UnitManager.TryGetAllPlayerUnitsAtLocation(HelperFunctions.GetMouseHex3OnPlane(), out List<PlayerUnit> playerUnits))
        {
            UnitSelected(playerUnits.FirstOrDefault(x => x.unitType != PlayerUnitType.cargoShuttle).gameObject);
        }
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

    public void SetUnitSelected(GameObject playerUnit)
    {
        UnitSelected(playerUnit);
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

        movementConnection.gameObject.SetActive(false);
        currentMove.DoMove(location);
        cursorManager.CursorOn();
        selectionMarker.transform.position = location.ToVector3() + Vector3.up * 0.05f;
        currentMove = null;
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

        return true;
    }
    #endregion

}
