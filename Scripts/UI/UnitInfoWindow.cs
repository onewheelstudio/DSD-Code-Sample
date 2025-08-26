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
    [SerializeField] private Button urgentButton;
    [SerializeField] private UIBlock2D urgentButtonImage;
    public static event Action priorityChanged;
    public static event Action<PlayerUnit> urgentPriorityTurnOn;
    public static event Action<PlayerUnit> urgentPriorityTurnOff;

    [Header("Production")]
    [SerializeField]private GameObject productionPanel;
    [SerializeField] private TextBlock currentReceipe;
    [SerializeField] private ListView recipeRequirements;
    [SerializeField] private ListView recipeResults;
    [SerializeField] private TextBlock efficiencyText;
    [SerializeField] private TextBlock upTimeText;
    [SerializeField] private UIBlock2D upTimeIcon;
    [SerializeField] private UIBlock2D timerBlock;
    [SerializeField] private InfoToolTip productionToolTip;
    [SerializeField] private Button selectReceipeButton;
    private SelectReceipeWindow selectReceipeWindow;

    [Header("Preferred Delivery")]
    [SerializeField] private UIBlock preferredDeliveryPanel;
    [SerializeField] private ListView preferredDeliveryList;
    [SerializeField] private Button addConnectionButton;
    [SerializeField] private Button removeConnectionButton;
    [SerializeField] private UIBlock2D addConnectionBlock;
    [SerializeField] private UIBlock2D removeConnectionBlock;
    [SerializeField] private UIBlock2D addingConnectionIcon;
    [SerializeField] private Color addingConnectionColor;
    private Color startingConnectionButtonColor;

    [Header("Shuttle Utilization")]
    [SerializeField] private UIBlock shuttleUtilizationPanel;
    [SerializeField] private UIBlock2D shuttleUtilizationBar;
    [SerializeField] private TextBlock shuttleUtilizationText;

    [Header("Resource Transport")]
    [SerializeField] private UIBlock allowedResourcePanel;
    [SerializeField] private GridView allowedResourceList;
    [SerializeField] private Button changeAllowedResources;
    private ITransportResources transportResources;
    private AllowedResourceWindow allowedResourceWindow;

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
    private List<PopUpResourceAmount> popUpResources;

    private Camera Camera;
    /// <summary>
    /// this time needs to be in game time not real time
    /// </summary>
    private float productionTime = 5f;

    private void Awake()
    {
        screenSpace = GameObject.FindObjectOfType<ScreenSpace>();

        playerResources = GameObject.FindObjectOfType<PlayerResources>();
        statsManager = GameObject.FindObjectOfType<StatsManager>();
        unitManager = GameObject.FindObjectOfType<UnitManager>();
        unitSelectionManager = FindFirstObjectByType<UnitSelectionManager>();
        parentBlock = this.GetComponent<UIBlock2D>();
        selectReceipeWindow = FindFirstObjectByType<SelectReceipeWindow>();
        allowedResourceWindow = FindFirstObjectByType<AllowedResourceWindow>();

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

        allowedResourceList.AddDataBinder<ResourceType, ResourceIconDisplayVisuals>(BindAllowedResources);
        allowedResourceList.SetSliceProvider(ResourceGridSlice);
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
        UnitStorageBehavior.startListeningAddConnection += AddButtonOn;
        UnitStorageBehavior.stopListeningAddConnection += AddButtonOff;
        UnitStorageBehavior.startListeningRemoveConnection += RemoveButtonOn;
        UnitStorageBehavior.stopListeningRemoveConnection += RemoveButtonOff;

        UnitToolTip.updateStats += PopulateStats;
        UnitToolTip.updateResources += PopulateResources;
        UnitToolTip.updateAllowedResources += PopulateAllowedResources;
        AllowedResourceWindow.AllowedResourcesChanged += PopulateAllowedResources;

        UnitToolTip.updateRecipe += SetUpReceipes;

        UIBlock2D block = this.GetComponent<UIBlock2D>();
        block.AddGestureHandler<Gesture.OnUnhover, PopUpVisuals>(OnEndHover);
        block.AddGestureHandler<Gesture.OnHover, PopUpVisuals>(OnStartHover);

        selectReceipeButton.Clicked += selectReceipeWindow.ToggleWindow;
        changeAllowedResources.Clicked += allowedResourceWindow.ToggleWindow;

        novaGroup.UpdateInteractables();

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
        UnitStorageBehavior.startListeningAddConnection -= AddButtonOn;
        UnitStorageBehavior.stopListeningAddConnection -= AddButtonOff;
        UnitStorageBehavior.startListeningRemoveConnection -= RemoveButtonOn;
        UnitStorageBehavior.stopListeningRemoveConnection -= RemoveButtonOff;

        UnitToolTip.updateStats -= PopulateStats;
        UnitToolTip.updateResources -= PopulateResources;
        UnitToolTip.updateAllowedResources -= PopulateAllowedResources;
        AllowedResourceWindow.AllowedResourcesChanged -= PopulateAllowedResources;

        UnitToolTip.updateRecipe -= SetUpReceipes;

        UIBlock2D block = this.GetComponent<UIBlock2D>();
        block.RemoveGestureHandler<Gesture.OnUnhover, PopUpVisuals>(OnEndHover);
        block.RemoveGestureHandler<Gesture.OnHover, PopUpVisuals>(OnStartHover);

        selectReceipeButton.Clicked -= selectReceipeWindow.ToggleWindow;
        changeAllowedResources.Clicked -= allowedResourceWindow.ToggleWindow;
    }


    private void OpenToolTip(IOrderedEnumerable<PopUpInfo> popUpInfos, List<PopUpPriorityButton> popUpButtons, PopUpCanToggle popUpCanToggle, RequestStorageInfo requestStorageInfo, RecipeInfo receipeInfo, List<PopUpButtonInfo> buttonInfo, UnitToolTip toolTip)
    {
        if (toolTipObject != null && toolTipObject == toolTip)
            return;

        PopulateToolTip(popUpInfos, popUpButtons, popUpCanToggle, requestStorageInfo, receipeInfo, buttonInfo, toolTip);
        
        OpenWindow();
    }

    private void PopulateToolTip(IOrderedEnumerable<PopUpInfo> popUpInfos, List<PopUpPriorityButton> popUpButtons, PopUpCanToggle popUpCanToggle, RequestStorageInfo requestStorageInfo, RecipeInfo receipeInfo, List<PopUpButtonInfo> buttonInfo, UnitToolTip toolTip)
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
        SetupShuttleUtilization(popUpInfos);
    }

    private void SetupShuttleUtilization(IOrderedEnumerable<PopUpInfo> popUpInfos)
    {
        if(popUpInfos.Count(popUpInfos => popUpInfos.infoType == PopUpInfo.PopUpInfoType.shuttleUtilization) == 0)
        {
            shuttleUtilizationPanel.gameObject.SetActive(false);
            return;
        }
        else
        {
            shuttleUtilizationPanel.gameObject.SetActive(true);
        }

        PopUpInfo shuttleUtilizationInfo = popUpInfos.FirstOrDefault(x => x.infoType == PopUpInfo.PopUpInfoType.shuttleUtilization);
        float shuttleUtilization = shuttleUtilizationInfo.objectInfo / 100f;
        shuttleUtilizationBar.Size.X.Percent = shuttleUtilization;
        shuttleUtilizationText.Text = $"{Mathf.RoundToInt(shuttleUtilization * 100)}%";
    }

    private void SetUnitImage(IOrderedEnumerable<PopUpInfo> popUpInfos)
    {
        PlayerUnitType unitType = (PlayerUnitType)popUpInfos.FirstOrDefault(x => x.infoType == PopUpInfo.PopUpInfoType.name 
                                                                            && x.objectInfo != (int)PlayerUnitType.cargoShuttle).objectInfo;
        if (unitType == PlayerUnitType.buildingSpot)
            unitType = (PlayerUnitType)popUpInfos.FirstOrDefault(x => x.infoType == PopUpInfo.PopUpInfoType.name 
                                                                 && x.objectInfo != (int)PlayerUnitType.buildingSpot).objectInfo;

        unitImage.SetImage(unitImages.GetPlayerUnitImage(unitType));
    }

    private void PopulateInfo(IOrderedEnumerable<PopUpInfo> popUpInfos, UnitToolTip toolTip)
    {
        toolTipObject = toolTip;
        SetUpText(popUpInfos);
        SetupShuttleUtilization(popUpInfos);
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


    private void SetupPreferredDelivery(RecipeInfo receipeInfo, RequestStorageInfo requestStorageInfo, UnitToolTip toolTip)
    {
        if(UnitSelectionManager.selectedUnit.unitType == PlayerUnitType.infantry)
            preferredDeliveryPanel.gameObject.SetActive(false);
        else
            preferredDeliveryPanel.gameObject.SetActive(ConnectionDisplayManager.ConnectionsUnlocked);

        if (UnitSelectionManager.selectedUnit == null)
        {
            currrentUnitStorage = null;
            return;
        }

        currrentUnitStorage = UnitSelectionManager.selectedUnit.GetComponent<UnitStorageBehavior>();

        toolTipObject = toolTip;
        preferredDeliveryList.SetDataSource(requestStorageInfo.connections.OrderByDescending(c => c.GetPriority()).ToList()); // need to send in the USB not just the list of connections
        addConnectionButton.RemoveClickListeners();
        addConnectionButton.Clicked += requestStorageInfo.startAddConnection;
        
        removeConnectionButton.RemoveClickListeners();
        removeConnectionButton.Clicked += requestStorageInfo.startRemoveConnection;
    }
    private void BindPreferredDelivery(Data.OnBind<UnitStorageBehavior> evt, PreferredDeliveryVisual target, int index)
    {
        if (evt.UserData.TryGetComponent(out PlayerUnit playerUnit))
            target.label.Text = evt.UserData.GetComponent<PlayerUnit>().unitType.ToNiceString();
        else if (evt.UserData.TryGetComponent(out HexTile hexTile))
            target.label.Text = hexTile.TileType.ToNiceString() + " Tile";
        else
            target.label.Text = "Connection"; //this shouldn't happen

        target.deleteButton.RemoveAllListeners();
        target.deleteButton.Clicked += () => currrentUnitStorage.RemoveConnection(evt.UserData);

        HashSet<ResourceType> resources = currrentUnitStorage.GetShippedResourceTypes(evt.UserData);
        List<ResourceTemplate> templateList = new List<ResourceTemplate>();

        foreach (var resource in resources)
        {
            if (resource == ResourceType.Workers)
                continue;

            templateList.Add(playerResources.GetResourceTemplate(resource));
        }
        target.AddResources(templateList);
        target.SetPriorityDisplay(evt.UserData.GetPriority());
    }

    private void AddButtonOff(UnitStorageBehavior behavior)
    {
        addConnectionBlock.Border.Color = startingConnectionButtonColor;
    }

    private void AddButtonOn(UnitStorageBehavior behavior)
    {
        addConnectionBlock.Border.Color = addingConnectionColor;
    }
    
    private void RemoveButtonOff(UnitStorageBehavior behavior)
    {
        removeConnectionBlock.Border.Color = startingConnectionButtonColor;
    }

    private void RemoveButtonOn(UnitStorageBehavior behavior)
    {
        removeConnectionBlock.Border.Color = addingConnectionColor;
    }

    private void PopulateResources(List<PopUpResourceAmount> list, UnitToolTip tip)
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


    private void PopulateAllowedResources(ITransportResources transportResources) => PopulateAllowedResources(transportResources, null);
    private void PopulateAllowedResources(ITransportResources transportResources, UnitToolTip tip)
    {
        bool showAllowedResources = transportResources != null;
        this.transportResources = transportResources;
        allowedResourcePanel.gameObject.SetActive(showAllowedResources);
        if (!showAllowedResources)
            return;

        List<ResourceType> allowedResources = transportResources.GetAllowedResources().ToList();
        allowedResourceList.SetDataSource(allowedResources);
        allowedResourceWindow.SetTransportStorage(transportResources);
    }

    private void BindAllowedResources(Data.OnBind<ResourceType> evt, ResourceIconDisplayVisuals target, int index)
    {
        var resourceTemplate = playerResources.GetResourceTemplate(evt.UserData);   
        target.Background.SetImage(resourceTemplate.icon);
        target.Background.Color = resourceTemplate.resourceColor;
        target.toolTip.SetToolTipInfo(evt.UserData.ToNiceString(), resourceTemplate.icon);
    }


    private void ResourceGridSlice(int sliceIndex, GridView gridView, ref GridSlice gridSlice)
    {
        gridSlice.AutoLayout.Spacing.Value = 5;
    }

    private void BindResources(Data.OnBind<ResourceAmount> evt, UnitInfoButtonVisuals target, int index)
    {
        target.icon.SetImage(playerResources.GetResourceTemplate(evt.UserData.type).icon);
        target.icon.Color = playerResources.GetResourceTemplate(evt.UserData.type).resourceColor;
        int countPerDay = Mathf.Max(0,Mathf.FloorToInt(dayNightManager.DayLength / productionTime) * evt.UserData.amount);
        string description = countPerDay.ToString()+ " per day";
        description = TMPHelper.Color(description, ColorManager.GetColor(ColorCode.repuation));
        target.infoToolTip.SetToolTipInfo(evt.UserData.type.ToNiceString(), playerResources.GetResourceTemplate(evt.UserData.type).icon, description);

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
            //float cost = (float)unitManager.GetUnitCost(typeToBuild, evt.UserData.type).amount;
            float percent = Mathf.Clamp01((float)evt.UserData.amount / (float)unitManager.GetUnitCost(typeToBuild, evt.UserData.type).amount);
            target.sliderFill.Size.Percent = new Vector2(percent, 1f);

            //target.label.Text = $"{evt.UserData.amount.ToString()}/{cost}";
        }
        else if(evt.UserData.type == ResourceType.Workers)
        {
            float percent = Mathf.Clamp01((float)evt.UserData.amount / UnitSelectionManager.selectedUnit.GetStat(Stat.workers));
            target.sliderFill.Size.Percent = new Vector2(percent, 1f);
        }
        else if(UnitSelectionManager.selectedUnitType == PlayerUnitType.orbitalBarge)
        {
            float storageLimit = UnitSelectionManager.selectedUnit.GetComponent<UnitStorageBehavior>().GetResourceStorageLimit(evt.UserData.type);
            if (storageLimit < 1f)
                storageLimit = UnitSelectionManager.selectedUnit.GetStat(Stat.maxStorage);

            float percent = Mathf.Clamp01((float)evt.UserData.amount / storageLimit);
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
        if(UnitSelectionManager.selectedUnit == null)
            return;

        float maxHealth = (int)UnitSelectionManager.selectedUnit.GetStat(Stat.hitPoints);
        float currentHealth = (int)UnitSelectionManager.selectedUnit.GetHP();
        healthBar.Size.X.Percent = Mathf.Clamp01(currentHealth / maxHealth);
        healthText.Text = $"{currentHealth}";// / {maxHealth}";

        float maxShield = (int)UnitSelectionManager.selectedUnit.GetStat(Stat.shield);
        shieldParent.SetActive(maxShield > 0f);
        float currentShield = (int)UnitSelectionManager.selectedUnit.GetShield();
        shieldBar.Size.X.Percent = Mathf.Clamp01(currentShield / maxShield);
        shieldText.Text = $"{currentShield}";// / {maxHealth}";

        if (list == null)
            return;

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

    private void SetUpReceipes(RecipeInfo receipeInfo, UnitToolTip toolTip)
    {
        if (receipeInfo.recipes == null || receipeInfo.recipes.Count == 0)
        {
            productionPanel.gameObject.SetActive(false);
            return;
        }
         
        string timeToProduceString;
        int efficiency = Mathf.RoundToInt(receipeInfo.efficiency);
        this.productionTime = receipeInfo.timeToProduce * GameConstants.GameSpeed; //keep time in game time not real time
        if (receipeInfo.timeToProduce <= 0 || receipeInfo.timeToProduce > 1000)
            timeToProduceString = "Time: ---";
        else
            timeToProduceString = $"Time: {this.productionTime.ToString("0.0")}s";

        Color efficiencyColor;
        if (efficiency < 50)
            efficiencyColor = colorData.GetColor(ColorCode.red);
        else if (efficiency < 85)
            efficiencyColor = colorData.GetColor(ColorCode.yellow);
        else if (efficiency > 110)
            efficiencyColor = colorData.GetColor(ColorCode.techCredit);
        else
            efficiencyColor = Color.white;

        efficiencyText.Text = TMPHelper.Color(timeToProduceString, efficiencyColor);
        timerBlock.Color = efficiencyColor;
        float timeLeft = Time.time - receipeInfo.recipeOwner.GetStartTime();
        timerBlock.RadialFill.FillAngle = 360f * (receipeInfo.timeToProduce - timeLeft) / receipeInfo.timeToProduce;

        upTimeText.Text = Mathf.RoundToInt(receipeInfo.upTime * 100).ToString() + "%";
        if(receipeInfo.upTime > 0.85f)
            upTimeIcon.Color = ColorManager.GetColor(ColorCode.green);
        else if(receipeInfo.upTime > 0.60f)
            upTimeIcon.Color = ColorManager.GetColor(ColorCode.yellow);
        else
            upTimeIcon.Color = ColorManager.GetColor(ColorCode.red);

        string workerEfficiency = Mathf.RoundToInt(WorkerManager.globalWorkerEfficiency * 100).ToString();
        string toolTipText = $"Production Time: {timeToProduceString}\nBuilding Efficiency: {efficiency.ToString()}%\nWorker Efficiency: {workerEfficiency}%\nUp Time: {Mathf.RoundToInt(receipeInfo.upTime * 100)}%";
        productionToolTip.SetToolTipInfo("Production Stats", toolTipText);

        if (selectReceipeWindow.SetRecipes(receipeInfo))
        {
            productionPanel.gameObject.SetActive(true);
            currentReceipe.Text = receipeInfo.recipes[receipeInfo.currentRecipe].niceName;
            recipeRequirements.SetDataSource(receipeInfo.recipes[receipeInfo.currentRecipe].GetCost());
            recipeResults.SetDataSource(receipeInfo.recipes[receipeInfo.currentRecipe].GetProduction());
        }
    }

    private void SetUpPriorityButton(RequestStorageInfo requestPriority)
    {
        if (requestPriority.setPriority == null)
        {
            priorityButtonImage.gameObject.SetActive(false);
            urgentButton.gameObject.SetActive(false);
        }
        else
        {
            priorityButtonImage.gameObject.SetActive(true);
            urgentButton.gameObject.SetActive(true);
        }

        priorityButton.RemoveAllListeners(); //this is important. dumb ass.
        priorityButton.Clicked += () => TogglePriority(requestPriority);
        SetPriorityDisplay(requestPriority);

        urgentButton.RemoveAllListeners();
        urgentButton.Clicked += () => ToggleUrgentPriority(requestPriority);
        urgentButtonImage.Color = requestPriority.getPriority() == CargoManager.RequestPriority.urgent ? colorData.GetColor(ColorCode.urgentDelivery) : Color.white;
    }

    private void ToggleUrgentPriority(RequestStorageInfo requestPriority)
    {
        PlayerUnit playerUnit = toolTipObject.GetComponent<PlayerUnit>();

        if (requestPriority.getPriority() == CargoManager.RequestPriority.urgent)
        {
            requestPriority.revertPrioity();
            SetPriorityDisplay(requestPriority);
            urgentPriorityTurnOff?.Invoke(playerUnit);
            priorityChanged?.Invoke();
            urgentButtonImage.Color = Color.white;
            MessagePanel.ShowMessage($"{playerUnit.unitType.ToNiceString()} urgent delivery canceled", toolTipObject.gameObject);
        }
        else
        {
            urgentPriorityTurnOn?.Invoke(playerUnit);
            priorityChanged?.Invoke();
            urgentButtonImage.Color = colorData.GetColor(ColorCode.urgentDelivery);
            MessagePanel.ShowMessage($"{playerUnit.unitType.ToNiceString()} set to urgent delivery", toolTipObject.gameObject);
        }
    }

    private void TogglePriority(RequestStorageInfo requestPriority)
    {
        //toggle through enum and roll over when getting to the end of the enum
        if (requestPriority.getPriority() == CargoManager.RequestPriority.urgent)
        {
            urgentPriorityTurnOff?.Invoke(UnitSelectionManager.selectedUnit);
            urgentButtonImage.Color = Color.white;
            requestPriority.revertPrioity();
            MessagePanel.ShowMessage($"{UnitSelectionManager.selectedUnit.unitType.ToNiceString()} urgent delivery canceled", toolTipObject.gameObject);
        }

        requestPriority.priority++;
        if (requestPriority.priority > CargoManager.RequestPriority.high)
            requestPriority.priority = CargoManager.RequestPriority.off;
        this.priority = requestPriority.priority;
        requestPriority.setPriority(requestPriority.priority);

        SetPriorityDisplay(requestPriority);
        priorityChanged?.Invoke();
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

    private void SetUpText(IOrderedEnumerable<PopUpInfo> popUpInfos)
    {
        if (popUpInfos.Count() == 0)
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
            deleteButton.Clicked += () => unit.DeleteUnit();
        else
            deleteButton.Clicked += () => Destroy(toolTipObject.gameObject);

        deleteButton.Clicked += CloseWindow;

        deleteButton.gameObject.SetActive(unit.unitType != PlayerUnitType.hq);

        foreach (var buttonInfo in buttonInfoList)
        {
            switch (buttonInfo.buttonType)
            {
                //case ButtonType.move:
                //    moveButton.gameObject.SetActive(true);
                //    moveButton.UnHide();
                //    moveButton.RemoveClickListeners();
                //    moveButton.Clicked += () => moveButtonClicked?.Invoke();
                //    break;
                case ButtonType.addUnit:
                    addUnitButton.gameObject.SetActive(true);
                    addUnitButton.UnHide();
                    addUnitButton.RemoveClickListeners();
                    addUnitButton.Clicked += buttonInfo.buttonAction;
                    break;
                case ButtonType.removeUnit:
                    removeUnitButton.gameObject.SetActive(true);
                    removeUnitButton.UnHide();
                    removeUnitButton.RemoveClickListeners();
                    removeUnitButton.Clicked += buttonInfo.buttonAction;
                    break;
                case ButtonType.connections:
                    addConnectionButton.gameObject.SetActive(true && ConnectionDisplayManager.ConnectionsUnlocked);
                    addConnectionButton.UnHide();

                    removeConnectionButton.gameObject.SetActive(true && ConnectionDisplayManager.ConnectionsUnlocked);
                    removeConnectionButton.UnHide();
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
        addConnectionButton.Hide();
        addConnectionButton.gameObject.SetActive(false);
        removeConnectionButton.Hide();
        removeConnectionButton.gameObject.SetActive(false);
    }

    private void SetupActiveIndicator(PopUpCanToggle popUpCanToggle)
    {
        if (popUpCanToggle.isActive)
            activeIndicatorUIBlock.DOColor(activeColor, 0.25f);
        else
            activeIndicatorUIBlock.DOColor(inactiveColor, 0.25f);

        activityButton.RemoveAllListeners();
        activityButton.Clicked += () => popUpCanToggle.toggleAction();
        activityButton.Clicked += () => ToggleIndicator();
    }

    private void ToggleIndicator()
    {
        if (activeIndicatorUIBlock.Color == activeColor)
            activeIndicatorUIBlock.DOColor(inactiveColor, 0.25f);
        else
            activeIndicatorUIBlock.DOColor(activeColor, 0.25f);
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

        novaGroup.UpdateInteractables();

        if (Application.isPlaying)
            clipMask.DoFade(0f, 0.1f);
        else
            clipMask.SetAlpha(0f);

        //make sure the dropdown isn't open for the next unit
        selectReceipeWindow.CloseWindow();
        allowedResourceWindow.CloseWindow();
        
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

