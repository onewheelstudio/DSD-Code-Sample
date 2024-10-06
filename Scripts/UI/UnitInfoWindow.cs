using HexGame.Resources;
using HexGame.Units;
using Nova;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnitInfoWindow : WindowPopup // IPointerExitHandler, ICancelHandler, IPointerEnterHandler
{

    [Header("Settings")]
    [SerializeField] private bool moveToMouse = false;
    private UIBlock2D parentBlock;

    [SerializeField, Required]
    private TextBlock unitName;
    [SerializeField] private UIBlock2D unitImage;

    [Header("Unit Info")]
    [SerializeField, Required]
    private GameObject unitInfoButtonPrefab;
    [SerializeField, Required]
    private ListView statsContainer;
    [SerializeField, Required]
    private ListView resourceContainer;
    [SerializeField] private UIBlock2D healthBar;
    [SerializeField] private TextBlock healthText;
    [SerializeField] private GameObject shieldParent;
    [SerializeField] private UIBlock2D shieldBar;
    [SerializeField] private TextBlock shieldText;


    [Header("Action Buttons")]
    [SerializeField] private Button moveButton;
    public static event Action moveButtonClicked;
    [SerializeField] private Button addUnitButton;
    [SerializeField] private Button removeUnitButton;
    [SerializeField] private Button deleteButton;

    [Header("Active Indicator")]
    [SerializeField] private Button activityButton;
    [SerializeField] private UIBlock2D activeIndicatorUIBlock;
    [SerializeField] private Color activeColor;
    [SerializeField] private Color inactiveColor;

    [Header("Priority")]
    [SerializeField] private Button priorityButton;
    [SerializeField] private UIBlock2D priorityButtonImage;
    [SerializeField] private Texture2D offPrioritySprite;
    [SerializeField] private Texture2D lowPrioritySprite;
    [SerializeField] private Texture2D mediumPrioritySprite;
    [SerializeField] private Texture2D highPrioritySprite;
    public static event Action prorityChanged;

    [Header("Production")]
    [SerializeField]private GameObject productionPanel;
    [SerializeField] private Dropdown productionDropdown;
    [SerializeField] private ListView recipeRequirements;
    [SerializeField] private ListView recipeResults;
    [SerializeField] private TextBlock efficiencyText;
    [SerializeField] private UIBlock2D timerBlock;

    [Header("Preferred Delivery")]
    [SerializeField] private UIBlock preferredDeliveryPanel;
    [SerializeField] private ListView preferredDeliveryList;
    [SerializeField] private Button addPreferredDeliveryButton;
    [SerializeField] private UIBlock2D addConnectionBlock;
    [SerializeField] private UIBlock2D addingConnectionIcon;
    [SerializeField] private Color addingConnectionColor;
    private Color startingConnectionButtonColor;

    private CargoManager.RequestPriority priority;

    [Header("Tool Tip Placemenet")]
    [SerializeField]
    [Range(-200, 200)]
    private float xOffset = 0;
    [SerializeField]
    [Range(-200, 200)]
    private float yOffset = 0;
    public static UnitToolTip toolTipObject;

    private bool mouseIsOver = false;
    private float closeDelay = 0.35f;

    private RequestStorageInfo requestPriority;
    private Vector2 canvasResolution;
    private ScreenSpace screenSpace;
    private float canvasScale => Screen.width / screenSpace.ReferenceResolution.x;

    private PlayerResources playerResources;
    private StatsManager statsManager;
    private UnitManager unitManager;
    private UnitStorageBehavior currrentUnitStorage;
    private UnitSelectionManager unitSelectionManager;
    [SerializeField] private ColorData colorData;

    [SerializeField]
    private Camera uiCamera;

    [Header("Info SOs")]
    [SerializeField] private UnitImages unitImages;
    [SerializeField] private StatsInfo statsInfo;

    //cached values
    private List<PopUpStats> popUpStats;
    private List<PopUpResource> popUpResources;

    private Camera Camera;

    private void Awake()
    {
        screenSpace = GameObject.FindObjectOfType<ScreenSpace>();

        playerResources = GameObject.FindObjectOfType<PlayerResources>();
        statsManager = GameObject.FindObjectOfType<StatsManager>();
        unitManager = GameObject.FindObjectOfType<UnitManager>();
        unitSelectionManager = FindFirstObjectByType<UnitSelectionManager>();
        parentBlock = this.GetComponent<UIBlock2D>();

        startingConnectionButtonColor = addConnectionBlock.Border.Color;

        Camera = Camera.main;
    }

    private void Start()
    {
        statsContainer.AddDataBinder<PopUpStats, UnitInfoButtonVisuals>(BindStats);
        resourceContainer.AddDataBinder<ResourceAmount, UnitInfoProgressBarVisuals>(BindResources);
        recipeRequirements.AddDataBinder<ResourceAmount, UnitInfoButtonVisuals>(BindResources);
        recipeResults.AddDataBinder<ResourceAmount, UnitInfoButtonVisuals>(BindResources);
        preferredDeliveryList.AddDataBinder<UnitStorageBehavior, PreferredDeliveryVisual>(BindPreferredDelivery);
    }

    private new void OnEnable()
    {
        base.OnEnable();
        UnitToolTip.openToolTip += OpenToolTip;
        UnitToolTip.closeToolTip += CloseWindow;

        UnitToolTip.updateToolTip += PopulateToolTip;
        UnitToolTip.updateInfo += PopulateInfo;
        UnitToolTip.updateButtons += PopulateButtons;
        UnitToolTip.updateToggle += PopulateToggle;
        UnitToolTip.updatePriority += PopulatePriorityInfo;

        UnitToolTip.updateStorage += SetupPreferredDelivery;
        UnitStorageBehavior.startListeningForConnection += AddButtonOn;
        UnitStorageBehavior.stopListeningForConnection += AddButtonOff;

        UnitToolTip.updateStats += PopulateStats;
        UnitToolTip.updateResources += PopulateResources;

        UnitToolTip.updateRecipe += SetUpReceipes;

        UIBlock2D block = this.GetComponent<UIBlock2D>();
        block.AddGestureHandler<Gesture.OnUnhover, PopUpVisuals>(OnEndHover);
        block.AddGestureHandler<Gesture.OnHover, PopUpVisuals>(OnStartHover);

        CloseWindow();
    }

    private new void OnDisable()
    {
        base.OnDisable();
        UnitToolTip.openToolTip -= OpenToolTip;
        UnitToolTip.closeToolTip -= CloseWindow;

        UnitToolTip.updateToolTip -= PopulateToolTip;
        UnitToolTip.updateInfo -= PopulateInfo; 
        UnitToolTip.updateButtons -= PopulateButtons;
        UnitToolTip.updateToggle -= PopulateToggle;
        UnitToolTip.updatePriority -= PopulatePriorityInfo;

        UnitToolTip.updateStorage -= SetupPreferredDelivery;
        UnitStorageBehavior.startListeningForConnection -= AddButtonOn;
        UnitStorageBehavior.stopListeningForConnection -= AddButtonOff;

        UnitToolTip.updateStats -= PopulateStats;
        UnitToolTip.updateResources -= PopulateResources;

        UnitToolTip.updateRecipe -= SetUpReceipes;

        UIBlock2D block = this.GetComponent<UIBlock2D>();
        block.RemoveGestureHandler<Gesture.OnUnhover, PopUpVisuals>(OnEndHover);
        block.RemoveGestureHandler<Gesture.OnHover, PopUpVisuals>(OnStartHover);
    }



    private void OpenToolTip(List<PopUpInfo> popUpInfos, List<PopUpPriorityButton> popUpButtons, PopUpCanToggle popUpCanToggle, RequestStorageInfo requestStorageInfo, ReceipeInfo receipeInfo, List<PopUpButtonInfo> buttonInfo, UnitToolTip toolTip)
    {
        if (toolTipObject != null && toolTipObject == toolTip)
            return;

        PopulateToolTip(popUpInfos, popUpButtons, popUpCanToggle, requestStorageInfo, receipeInfo, buttonInfo, toolTip);
        
        OpenWindow();
        //UIBlock2D block = this.GetComponent<UIBlock2D>();
        //block.Position.Y = (Mouse.current.position.ReadValue().y + TooltipOffset().y * block.Size.Y.Value) / canvasScale;
        //block.Position.X = (Mouse.current.position.ReadValue().x + TooltipOffset().x * block.Size.X.Value) / canvasScale;
        //this.transform.position = (GetMousePosition() + TooltipOffset()) / canvasScale;
    }

    private void PopulateToolTip(List<PopUpInfo> popUpInfos, List<PopUpPriorityButton> popUpButtons, PopUpCanToggle popUpCanToggle, RequestStorageInfo requestStorageInfo, ReceipeInfo receipeInfo, List<PopUpButtonInfo> buttonInfo, UnitToolTip toolTip)
    {
        toolTipObject = toolTip;

        SetUnitImage(popUpInfos);
        SetUpText(popUpInfos);
        SetUpPriorityButton(popUpButtons);
        SetupActiveIndicator(popUpCanToggle);
        SetUpPriorityButton(requestStorageInfo);
        SetupPreferredDelivery(receipeInfo, requestStorageInfo, toolTip);
        SetUpReceipes(receipeInfo, toolTip);
        SetUpButtons(buttonInfo);
    }

    private void SetUnitImage(List<PopUpInfo> popUpInfos)
    {
        PlayerUnitType unitType = (PlayerUnitType)popUpInfos.FirstOrDefault(x => x.infoType == PopUpInfo.PopUpInfoType.name 
                                                                            && x.objectInfo != (int)PlayerUnitType.cargoShuttle).objectInfo;
        if (unitType == PlayerUnitType.buildingSpot)
            unitType = (PlayerUnitType)popUpInfos.FirstOrDefault(x => x.infoType == PopUpInfo.PopUpInfoType.name 
                                                                 && x.objectInfo != (int)PlayerUnitType.buildingSpot).objectInfo;

        unitImage.SetImage(unitImages.GetPlayerUnitImage(unitType));
    }

    private void PopulateInfo(List<PopUpInfo> popUpInfos, UnitToolTip toolTip)
    {
        toolTipObject = toolTip;
        SetUpText(popUpInfos);
    }

    private void PopulateButtons(List<PopUpPriorityButton> popUpButtons, UnitToolTip toolTip)
    {
        toolTipObject = toolTip;
        SetUpPriorityButton(popUpButtons);
    }

    private void PopulateToggle(PopUpCanToggle popUpCanToggle, UnitToolTip toolTip)
    {
        toolTipObject = toolTip;
        SetupActiveIndicator(popUpCanToggle);
    }

    private void PopulatePriorityInfo(RequestStorageInfo requestStorageInfo, UnitToolTip toolTip)
    {
        toolTipObject = toolTip;
        SetUpPriorityButton(requestStorageInfo);
    }

    private void SetupPreferredDelivery(ReceipeInfo receipeInfo, RequestStorageInfo requestStorageInfo, UnitToolTip toolTip)
    {
        if(UnitSelectionManager.selectedUnit.unitType == PlayerUnitType.infantry)
            preferredDeliveryPanel.gameObject.SetActive(false);
        else
            preferredDeliveryPanel.gameObject.SetActive(true);

        if (UnitSelectionManager.selectedUnit == null)
        {
            currrentUnitStorage = null;
            return;
        }

        currrentUnitStorage = UnitSelectionManager.selectedUnit.GetComponent<UnitStorageBehavior>();

        toolTipObject = toolTip;
        preferredDeliveryList.SetDataSource(requestStorageInfo.connections); // need to send in the USB not just the list of connections
        addPreferredDeliveryButton.RemoveClickListeners();
        addPreferredDeliveryButton.clicked += requestStorageInfo.startAddConnection;
    }
    private void BindPreferredDelivery(Data.OnBind<UnitStorageBehavior> evt, PreferredDeliveryVisual target, int index)
    {
        target.label.Text = evt.UserData.GetComponent<PlayerUnit>().unitType.ToNiceString();
        target.upArrow.RemoveAllListeners();
        target.downArrow.RemoveAllListeners();
        target.deleteButton.RemoveAllListeners();

        target.upArrow.clicked += () => currrentUnitStorage.MoveConnectionUp(index);
        target.downArrow.clicked += () => currrentUnitStorage.MoveConnectionDown(index);
        target.deleteButton.clicked += () => currrentUnitStorage.RemoveConnection(index);
    }

    private void AddButtonOff(UnitStorageBehavior behavior)
    {
        addConnectionBlock.Border.Color = startingConnectionButtonColor;
        addingConnectionIcon.Color = Color.white;
    }

    private void AddButtonOn(UnitStorageBehavior behavior)
    {
        addConnectionBlock.Border.Color = addingConnectionColor;
        addingConnectionIcon.Color = addingConnectionColor;
    }


    private void PopulateResources(List<PopUpResource> list, UnitToolTip tip)
    {
        if(!tip.ShowInventory)
        {
            resourceContainer.gameObject.SetActive(false);
            return;
        }

        resourceContainer.gameObject.SetActive(list.Count > 0);

        if (list.Count == 0)
            return;

        if(popUpResources != null && Enumerable.SequenceEqual(list, popUpResources))
            return;

        popUpResources = list;

        List<ResourceAmount> resources = new List<ResourceAmount>();
        foreach (var resource in list)
        {
            if(resource.resource.type == ResourceType.Workers)
                resources.Insert(0, resource.resource);
            else
                resources.Add(resource.resource);
        }

        //make sure that all build costs are shown
        if (UnitSelectionManager.selectedUnitType == PlayerUnitType.buildingSpot)
        {
            PlayerUnitType typeToBuild = UnitSelectionManager.selectedUnit.GetComponent<BuildingSpotBehavior>().unitTypeToBuild;
            List<ResourceAmount> buildCost = unitManager.GetUnitCost(typeToBuild);

            if (buildCost.Count != resources.Count)
            {
                foreach (var resource in buildCost)
                {
                    if (!resources.Any(x => x.type == resource.type))
                        resources.Add(new ResourceAmount(resource.type, 0));
                }
            }
        }

        resourceContainer.SetDataSource(resources);
    }

    private void BindResources(Data.OnBind<ResourceAmount> evt, UnitInfoButtonVisuals target, int index)
    {
        target.icon.SetImage(playerResources.GetResourceTemplate(evt.UserData.type).icon);
        target.icon.Color = playerResources.GetResourceTemplate(evt.UserData.type).resourceColor;
        target.infoToolTip.SetToolTipInfo(evt.UserData.type.ToNiceString(), playerResources.GetResourceTemplate(evt.UserData.type).icon,"");

        if (UnitSelectionManager.selectedUnitType == PlayerUnitType.buildingSpot)
        {
            PlayerUnitType typeToBuild = UnitSelectionManager.selectedUnit.GetComponent<BuildingSpotBehavior>().unitTypeToBuild;
            target.label.Text = $"{evt.UserData.amount}/{unitManager.GetUnitCost(typeToBuild, evt.UserData.type).amount}";
        }
        else
            target.label.Text = evt.UserData.amount.ToString();
    }

    private void BindResources(Data.OnBind<ResourceAmount> evt, UnitInfoProgressBarVisuals target, int index)
    {
        target.icon.SetImage(playerResources.GetResourceTemplate(evt.UserData.type).icon);
        target.icon.Color = playerResources.GetResourceTemplate(evt.UserData.type).resourceColor;
        target.sliderFill.Color = playerResources.GetResourceTemplate(evt.UserData.type).resourceColor;
        target.infoToolTip.SetToolTipInfo(evt.UserData.type.ToNiceString(), playerResources.GetResourceTemplate(evt.UserData.type).icon, "");
        target.label.Text = evt.UserData.amount.ToString();

        if (UnitSelectionManager.selectedUnitType == PlayerUnitType.buildingSpot)
        {
            PlayerUnitType typeToBuild = UnitSelectionManager.selectedUnit.GetComponent<BuildingSpotBehavior>().unitTypeToBuild;
            float percent = Mathf.Clamp01((float)evt.UserData.amount / (float)unitManager.GetUnitCost(typeToBuild, evt.UserData.type).amount);
            target.sliderFill.Size.Percent = new Vector2(percent, 1f);
        }
        else if(evt.UserData.type == ResourceType.Workers)
        {
            float percent = Mathf.Clamp01((float)evt.UserData.amount / UnitSelectionManager.selectedUnit.GetStat(Stat.workers));
            target.sliderFill.Size.Percent = new Vector2(percent, 1f);
        }
        else
        {
            float percent = Mathf.Clamp01((float)evt.UserData.amount / UnitSelectionManager.selectedUnit.GetStat(Stat.maxStorage));
            target.sliderFill.Size.Percent = new Vector2(percent, 1f);
        }
    }

    private void PopulateStats(List<PopUpStats> list, UnitToolTip tip)
    {
        float maxHealth = (int)UnitSelectionManager.selectedUnit.GetStat(Stat.hitPoints);
        float currentHealth = (int)UnitSelectionManager.selectedUnit.GetHP();
        healthBar.Size.X.Percent = Mathf.Clamp01(currentHealth / maxHealth);
        healthText.Text = $"{currentHealth}";// / {maxHealth}";

        float maxShield = (int)UnitSelectionManager.selectedUnit.GetStat(Stat.shield);
        shieldParent.gameObject.SetActive(maxShield > 0f);
        float currentShield = (int)UnitSelectionManager.selectedUnit.GetShield();
        shieldBar.Size.X.Percent = Mathf.Clamp01(currentShield / maxShield);
        shieldText.Text = $"{currentShield}";// / {maxHealth}";

        if (popUpStats != null && Enumerable.SequenceEqual(popUpStats, list))
            return;
        popUpStats = list;

        List<PopUpStats> tempList = new List<PopUpStats>(list);
        for(int i = tempList.Count - 1; i >= 0; i--)
        {
            if (tempList[i].stat == Stat.hitPoints || tempList[i].stat == Stat.shield)
                tempList.RemoveAt(i);
        }

        statsContainer.SetDataSource(tempList);
    }

    private void BindStats(Data.OnBind<PopUpStats> evt, UnitInfoButtonVisuals target, int index)
    {
        StatsInfo.StatInfo info = statsInfo.GetStatInfo(evt.UserData.stat);
        target.icon.SetImage(info.icon);
        target.icon.Color = evt.UserData.color;
        target.label.Color = evt.UserData.color;
        float value = evt.UserData.value;
        if(value < 1)
            target.label.Text = evt.UserData.value.ToString("n2");
        else
            target.label.Text = evt.UserData.value.ToString();

        target.infoToolTip.SetToolTipInfo(info.stat.ToNiceString(), info.icon, info.description);
    }

    private void SetUpReceipes(ReceipeInfo receipeInfo, UnitToolTip toolTip)
    {
        if (receipeInfo.receipes == null || receipeInfo.receipes.Count == 0)
        {
            productionPanel.gameObject.SetActive(false);
            return;
        }

        productionPanel.gameObject.SetActive(true);
        //intentionally setting rather than adding to the action
        productionDropdown.ValueChanged = receipeInfo.receipeOwner.SetReceipe;
        productionDropdown.ValueChanged += toolTip.UpdateRecipes;
        List<string> recipes = receipeInfo.receipes.Select(x => x.niceName).ToList();
        productionDropdown.SetOptions(recipes);

        productionDropdown.DropdownData.SelectedIndex = receipeInfo.currentRecipe;

        recipeRequirements.SetDataSource(receipeInfo.receipes[receipeInfo.currentRecipe].GetCost());
        recipeResults.SetDataSource(receipeInfo.receipes[receipeInfo.currentRecipe].GetProduction());

        int efficiency = Mathf.RoundToInt(receipeInfo.efficiency);
        //string efficiencyString = $"Efficiency: {Mathf.RoundToInt(receipeInfo.efficiency)}%";
        string timeToProduceString;
        if (receipeInfo.timeToProduce <= 0 || receipeInfo.timeToProduce > 1000)
            timeToProduceString = "Time: ---";
        else
            timeToProduceString = $"Time: {Mathf.RoundToInt(receipeInfo.timeToProduce * 10) / 10f}s";

        if (efficiency < 30)
            timeToProduceString = TMPHelper.Color(timeToProduceString, colorData.GetColor(ColorCode.red));
        else if (efficiency < 99)
            timeToProduceString = TMPHelper.Color(timeToProduceString, colorData.GetColor(ColorCode.yellow));
        else if(efficiency > 100)
            timeToProduceString = TMPHelper.Color(timeToProduceString, colorData.GetColor(ColorCode.blue));
        else
            timeToProduceString = TMPHelper.Color(timeToProduceString, Color.white);

        efficiencyText.Text = timeToProduceString;
        float timeLeft = Time.time - receipeInfo.receipeOwner.GetStartTime();
        timerBlock.RadialFill.FillAngle = 360f * (receipeInfo.timeToProduce - timeLeft) / receipeInfo.timeToProduce;
    }

    private void SetUpPriorityButton(RequestStorageInfo requestPriority)
    {
        if (requestPriority.setPriority == null)
            priorityButtonImage.gameObject.SetActive(false);
        else
            priorityButtonImage.gameObject.SetActive(true);

        priorityButton.RemoveAllListeners(); //this is important. dumb ass.
        priorityButton.clicked += () => TogglePriority(requestPriority);
        //priorityButton.clicked += () => requestPriority.setPriority(priority);
        this.requestPriority = requestPriority;
        SetPriorityDisplay(requestPriority);

    }
    private void SetOffPriority(InputAction.CallbackContext obj)
    {
        requestPriority.setPriority(CargoManager.RequestPriority.off);
    }
    private void SetLowPriority(InputAction.CallbackContext obj)
    {
        requestPriority.setPriority(CargoManager.RequestPriority.off);

    }
    private void SetMediumPriority(InputAction.CallbackContext obj)
    {
        requestPriority.setPriority(CargoManager.RequestPriority.off);
    }
    private void SetHighPriority(InputAction.CallbackContext obj)
    {
        priorityButton.clicked += () => requestPriority.setPriority(CargoManager.RequestPriority.off);

    }
    private void TogglePriority(RequestStorageInfo requestPriority)
    {
        //toggle through enum and roll over when getting to the end of the enum
        requestPriority.priority++;
        if ((int)requestPriority.priority > System.Enum.GetValues(typeof(HexGame.Resources.CargoManager.RequestPriority)).Length - 1)
            requestPriority.priority = (CargoManager.RequestPriority)0;
        this.priority = requestPriority.priority;
        requestPriority.setPriority(requestPriority.priority);

        SetPriorityDisplay(requestPriority);
        prorityChanged?.Invoke();
        MessagePanel.ShowMessage($"{UnitSelectionManager.selectedUnit.unitType.ToNiceString()} priority set to {requestPriority.priority}", toolTipObject.gameObject);
    }

    private void SetPriorityDisplay(RequestStorageInfo requestPriority)
    {
        switch (requestPriority.priority)
        {
            case CargoManager.RequestPriority.off:
                priorityButtonImage.SetImage(offPrioritySprite);
                priorityButtonImage.Color = colorData.GetColor(ColorCode.offPriority);
                break;
            case CargoManager.RequestPriority.low:
                priorityButtonImage.SetImage(lowPrioritySprite);
                priorityButtonImage.Color = colorData.GetColor(ColorCode.lowPriority);
                break;
            case CargoManager.RequestPriority.medium:
                priorityButtonImage.SetImage(mediumPrioritySprite);
                priorityButtonImage.Color = colorData.GetColor(ColorCode.mediumPriority);
                break;
            case CargoManager.RequestPriority.high:
                priorityButtonImage.SetImage(highPrioritySprite);
                priorityButtonImage.Color = colorData.GetColor(ColorCode.highPriority);
                break;
            default:
                break;
        }
    }

    private void SetUpText(List<PopUpInfo> popUpInfos)
    {
        if (popUpInfos == null || popUpInfos.Count == 0)
        {
            unitName.Text = "";
            return;
        }

        unitName.Text = popUpInfos.FirstOrDefault(x => x.infoType == PopUpInfo.PopUpInfoType.name).info;
    }

    //left here as an example in case its needed....
    private void SetUpPriorityButton(List<PopUpPriorityButton> popUpButtons)
    {
        //CleanUpButtons();
        //popUpButtons = popUpButtons.OrderByDescending(o => o.priority).ToList();
        //foreach (var popUpButton in popUpButtons)
        //{
        //    SetUpButton(popUpButton);
        //}
    }

    private void SetUpButtons(List<PopUpButtonInfo> buttonInfoList)
    {
        ToggleOffButtons();
        deleteButton.RemoveClickListeners();

        if (toolTipObject.gameObject.TryGetComponent(out PlayerUnit unit))
            deleteButton.clicked += () => unit.DeleteUnit();
        else
            deleteButton.clicked += () => Destroy(toolTipObject.gameObject);

        deleteButton.gameObject.SetActive(unit.unitType != PlayerUnitType.hq);

        foreach (var buttonInfo in buttonInfoList)
        {
            switch (buttonInfo.buttonType)
            {
                case ButtonType.move:
                    moveButton.gameObject.SetActive(true);
                    moveButton.UnHide();
                    moveButton.RemoveClickListeners();
                    moveButton.clicked += () => moveButtonClicked?.Invoke();
                    break;
                case ButtonType.addUnit:
                    addUnitButton.gameObject.SetActive(true);
                    addUnitButton.UnHide();
                    addUnitButton.RemoveClickListeners();
                    addUnitButton.clicked += buttonInfo.buttonAction;
                    break;
                case ButtonType.removeUnit:
                    removeUnitButton.gameObject.SetActive(true);
                    removeUnitButton.UnHide();
                    removeUnitButton.RemoveClickListeners();
                    removeUnitButton.clicked += buttonInfo.buttonAction;
                    break;
                default:
                    break;
            }
        }
    }


    private void ToggleOffButtons()
    {
        moveButton.Hide();
        moveButton.gameObject.SetActive(false);
        addUnitButton.Hide();
        addUnitButton.gameObject.SetActive(false);
        removeUnitButton.Hide();
        removeUnitButton.gameObject.SetActive(false);
        //launchButton.Hide();
    }

    private void SetupActiveIndicator(PopUpCanToggle popUpCanToggle)
    {
        if (popUpCanToggle.isActive)
            activeIndicatorUIBlock.DOColor(activeColor, 0.25f);
        else
            activeIndicatorUIBlock.DOColor(inactiveColor, 0.25f);

        activityButton.RemoveAllListeners();
        activityButton.clicked += () => popUpCanToggle.toggleAction();
        activityButton.clicked += () => ToggleIndicator();
    }

    private void ToggleIndicator()
    {
        if (activeIndicatorUIBlock.Color == activeColor)
            activeIndicatorUIBlock.DOColor(inactiveColor, 0.25f);
        else
            activeIndicatorUIBlock.DOColor(activeColor, 0.25f);
    }

    private void SetUpButton(PopUpPriorityButton popUpButton)
    {
        //GameObject button = buttonPool.PullGameObject();

        //if (!buttonList.Contains(button))
        //{
        //    buttonList.Add(button);
        //    button.transform.SetParent(buttonContainer);
        //}

        //button.GetComponentInChildren<TextBlock>().Text = popUpButton.displayName;

        //Button uiButton = button.GetComponent<Button>();
        //button.transform.localScale = Vector3.one;
        //uiButton.RemoveAllListeners();
        //uiButton.clicked += () => popUpButton.button?.Invoke();
        //if (popUpButton.closeWindowOnClick)
        //    uiButton.clicked += () => CloseWindow();
    }

    private void ClearText()
    {
        unitName.Text = string.Empty;
        //tooltipStats.Text = string.Empty;
        //tooltipStorage.Text = string.Empty;
    }

    private Vector3 GetMousePosition()
    {
        return new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, 0f);
    }

    private void CloseWindow(bool isInEditMode)
    {
        if (!isInEditMode)
            CloseWindow();
    }

    public override void CloseWindow()
    {
        if (mouseIsOver)
            return;

        if (clipMask == null)
            clipMask = this.GetComponent<ClipMask>();

        if (Application.isPlaying)
            clipMask.DoFade(0f, 0.1f);
        else
            clipMask.SetAlpha(0f);

        //make sure the dropdown isn't open for the next unit
        productionDropdown.Collapse();
        
        isOpen = false;
        //clipMask.interactable = false;
        //clipMask.obstructDrags = false;
        toolTipObject = null; //used to allow tooltip to know what is active
        StopAllCoroutines();
        openWindows.Remove(this);
    }

    public override void OpenWindow()
    {
        if(moveToMouse)
            StartCoroutine(OneFrameDelayPositioning());

        if (clipMask == null)
            clipMask = this.GetComponent<ClipMask>();

        if (Application.isPlaying)
            clipMask.DoFade(1f, 0.15f);
        else
            clipMask.SetAlpha(1f);

        //SFXManager.PlayClick();
        isOpen = true;
        //clipMask.interactable = true;
        //clipMask.obstructDrags = true;

        this.transform.SetAsLastSibling();
        if(!openWindows.Contains(this))
            openWindows.Add(this);
    }

    private IEnumerator OneFrameDelayPositioning()
    {
        yield return null;

        Vector2 unitPosition = Camera.WorldToScreenPoint(UnitSelectionManager.selectedUnit.transform.position);
        var offset = GetOffset(unitPosition);

        parentBlock.Position.Y = (unitPosition.y + offset.y) / canvasScale;
        parentBlock.Position.X = (unitPosition.x + offset.x) / canvasScale;
    }

    private Vector2 GetOffset(Vector2 unitPosition)
    {
        Vector2 offset = Vector2.zero;
        Vector3 size = parentBlock.Size.Value;


        if (unitPosition.y > Screen.height * 0.75f)
            offset += new Vector2(0, -size.y - yOffset);
        else
            offset += new Vector2(0, yOffset);

        //not dividing the size by 2 because of the canvas scale
        if (unitPosition.x + size.x * canvasScale > Screen.width * 0.95f)
            offset += new Vector2(-size.x - xOffset, 0);
        else
            offset += new Vector2(xOffset, 0);

        return offset * canvasScale;
    }

    public void OnEndHover(Gesture.OnUnhover evt, PopUpVisuals button)
    {
        Ray ray = uiCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        List<UIBlockHit> blocksHit = new List<UIBlockHit>();
        Interaction.RaycastAll(ray, blocksHit);

        foreach (var hit in blocksHit)
        {
            if (hit.UIBlock.gameObject == this.gameObject)
                return;
        }

        mouseIsOver = false;
        StartCoroutine(DelayClose());
        CloseWindow();
    }

    public void ForceClose(InputAction.CallbackContext cxt)
    {
        if (clipMask == null)
            clipMask = this.GetComponent<ClipMask>();

        if (Application.isPlaying)
            clipMask.DoFade(0f, 0.1f);
        else
            clipMask.SetAlpha(0f);

        //clipMask.interactable = false;
        //clipMask.obstructDrags = false;
        toolTipObject = null; //used to allow tooltip to know what is active
        StopAllCoroutines();
    }

    private void OnStartHover(Gesture.OnHover evt, PopUpVisuals target)
    {
        mouseIsOver = true;
    }

    private IEnumerator DelayClose()
    {
        yield return new WaitForSeconds(closeDelay);
        if (!mouseIsOver)
            CloseWindow();
    }
}

