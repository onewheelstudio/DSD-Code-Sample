using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using Nova;
using NovaSamples.UIControls;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WorkerMenu : WindowPopup
{
    [Header("Worker Stats")]
    private int hireCost = 150;
    [SerializeField] private TextBlock workerHireCost;
    [SerializeField] private TextBlock workerInfoText;
    [SerializeField] private TextBlock foodCost;
    [SerializeField] private TextBlock waterCost;
    [SerializeField] private TextBlock creditCost;
    [SerializeField] private Slider wageSlider;
    [SerializeField] private Button addWorkerButton;
    [SerializeField] private Button removeWorkerButton;
    [SerializeField] private Toggle halfRations;
    [SerializeField] private Toggle fullRations;
    [SerializeField] private Toggle doubleRations;
    private Rations currentRations;

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
    private UnitManager unitManager;
    private CursorManager cursorManager;

    private PlayerResources playerResources;
    private bool workerMenuUnlocked = false;

    public static event Action WorkerHired;
    public static event Action WorkerFired;
    public static event Action RationSet;
    public static event Action WagesSet;

    private void Awake()
    {
        CheatCodes.AddButton(() => { UnlockWindow(); OpenWindow(); }, "Open Worker Menu");
        unitManager = FindFirstObjectByType<UnitManager>();
        cursorManager = FindFirstObjectByType<CursorManager>();
    }

    private new void OnEnable()
    {
        base.OnEnable();
        halfRations.toggled += SetRations;
        fullRations.toggled += SetRations;
        doubleRations.toggled += SetRations;
        UpdateWorkerValues();
        UpdateHappiness();

        wageSlider.OnValueChanged.AddListener(SetWage);
        wageSlider.Value = WorkerManager.wagePerWorker;

        addWorkerButton.clicked += AddWorker;
        removeWorkerButton.clicked += RemoveWorker;

        UnitManager.unitPlaced += UpdateWorkerMenu;
        UnitManager.unitPlaced += UpdateHappiness;
        PlayerUnit.unitRemoved += UpdateWorkerMenu;

        WorkerManager.workerStateChanged += UpdateWorkerValues;
        WorkerManager.workerStateChanged += UpdateHappiness;
        OpenWorkerMenu.WorkerMenuOpen += OpenWindow;
        UnlockWorkerMenuButton.WorkerButtonUnlocked += UnlockWindow;
        PubBehavior.HappinessChanged += UpdateHappiness;

        addInfantryButton.clicked += AddInfantry;
        setRallyPoint.clicked += SetRallyPoint;
        CloseWindow();
    }

    private new void OnDisable()
    {
        base.OnDisable();
        halfRations.toggled -= SetRations;
        fullRations.toggled -= SetRations;
        doubleRations.toggled -= SetRations;

        wageSlider.OnValueChanged.AddListener(SetWage);

        addWorkerButton.clicked -= AddWorker;
        removeWorkerButton.clicked -= RemoveWorker;

        UnitManager.unitPlaced -= UpdateWorkerMenu;
        PlayerUnit.unitRemoved -= UpdateWorkerMenu;

        WorkerManager.workerStateChanged -= UpdateWorkerValues;
        WorkerManager.workerStateChanged -= UpdateHappiness;

        OpenWorkerMenu.WorkerMenuOpen -= OpenWindow;
        UnlockWorkerMenuButton.WorkerButtonUnlocked -= UnlockWindow;
        PubBehavior.HappinessChanged -= UpdateHappiness;

        addInfantryButton.clicked -= AddInfantry;
        setRallyPoint.clicked -= SetRallyPoint;
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
            playerResources = GameObject.FindObjectOfType<PlayerResources>();

        workerHireCost.Text = "Hire Cost: " + hireCost.ToString();

        workerInfoText.Text = "Available: " + WorkerManager.availableWorkers.ToString();
        if (WorkerManager.workersNeeded > 0)
            workerInfoText.Text += ("\nNeeded: " + WorkerManager.workersNeeded.ToString()).TMP_Color(ColorManager.GetColor(ColorCode.offPriority));
        workerInfoText.Text += "\nTotal: " + WorkerManager.totalWorkers.ToString();
        workerInfoText.Text += $"\nHousing: {WorkerManager.housingCapacity}";

        foodCost.Text = (WorkerManager.foodperworker * WorkerManager.totalWorkers).ToString();
        waterCost.Text = (WorkerManager.foodperworker * WorkerManager.totalWorkers).ToString();
        creditCost.Text = (WorkerManager.wagePerWorker * WorkerManager.totalWorkers).ToString();

        efficiencyText.Text = $"Efficiency: {WorkerManager.CalculateGlobalWorkerEfficiency() * 100}%";
    }

    private void UpdateHappiness(Unit unit)
    {
        UpdateHappiness();
    }

    private void UpdateHappiness()
    {
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

        infantryFoodCost.Text = (count * WorkerManager.foodperworker * 3).ToString();
        infantryWaterCost.Text = (count * WorkerManager.foodperworker * 3).ToString();
        infantryCreditCost.Text = (count * WorkerManager.wagePerWorker * 3).ToString();
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
                && tile.TileType != HexTileType.grass)
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
        GameObject newUnit =  unitManager.InstantiateUnitByType(PlayerUnitType.infantry, target);
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

    private void SetRations(Toggle toggle, bool isOn)
    {
        if (!isOn)
            return;

        if(toggle == halfRations && isOn)
        {
            currentRations = Rations.Half;
            WorkerManager.foodperworker = 0.5f * WorkerManager.baseFoodPerWorker;
        }
        else if(toggle == fullRations && isOn)
        {
            currentRations = Rations.Full;
            WorkerManager.foodperworker = WorkerManager.baseFoodPerWorker;
        }
        else if(toggle == doubleRations && isOn)
        {
            currentRations = Rations.Double;
            WorkerManager.foodperworker = 2 * WorkerManager.baseFoodPerWorker;
        }
        UpdateWorkerValues();
        UpdateHappiness();
        RationSet?.Invoke();
    }

    private void SetWage(float wage)
    {
        WorkerManager.wagePerWorker = Mathf.RoundToInt(wage);
        UpdateWorkerValues();
        UpdateHappiness();
        WagesSet?.Invoke();
    }

    private void RemoveWorker()
    {
        if(WorkerManager.availableWorkers > 0)
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
    }

    private enum Rations
    {
        Half,
        Full,
        Double
    }
}
