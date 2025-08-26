using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using Nova;
using Nova.Animations;
using NovaSamples.UIControls;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WorkerMenu : WindowPopup, ISaveData
{
    [Header("Worker Stats")]
    private int hireCost = 250;
    [SerializeField] private TextBlock workerHireCost;
    [SerializeField] private TextBlock workerInfoText;
    [SerializeField] private TextBlock foodCost;
    [SerializeField] private TextBlock waterCost;
    [SerializeField] private TextBlock creditCost;
    [SerializeField] private Button addWorkerButton;
    [SerializeField] private Button removeWorkerButton;
    
    [Header("Rations")]
    [SerializeField] private Slider rationsSlider;
    private Rations currentRations;
    private float _currentRations;

    [Header("Wages")]
    [SerializeField] private Slider wagesSlider;
    private Wages currentWages;
    private int _currentWages;

    [Header("Worker Happiness")]
    [SerializeField] private TextBlock efficiencyText;
    [SerializeField] private TextBlock averageHappinessText;
    [SerializeField] private TextBlock happinessEffects;

    [Header("Infantry")]
    [SerializeField] private TextBlock infantryCount;
    [SerializeField] private TextBlock infantryHireCost;
    private int currentCost;
    private int numberHired = 0;
    [SerializeField] private Button addInfantryButton;
    [SerializeField] private Button setRallyPoint;
    private Hex3? rallyLocation;
    [SerializeField] private GameObject rallyPointMarker;
    [SerializeField] private TextBlock infantryFoodCost;
    [SerializeField] private TextBlock infantryWaterCost;
    [SerializeField] private TextBlock infantryCreditCost;
    private CursorManager cursorManager;

    private PlayerResources playerResources;
    private bool workerMenuUnlocked = false;

    public static event Action WorkerHired;
    public static event Action WorkerFired;
    public static event Action RationSet;
    public static event Action WagesSet;

    [Header("Unlock and Open Bits")]
    [SerializeField] private Button openButton;
    private UIBlock2D openButtonBlock;
    private AnimationHandle animationHandle;

    private void Awake()
    {
        CheatCodes.AddButton(() => { UnlockWindow(); OpenWindow(); }, "Open Worker Menu");
        unitManager = FindFirstObjectByType<UnitManager>();
        cursorManager = FindFirstObjectByType<CursorManager>();

        openButtonBlock = openButton.GetComponent<UIBlock2D>();

        RegisterDataSaving();
    }

    private new void OnEnable()
    {
        base.OnEnable();
        rationsSlider.ValueChanged += SetRations;
        wagesSlider.ValueChanged += SetWage;

        UpdateWorkerValues();
        UpdateHappiness();

        addWorkerButton.Clicked += AddWorker;
        removeWorkerButton.Clicked += RemoveWorker;

        UnitManager.unitPlaced += UpdateWorkerMenu;
        UnitManager.unitPlaced += UpdateHappiness;
        PlayerUnit.unitRemoved += UpdateWorkerMenu;

        WorkerManager.workerStateChanged += UpdateWorkerValues;
        WorkerManager.workerStateChanged += UpdateHappiness;

        OpenWorkerMenu.WorkerMenuOpen += OpenWindow;
        UnlockWorkerMenuButton.WorkerButtonUnlocked += UnlockWindow;
        PubBehavior.HappinessChanged += UpdateHappiness;

        addInfantryButton.Clicked += AddInfantry;
        setRallyPoint.Clicked += SetRallyPoint;
        CloseWindow();

        wagesSlider.Min = 0f;
        wagesSlider.Max = WorkerManager.maxWages;

        rationsSlider.Min = 0f;
        rationsSlider.Value = WorkerManager.rations * 10;
        rationsSlider.Max = WorkerManager.maxRations * 10; //slider uses integers for snapping
    }


    private new void OnDisable()
    {
        base.OnDisable();
        addWorkerButton.Clicked -= AddWorker;
        removeWorkerButton.Clicked -= RemoveWorker;

        UnitManager.unitPlaced -= UpdateWorkerMenu;
        UnitManager.unitPlaced -= UpdateHappiness;
        PlayerUnit.unitRemoved -= UpdateWorkerMenu;

        WorkerManager.workerStateChanged -= UpdateWorkerValues;
        WorkerManager.workerStateChanged -= UpdateHappiness;

        OpenWorkerMenu.WorkerMenuOpen -= OpenWindow;
        UnlockWorkerMenuButton.WorkerButtonUnlocked -= UnlockWindow;
        PubBehavior.HappinessChanged -= UpdateHappiness;

        addInfantryButton.Clicked -= AddInfantry;
        setRallyPoint.Clicked -= SetRallyPoint;
    }



    private void UpdateWorkerMenu(Unit unit)
    {
        if (!this.instanceIsOpen)
            return;

        UpdateWorkerValues();
        UpdateHappiness();
        UpdateInfantry();
    }

    private void UpdateWorkerValues()
    {
        if (playerResources == null)
            playerResources = GameObject.FindFirstObjectByType<PlayerResources>();

        workerHireCost.Text = "Hire Cost: " + hireCost.ToString();

        workerInfoText.Text = "Available: " + Mathf.Max(0,WorkerManager.AvailableWorkers).ToString();
        if (WorkerManager.AvailableWorkers <= 0 && WorkerManager.workersNeeded > 0)
            workerInfoText.Text += ("\nNeeded: " + WorkerManager.workersNeeded.ToString()).TMP_Color(ColorManager.GetColor(ColorCode.offPriority));
        workerInfoText.Text += "\nTotal: " + WorkerManager.TotalWorkers.ToString();
        workerInfoText.Text += $"\nHousing: {WorkerManager.housingCapacity}";

        foodCost.Text = Mathf.RoundToInt((WorkerManager.rations) * WorkerManager.TotalWorkers).ToString();
        waterCost.Text = Mathf.RoundToInt((WorkerManager.rations) * WorkerManager.TotalWorkers).ToString();
        creditCost.Text = (WorkerManager.wages * WorkerManager.TotalWorkers).ToString();

        efficiencyText.Text = $"Efficiency: {Mathf.RoundToInt(WorkerManager.CalculateGlobalWorkerEfficiency() * 100)}%";
    }

    private void UpdateHappiness(Unit unit)
    {
        UpdateHappiness();
    }

    private void UpdateHappiness()
    {
        if(SaveLoadManager.Loading)
            return;

        List<HappinessFactor> factors = WorkerManager.CalculateHappiness(out int happiness);
        averageHappinessText.Text = $"Compliance: {happiness}";
        happinessEffects.Text = "";

        for (int i = 0; i < factors.Count; i++)
        {
            if (factors[i].value == 0)
                continue;

            string value = factors[i].value > 0 ? $"+{factors[i].value}" : factors[i].value.ToString();
            string description = $"{factors[i].description}";
            Color factorColor = factors[i].value > 0 ? ColorManager.GetColor(ColorCode.mediumPriority) : ColorManager.GetColor(ColorCode.offPriority);
            value = TMPHelper.Color(value, factorColor);

            if(i != 0)
                happinessEffects.Text += $"\n{value} {description}";
            else
                happinessEffects.Text = $"{value} {description}";
        }
    }

    private void UpdateInfantry()
    {
        int count = UnitManager.GetPlayerUnitByType(PlayerUnitType.infantry).Count;
        infantryCount.Text = "Current: " + count.ToString();
        currentCost = GameConstants.infantryCost + numberHired * GameConstants.infantryCostIncrease;
        infantryHireCost.Text = "Recruit Cost: " + currentCost.ToString();

        infantryFoodCost.Text = (count * WorkerManager.rations * 3).ToString();
        infantryWaterCost.Text = (count * WorkerManager.rations * 3).ToString();
        infantryCreditCost.Text = (count * WorkerManager.wages * 3).ToString();
    }

    private void AddInfantry()
    {
        //check cost
        if(HexTechTree.TechCredits < currentCost)
        {
            MessageData notEnoughCredits = new MessageData();
            notEnoughCredits.message = "Not enough credits to recuit a unit.";
            notEnoughCredits.messageColor = ColorManager.GetColor(ColorCode.red);
            notEnoughCredits.messageObject = null;
            MessagePanel.ShowMessage(notEnoughCredits);
            SFXManager.PlaySFX(SFXType.error);
            return;
        }

        //find suitable location
        Hex3 target = Hex3.Zero;
        if (rallyLocation == null)
        {
            PlayerUnit hq = UnitManager.GetPlayerUnitByType(PlayerUnitType.hq)?[0];
            if (hq == null)
                return;

            target = hq.transform.position.ToHex3();
        }
        else
            target = rallyLocation.Value;

        List<Hex3> emptyLocations = HexTileManager.GetHex3WithInRange(target, 0, 2);
        emptyLocations.Insert(0, target);

        bool foundLocation = false;
        foreach (Hex3 hex in emptyLocations)
        {
            if (UnitManager.PlayerUnitAtLocation(hex) != null)
                continue;

            HexTile tile = HexTileManager.GetHexTileAtLocation(hex);
            if (tile == null)
                continue;

            if (tile.TileType != HexTileType.forest 
                && tile.TileType != HexTileType.aspen 
                && tile.TileType != HexTileType.grass
                && tile.TileType != HexTileType.funkyTree
                && tile.TileType != HexTileType.palmTree)
                continue;

            target = hex;
            foundLocation = true;
            break;
        }

        if (foundLocation == false)
        {
            MessageData noLocation = new MessageData();
            noLocation.message = "No suitable location to recuit a unit.";
            noLocation.messageColor = ColorManager.GetColor(ColorCode.highPriority);
            noLocation.messageObject = rallyPointMarker;
            MessagePanel.ShowMessage(noLocation);
            SFXManager.PlaySFX(SFXType.error);
            return;
        }

        //recruit unit
        HexTechTree.ChangeTechCredits(-currentCost);
        numberHired++;
        GameObject newUnit = unitManager.InstantiateUnitByType(PlayerUnitType.infantry, target);
        MessageData newInfantryMessage = new MessageData();
        newInfantryMessage.message = "Security unit recruited.";
        newInfantryMessage.messageColor = ColorManager.GetColor(ColorCode.highPriority);
        newInfantryMessage.messageObject = newUnit;
        MessagePanel.ShowMessage(newInfantryMessage);
        SFXManager.PlaySFX(SFXType.buildingPlace);
        UpdateInfantry();
    }

    private void SetRallyPoint()
    {
        rallyPointMarker.SetActive(false);
        cursorManager.SetCursor(CursorType.rallyPoint);
        StartCoroutine(ListenForRallyPointClick());
    }

    private IEnumerator ListenForRallyPointClick()
    {
        yield return new WaitUntil(() => Mouse.current.leftButton.wasPressedThisFrame);
        rallyLocation = HelperFunctions.GetMouseHex3OnPlane();
        cursorManager.SetCursor(CursorType.hex);
        rallyPointMarker.SetActive(true);
        rallyPointMarker.transform.position = rallyLocation.Value.ToVector3() + Vector3.up * 0.01f;
    }

    private void SetRations(float rations)
    {
        WorkerManager.rations = rations / 10f; //slider is set to integers for snapping
        UpdateWorkerValues();
        UpdateHappiness();
        RationSet?.Invoke();
    }

    private void SetWage(float wages)
    {
        WorkerManager.wages = Mathf.RoundToInt(wages);
        UpdateWorkerValues();
        UpdateHappiness();
        WagesSet?.Invoke();
    }

    private void RemoveWorker()
    {
        if(WorkerManager.AvailableWorkers > 0)
        {
            WorkerManager.HireWorker(-1);
            UpdateWorkerValues();
            UpdateHappiness();
            WorkerFired?.Invoke();
        }
        else
        {
            MessagePanel.ShowMessage("No available workers to fire.", null);
            SFXManager.PlaySFX(SFXType.error);
        }
    }

    private void AddWorker()
    {
        if(HexTechTree.TechCredits >= hireCost && WorkerManager.HireWorker(1))
        {
            HexTechTree.ChangeTechCredits(-hireCost);
            UpdateWorkerValues();
            UpdateHappiness();
            WorkerHired?.Invoke();
        }
        else if(HexTechTree.TechCredits < hireCost)
        {
            SFXManager.PlaySFX(SFXType.error);
            MessagePanel.ShowMessage("Not enough credits to hire a worker.", null);
        }
    }

    public override void OpenWindow()
    {
        if(!workerMenuUnlocked)
            return;

        if (!animationHandle.IsComplete())
        {
            animationHandle.Complete();
            openButtonBlock.Color = Color.white;
        }

        wagesSlider.Value = WorkerManager.wages;
        rationsSlider.Value = WorkerManager.rations * 10f; //slider uses integers for snapping

        base.OpenWindow();
        rallyPointMarker.SetActive(true);
        UpdateWorkerValues();
        UpdateHappiness();
        UpdateInfantry();
    }

    public override void CloseWindow()
    {
        rallyPointMarker.SetActive(false);
        base.CloseWindow();
    }

    private void UnlockWindow()
    {
        workerMenuUnlocked = true;
        if (!StateOfTheGame.tutorialSkipped && !SaveLoadManager.Loading)
            OpenWindow();
        ButtonOn();
    }

    private void ButtonOff()
    {
        openButton.GetComponent<Interactable>().ClickBehavior = ClickBehavior.None;
        openButtonBlock.Color = ColorManager.GetColor(ColorCode.buttonGreyOut);
    }

    private void ButtonOn()
    {
        Interactable interactable = openButton.GetComponent<Interactable>();
        if (interactable.ClickBehavior == ClickBehavior.OnRelease)
            return; //we're already on

        openButton.GetComponent<Interactable>().ClickBehavior = ClickBehavior.OnRelease;

        if (!StateOfTheGame.tutorialSkipped && !SaveLoadManager.Loading)
        {
            ButtonHighlightAnimation animation = new ButtonHighlightAnimation()
            {
                startSize = new Vector3(50, 50, 0),
                endSize = new Vector3(50, 50, 0) * 1.1f,
                startColor = ColorManager.GetColor(ColorCode.callOut),
                endColor = ColorManager.GetColor(ColorCode.callOut),
                endAlpha = 0.5f,
                uIBlock = openButtonBlock
            };
            ButtonIndicator.IndicatorButton(openButtonBlock);
            animationHandle = animation.Loop(1f, -1);
        }
        else
        {
            openButtonBlock.Color = Color.white;
        }
    }

    public void RegisterDataSaving()
    {
        //load after worker manager
        SaveLoadManager.RegisterData(this, 5);
    }

    private const string WORKER_MENU_PATH = "WorkerMenu";

    public void Save(string savePath, ES3Writer writer)
    {
        WorkerMenuData data = new WorkerMenuData
        {
            MenuUnlocked = workerMenuUnlocked,
            CurrentRations = currentRations,
            CurrentWage = currentWages,
        };
        writer.Write<WorkerMenuData>(WORKER_MENU_PATH, data);
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if (ES3.KeyExists(WORKER_MENU_PATH, loadPath))
        {
            WorkerMenuData data = ES3.Load<WorkerMenuData>(WORKER_MENU_PATH, loadPath);

            workerMenuUnlocked = data.MenuUnlocked;
            if (data.MenuUnlocked)
                UnlockWindow();
            
            //switch (data.CurrentRations)
            //{
            //    case Rations.Half:
            //        halfRations.ToggledOn = true;
            //        break;
            //    case Rations.Full:
            //        fullRations.ToggledOn = true;
            //        break;
            //    case Rations.Double:
            //        doubleRations.ToggledOn = true;
            //        break;
            //}

            //switch (data.CurrentWage)
            //{
            //    case Wages.Half:
            //        halfWages.ToggledOn = true;
            //        break;
            //    case Wages.Full:
            //        fullWages.ToggledOn = true;
            //        break;
            //    case Wages.Double:
            //        doubleWages.ToggledOn = true;
            //        break;
            //}
        }

        yield return null;
    }

    public struct WorkerMenuData
    {
        public bool MenuUnlocked;
        public Rations CurrentRations;
        public Wages CurrentWage;
        public float Rations;
        public int Wages;
    }

    public enum Rations
    {
        Half,
        Full,
        Double
    }

    public enum Wages
    {
        Half = 3,
        Full = 6,
        Double = 12,
    }
}
