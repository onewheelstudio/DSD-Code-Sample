using DG.Tweening;
using HexGame.Resources;
using HexGame.Units;
using Nova;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AddUnitButton : MonoBehaviour, IHaveResources
{
    [SerializeField, OnValueChanged("@LoadUnitInfo()")]
    private PlayerUnitType _unitType;
    public PlayerUnitType unitType { get { return _unitType; } }
    private Button button;
    UnitManager um;
    private TextBlock text;
    [SerializeField]
    private bool canPlace = false;
    public bool CanPlace => canPlace;
    [SerializeField] private bool hasLimit = false;
    [SerializeField, ShowIf("@hasLimit")] private int limit = 1;
    [SerializeField, ShowIf("@hasLimit")] private bool limitForTutorial = false;
    private bool isTutorial
    {
        get
        {
            if (SaveLoadManager.Loading || SaveLoadManager.loadedGame)
                return false;

            if(StateOfTheGame.gameStarted && StateOfTheGame.tutorialSkipped)
                return false;

            return true;
        }
    }

    [OnValueChanged("GetButtons")]
    [SerializeField] private List<PlayerUnitType> requiredToUnlock = new List<PlayerUnitType>();
    List<AddUnitButton> requiredButtons;
    private static UnitSelectionManager unitSelectionManager;
    [SerializeField] private UnitImages unitImages;
    [SerializeField] private UIBlock2D iconBlock;
    private BuildingSelectWindow buildingSelectWindow;
    private ClipMask clipMask;

    private Interactable interactable;

    [SerializeField] private Transform buildCostList;
    [SerializeField] private GameObject buildCostPrefab;

    private void Awake()
    {
        clipMask = this.GetComponent<ClipMask>();
        interactable = this.GetComponent<Interactable>();
        button = this.GetComponent<Button>();
        buildingSelectWindow = this.GetComponentInParent<BuildingSelectWindow>();
        um = FindFirstObjectByType<UnitManager>();
        um.RegisterUnitButton(this);
        text = this.GetComponentInChildren<TextBlock>(true);
        interactable.enabled = canPlace;
        this.gameObject.SetActive(canPlace);
        unitSelectionManager ??= FindFirstObjectByType<UnitSelectionManager>();
        LoadUnitInfo();
    }

    private void OnEnable()
    {
        text.Text = _unitType.ToNiceString();
        button.Clicked += AddUnit;
        if (limitForTutorial && isTutorial)
        {
            DayNightManager.toggleDay += TurnOffTutorialLimit;
            StateOfTheGame.TutorialSkipped += TurnOffTutorialLimit;
        }
        else if(limitForTutorial && !isTutorial)
        {
            TurnOffTutorialLimit();
        }
        else if (hasLimit && !limitForTutorial)
            IncreaseLimitUpgrade.OnLimitIncreased += OnLimitIncreased;

        List<ResourceAmount> buildCosts = um.GetUnitCost(unitType);
        if(buildCosts == null || buildCosts.Count == 0)
        {
            buildCosts = new();
            buildCosts.Add(new ResourceAmount(ResourceType.FeOre, 0));
            buildCosts.Add(new ResourceAmount(ResourceType.Energy, 0));
        }
        DisplayBuildCost(buildCosts);

        if(hasLimit)
        {
            UnitManager.unitPlacementStarted += UnitPlaced;
            UnitManager.unitPlaced += UnitChanged;
            UnitManager.unitPlacementFinished += UnitFinished;
            PlayerUnit.unitRemoved += UnitChanged;
        }
    }

    private void OnDisable()
    {
        button.Clicked -= AddUnit;
        if (hasLimit && limitForTutorial && isTutorial)
        {
            DayNightManager.toggleDay -= TurnOffTutorialLimit;
            StateOfTheGame.TutorialSkipped -= TurnOffTutorialLimit;
        }
        else if (hasLimit && !limitForTutorial)
            IncreaseLimitUpgrade.OnLimitIncreased -= OnLimitIncreased;
        DOTween.Kill(this,true);

        if(hasLimit)
        {
            UnitManager.unitPlaced -= UnitChanged;
            UnitManager.unitPlacementStarted -= UnitPlaced;
            UnitManager.unitPlacementFinished -= UnitFinished;
            PlayerUnit.unitRemoved -= UnitChanged;
        }
    }

    private void AddUnit()
    {
        if (hasLimit && GetTotalUnits() >= limit)
        {
            MessagePanel.ShowMessage($"Current limit of {_unitType.ToNiceStringPlural()} reached.", null);
            return;
        }

        if(canPlace || CanUnlock())
        {
            um.SetUnitTypeToAdd(_unitType, CanRepeatPlace);
            unitSelectionManager.ClearSelection();
            buildingSelectWindow.HideWindow();
        }
    }

    private bool CanRepeatPlace()
    {
        return hasLimit ? GetTotalUnits() < limit : true;
    }

    private void UnitChanged(Unit unit)
    {
        if(unit is PlayerUnit playerUnit && playerUnit.unitType == _unitType)
        {
            UnitPlaced(playerUnit.unitType); 
        }
    }
    
    private void UnitPlaced(PlayerUnitType unitType)
    {
        if (!hasLimit)
            return;

        if(unitType == _unitType)
        {
            if(GetTotalUnits() >= limit)
            {
                Color tint = clipMask.Tint;
                tint.a = 0.4f;
                clipMask.Tint = tint;
            }
            else
            {
                Color tint = clipMask.Tint;
                tint.a = 1f;
                clipMask.Tint = tint;
            }    
        }
    }

    private void UnitFinished()
    {
        if (!hasLimit)
            return;

        if (GetTotalUnits() >= limit) //plus one since the unit still exists
        {
            Color tint = clipMask.Tint;
            tint.a = 0.4f;
            clipMask.Tint = tint;
        }
        else
        {
            Color tint = clipMask.Tint;
            tint.a = 1f;
            clipMask.Tint = tint;
        }
    }


    private int GetTotalUnits()
    {
        int buildingSpots = FindObjectsOfType<BuildingSpotBehavior>().Count(bs => bs.GetComponent<BuildingSpotBehavior>().unitTypeToBuild == _unitType); 

        return UnitManager.GetPlayerUnitByType(_unitType).Count + buildingSpots;
    }

    public void SetUnitType(PlayerUnitType unitType)
    {
        this._unitType = unitType;
        text.Text = unitType.ToNiceString().ToUpper();
        this.gameObject.name = $"Add {unitType.ToString().ToUpper()} Button";
    }

    public bool CanUnlock()
    {
        if (requiredButtons == null || requiredButtons.Count == 0)
            return true;
        else
            return requiredButtons.All(x => x.canPlace);
    }

    public void UnlockButton()
    {
        this.canPlace = true;
    }
    public void LockButton()
    {
        this.canPlace = false;
    }

    private void GetButtons()
    {
        requiredButtons = FindObjectsOfType<AddUnitButton>().Where(x => requiredToUnlock.Contains(x.unitType)).ToList();
    }

    private void TurnOffTutorialLimit()
    {
        TurnOffTutorialLimit(0);
    }

    private void TurnOffTutorialLimit(int obj)
    {
        hasLimit = false;
        Color tint = clipMask.Tint;
        tint.a = 1f;
        clipMask.Tint = tint;
        DayNightManager.toggleDay -= TurnOffTutorialLimit;
    }


    private void OnLimitIncreased(PlayerUnitType type, int increase)
    {
        if (this.unitType == type)
            limit += increase;

        if (limit > GetTotalUnits())
        {
            Color tint = clipMask.Tint;
            tint.a = 1f;
            clipMask.Tint = tint;
        }
    }

    public List<PopUpResourceAmount> GetPopUpResources()
    {
        throw new System.NotImplementedException();
    }

    private void LoadUnitInfo()
    {
        InfoToolTip info = this.GetComponent<InfoToolTip>();
        info.SetToolTipInfo(_unitType.ToNiceString(), unitImages.GetPlayerUnitImage(unitType));
        iconBlock.SetImage(unitImages.GetPlayerUnitImage(unitType));
        TextBlock label = this.GetComponentInChildren<TextBlock>(true);
        label.Text = _unitType.ToNiceString();
        this.gameObject.name = unitType.ToString();
    }

    private void DisplayBuildCost(List<ResourceAmount> buildCosts)
    {
        foreach (ResourceAmount resource in buildCosts)
        {
            ResourceTemplate resourceTemplate = GameObject.FindObjectOfType<PlayerResources>().GetResourceTemplate(resource.type);
            GameObject go = Instantiate(buildCostPrefab, buildCostList.transform);
            ItemView itemView = go.GetComponent<ItemView>();

            UnitInfoButtonVisuals visuals = (UnitInfoButtonVisuals)itemView.Visuals;
            visuals.icon.SetImage(resourceTemplate.icon);
            visuals.label.Text = $"{resource.amount}";
            visuals.infoToolTip.SetToolTipInfo(resourceTemplate.type.ToNiceString(), resourceTemplate.icon, "");
            visuals.icon.Color = resourceTemplate.resourceColor;
        }
    }
}
