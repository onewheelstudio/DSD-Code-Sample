using HexGame.Grid;
using HexGame.Units;
using Nova;
using NovaSamples.UIControls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class GroupControlManager : MonoBehaviour, ISaveData
{
    private UIControlActions inputAction;
    private GroupInfo[] groups = new GroupInfo[5];
    private Transform cameraTransform;
    private CameraMovement cameraMovement;
    public static event Action<Vector3> MoveToGroup;
    public static event Action<Transform> MoveToUnit;

    [SerializeField] private Button[] buttons = new Button[5];
    private int currentGroup = 0;
    private UnitSelectionManager usm;


    private void Awake()
    {
        usm = FindFirstObjectByType<UnitSelectionManager>();
        inputAction = new UIControlActions();
        cameraMovement = FindFirstObjectByType<CameraMovement>();
        cameraTransform = cameraMovement.transform;

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].GetComponent<ClipMask>().Tint = ColorManager.GetColor(ColorCode.buttonGreyOut);
        }

        RegisterDataSaving();
    }

    private void OnEnable()
    {
        inputAction.UI.Group1.performed += ToggleGroup1;
        inputAction.UI.Group1.Enable();
        inputAction.UI.Group2.performed += ToggleGroup2;
        inputAction.UI.Group2.Enable();
        inputAction.UI.Group3.performed += ToggleGroup3;
        inputAction.UI.Group3.Enable();
        inputAction.UI.Group4.performed += ToggleGroup4;
        inputAction.UI.Group4.Enable();
        inputAction.UI.Group5.performed += ToggleGroup5;
        inputAction.UI.Group5.Enable();

        inputAction.UI.TabGroup.performed += NextGroup;
        inputAction.UI.TabGroup.Enable();

        buttons[0].OnClicked.AddListener(() => GroupPressed(0));
        buttons[1].OnClicked.AddListener(() => GroupPressed(1));
        buttons[2].OnClicked.AddListener(() => GroupPressed(2));
        buttons[3].OnClicked.AddListener(() => GroupPressed(3));
        buttons[4].OnClicked.AddListener(() => GroupPressed(4));

        ControlsManager.UIControlsUpdated += UpdateGroupBindings;
    }



    private void OnDisable()
    {
        inputAction.UI.Group1.Disable();
        inputAction.UI.Group2.Disable();
        inputAction.UI.Group3.Disable();
        inputAction.UI.Group4.Disable();
        inputAction.UI.Group5.Disable();

        inputAction.UI.TabGroup.Disable();

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].OnClicked.RemoveAllListeners();
        }

        ControlsManager.UIControlsUpdated -= UpdateGroupBindings;
    }

    private void UpdateGroupBindings(string rebinds)
    {
        inputAction.LoadBindingOverridesFromJson(rebinds);
    }

    private void ToggleGroup5(InputAction.CallbackContext context)
    {
        GroupPressed(4);
    }

    private void ToggleGroup4(InputAction.CallbackContext context)
    {
        GroupPressed(3);
    }

    private void ToggleGroup3(InputAction.CallbackContext context)
    {
        GroupPressed(2);
    }

    private void ToggleGroup2(InputAction.CallbackContext context)
    {
        GroupPressed(1);
    }

    private void ToggleGroup1(InputAction.CallbackContext context)
    {
        GroupPressed(0);
    }

    private void GroupPressed(int groupNumber)
    {
        //a bit janky, but prevents group selection if the feedback window is open
        if (WindowPopup.BlockWindowHotkeys)
            return;

        currentGroup = groupNumber;
        if(Keyboard.current.shiftKey.isPressed)
        {
            ClearGroup(groupNumber);
            buttons[groupNumber].GetComponent<ClipMask>().Tint = ColorManager.GetColor(ColorCode.buttonGreyOut);
        }
        else if (Keyboard.current.ctrlKey.isPressed)
        {
            SetGroupInfo(groupNumber);
            buttons[groupNumber].GetComponent<ClipMask>().Tint = Color.white;
        }
        else
        {
            ZoomToGroup(groupNumber);
        }
    }

    private void ClearGroup(int groupNumber)
    {
        groups[groupNumber] = new GroupInfo();
    }

    private void ZoomToGroup(int groupNumber)
    {
        GroupInfo group = groups[groupNumber];

        if (group == null)
            return; 

        if(group.unit != null)
        {
            if (UnitSelectionManager.selectedUnit == null  || UnitSelectionManager.selectedUnit.gameObject != group.unit.gameObject)
            {
                usm.SetUnitSelected(group.unit.gameObject);
                return;
            }
        }

        Vector3? location = group.Location;
        if(location != null)
            MoveToGroup?.Invoke((Vector3)group.Location);
        MoveToUnit?.Invoke(group.unit);
    }

    private void SetGroupInfo(int groupNumber)
    {
        if(UnitSelectionManager.selectedUnit != null)
        {
            groups[groupNumber] = new GroupInfo(UnitSelectionManager.selectedUnit.transform);
            MessagePanel.ShowMessage($"Location {groupNumber + 1} set to {UnitSelectionManager.selectedUnit.unitType.ToNiceString()}", null); 
        }
        else
        {
            groups[groupNumber] = new GroupInfo(cameraTransform.position);
            MessagePanel.ShowMessage($"Location {groupNumber + 1} postion set to {cameraTransform.position.ToHex3().StringCoordinates()}", null);
        }
    }

    private void NextGroup(InputAction.CallbackContext context)
    {
        //a bit janky, but prevents group selection if the feedback window is open
        if (WindowPopup.BlockWindowHotkeys)
            return;

        NextGroup(currentGroup);
    }

    private void NextGroup(int currentGroup)
    {
        //do any groups have valid locations?
        if (!groups.Any(g => g != null && g.Location != null))
            return;

        currentGroup++;
        if (currentGroup >= groups.Length)
            currentGroup = 0;

        //does the next group have a location?
        if(groups[currentGroup] == null || groups[currentGroup].Location == null)
        {
            NextGroup(currentGroup);
            return;
        }

        //update and zoom
        this.currentGroup = currentGroup;

        Vector3? location = groups[currentGroup].Location;
        if (location != null)
            MoveToGroup?.Invoke((Vector3)groups[currentGroup].Location);
        MoveToUnit?.Invoke(groups[currentGroup].unit);
    }

    private const string GROUP_DATA = "GroupControlManager";
    public void RegisterDataSaving()
    {
        //load after units have loaded
        SaveLoadManager.RegisterData(this, 5);
    }

    public void Save(string savePath, ES3Writer writer)
    {
        List<GroupData> locations = new List<GroupData>();
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] == null)
                continue;

            locations.Add(new GroupData
            {
                Location = (Vector3)groups[i].Location,
                GroupNumber = i
            });
        }
        
        writer.Write<List<GroupData>>(GROUP_DATA, locations);
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if(ES3.KeyExists(GROUP_DATA, loadPath))
        {
            UnitManager unitManager = FindFirstObjectByType<UnitManager>();
            List<GroupData> locations = ES3.Load<List<GroupData>>(GROUP_DATA, loadPath);
            for (int i = 0; i < groups.Length; i++)
            {
                buttons[i].GetComponent<ClipMask>().Tint = ColorManager.GetColor(ColorCode.buttonGreyOut);
            }

            yield return null;

            for (int j = 0; j < locations.Count; j++)
            { 
                buttons[locations[j].GroupNumber].GetComponent<ClipMask>().Tint = Color.white;

                Hex3 location = locations[j].Location.ToHex3();

                if(UnitManager.TryGetPlayerUnitAtLocation(location, out PlayerUnit unit))
                    groups[locations[j].GroupNumber] = new GroupInfo(unit.transform);
                else
                    groups[locations[j].GroupNumber] = new GroupInfo(locations[j].Location);
            }
        }
        yield return null;
    }

    public struct GroupData
    {
        public Vector3 Location;
        public int GroupNumber;
    }

    public class GroupInfo
    {
        public Vector3? Location
        {
            get
            {
                if (unit != null)
                    return unit.position;
                else
                    return location;
            }
        }
        private Vector3? location;
        public Transform unit;

        public GroupInfo(Vector3 location)
        {
            this.location = location;
        }
        public GroupInfo(Transform unit)
        {
            this.unit = unit;
        }

        public GroupInfo()
        {
        }
    }
}
