using Sirenix.OdinInspector;
using System;
using UnityEngine;
using DG.Tweening;
using HexGame.Units;
using System.Collections;
using NovaSamples.UIControls;
using UnityEngine.InputSystem;
using Nova;
using HexGame.Grid;
using Pathfinding;
using static HexTileManager;
using System.Collections.Generic;
using Unity.Mathematics;

public class DayNightManager : MonoBehaviour, ISaveData
{

    [SerializeField]
    private float totalTime = 0f;
    [SerializeField]
    private float timeOfDay = 0f; //in seconds

    [SerializeField]
    private static int dayNumber = 0;
    public static int DayNumber => dayNumber;
    [SerializeField]
    private int dayLength = 180;
    public int DayLength { get => dayLength; }
    public static int secondRemaining;
    public static int secondsPast;
    [SerializeField] private DayNightState dayNightState = DayNightState.Day;

    public static bool isDay
    {
        get
        {
            if (Instance == null)
                return true;
            else return Instance.dayNightState == DayNightState.Day;
        }
    }
    public static bool isNight
    {
        get
        {
            //if we don't exist assume it's day
            if (Instance == null)
                return false;
            else return Instance.dayNightState == DayNightState.Night;
        }
    }
    private bool paused = false;
    public static bool PausedByPlayer => Instance.paused;
    private int speedWhenPaused = 1;
    private int speedWhenNightStarted = 1;
    private bool nightComplete = false;

    [SerializeField, OnValueChanged("SetSunIntensityCurve")]
    private float dayIntensity = 0.5f;
    [SerializeField, OnValueChanged("SetSunIntensityCurve")]
    private float nightIntensity = 0.2f;

    [SerializeField,DisableIf("@true"),ProgressBar(0f, 1f)]
    private float normalizedTime;
    public static float NormalizedTime => Instance.normalizedTime;

    [BoxGroup("Ambient Light")]
    [SerializeField] private bool adjustAmbientLight = true;
    [BoxGroup("Ambient Light")]
    [SerializeField] private Gradient skycolor = new Gradient();
    [BoxGroup("Ambient Light")]
    [SerializeField] private Gradient equatorcolor = new Gradient() ;
    [BoxGroup("Ambient Light")]
    [SerializeField] private Gradient groundcolor = new Gradient();

    [SerializeField]
    private Light sunLight;
    [SerializeField]
    private Gradient sunGradient;


    private static DayNightManager instance;
    private static DayNightManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DayNightManager>();
            }

            return instance;
        }
    }

    public static event Action<int, float> transitionToDay;
    public static event Action<int> toggleDay;
    public static event Action<int, float> transitionToNight;
    public static event Action<int> toggleNight;
    public static event Action<float, bool> percentLeft;
    private float transitionDelay = 5f;

    [Header("Speed Controls")]
    [SerializeField] private Button pauseButton;
    private UIBlock2D pauseBlock;
    private float pausedTimeScale = 1f;
    [SerializeField] private Button normalSpeedButton;
    private UIBlock2D normalSpeedBlock;
    [SerializeField] private Button doubleSpeedButton;
    private UIBlock2D doubleSpeedBlock;
    [SerializeField] private Button tripleSpeedButton;
    private UIBlock2D tripleSpeedBlock;
    [SerializeField] private Color unselectedColor;
    [SerializeField] private Color SelectedColor;

    [SerializeField] private AnimationCurve sunIntensity = new AnimationCurve();
    private UIControlActions uiControlActions;


    private void Awake()
    {
        dayNumber = 0;
        secondsPast = 0;
        //ensure that we have a normal day/night cycle in builds
        if (!Application.isEditor)
            NormalDay();

        if (pauseButton)
        {
            pauseBlock = pauseButton.GetComponent<UIBlock2D>();
            normalSpeedBlock = normalSpeedButton.GetComponent<UIBlock2D>();
            doubleSpeedBlock = doubleSpeedButton.GetComponent<UIBlock2D>();
            tripleSpeedBlock = tripleSpeedButton.GetComponent<UIBlock2D>();
        }

        percentLeft?.Invoke(0.5f, dayNightState == DayNightState.Day);

        paused = true;
        dayNightState = DayNightState.Transitioning;

        uiControlActions = new UIControlActions();
        if(pauseButton != null)
            SetGameSpeed(1);

        CheatCodes.AddButton(QuickDay, "Quick Day");
        CheatCodes.AddButton(NormalDay, "Normal Day");


        RegisterDataSaving();
    }

    private void Start()
    {
        sunLight.color = sunGradient.Evaluate(10f / DayLength);
        sunLight.intensity = dayIntensity;
        SetSunIntensityCurve();
    }

    private void OnEnable()
    {
        if (FindObjectOfType<UnitManager>() == null)
            FirstUnitPlaced(null);

        UnitManager.unitPlaced += FirstUnitPlaced;
        EnemySpawnManager.AllEnemiesKilled += NightComplete;
        BuildingTutorialComplete.buildingTutorialComplete += StartClock;
        StateOfTheGame.TutorialSkipped += TutorialSkipped;

        Initialize();

        if (pauseButton != null)
        {
            pauseButton.Clicked += PauseGame;
            normalSpeedButton.Clicked += () => SetGameSpeed(1);
            doubleSpeedButton.Clicked += () => SetGameSpeed(2);
            tripleSpeedButton.Clicked += () => SetGameSpeed(3);
        }


        uiControlActions.UI.Pause.started += PauseGame;
        uiControlActions.UI.NormalSpeed.started += SetNormalSpeed;
        uiControlActions.UI.DoubleSpeed.started += SetDoubleSpeed;
        uiControlActions.UI.TripleSpeed.started += SetTripleSpeed;
        uiControlActions.UI.Enable();

        ControlsManager.UIControlsUpdated += UpdateSpeedBindings;
    }

    private void UpdateSpeedBindings(string rebinds)
    {
        uiControlActions.LoadBindingOverridesFromJson(rebinds);
    }

    private void Initialize()
    {
        if (!SaveLoadManager.Loading)
            return;

        timeOfDay = 20;
        normalizedTime = 0.2f;
        UpdateAmbientLight();
    }

    private void OnDisable()
    {
        UnitManager.unitPlaced -= FirstUnitPlaced;
        EnemySpawnManager.AllEnemiesKilled -= NightComplete;
        BuildingTutorialComplete.buildingTutorialComplete -= StartClock;
        StateOfTheGame.TutorialSkipped -= TutorialSkipped;
        DOTween.Kill(this,true);

        if (pauseButton != null)
        {
            pauseButton.RemoveClickListeners();
            normalSpeedButton.RemoveClickListeners();
            doubleSpeedButton.RemoveClickListeners();
            tripleSpeedButton.RemoveClickListeners();
        }

        uiControlActions.UI.Pause.started -= PauseGame;
        uiControlActions.UI.NormalSpeed.started -= SetNormalSpeed;
        uiControlActions.UI.DoubleSpeed.started -= SetDoubleSpeed;
        uiControlActions.UI.TripleSpeed.started -= SetTripleSpeed;
        uiControlActions.UI.Disable();

        ControlsManager.UIControlsUpdated -= UpdateSpeedBindings;
    }

    private void TutorialSkipped()
    {
        dayNightState = DayNightState.Day;
        totalTime = 0f;
        percentLeft?.Invoke(0, dayNightState == DayNightState.Day);
        paused = false;
    }

    private void StartClock()
    {
        SetGameSpeed(1);
        dayNightState = DayNightState.Day;
        totalTime = 0.94f * DayLength;
        percentLeft?.Invoke(0.94f, dayNightState == DayNightState.Day);
        paused = false;
    }

    [Button]
    private void NightComplete()
    {
        nightComplete = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (paused || SaveLoadManager.Loading)
            return;

        if (!paused && dayNightState == DayNightState.Day)
            totalTime += Time.deltaTime * Mathf.Round(GameConstants.GameSpeed);

        timeOfDay = totalTime - dayNumber * DayLength;
        normalizedTime = dayNightState == DayNightState.Day ? timeOfDay / DayLength : 1;
        dayNumber = Mathf.FloorToInt(totalTime / DayLength);
        sunLight.color = sunGradient.Evaluate(normalizedTime);
        if(dayNumber > 0 || normalizedTime > 0.1f) //start game at full brightness
            sunLight.intensity = sunIntensity.Evaluate(normalizedTime);

        UpdateClock();
        UpdateAmbientLight();

        if (dayNightState == DayNightState.Day)
            percentLeft?.Invoke(1f - normalizedTime, dayNightState == DayNightState.Day);
        else
            percentLeft?.Invoke(0f, dayNightState == DayNightState.Day);
    }

    private void SetSunIntensityCurve()
    {
        var keys = sunIntensity.keys;

        keys[0].value = nightIntensity;
        keys[1].value = dayIntensity;
        keys[2].value = dayIntensity;
        keys[3].value = nightIntensity;

        sunIntensity.keys = keys;
    }

    private void UpdateClock()
    {
        secondRemaining = DayLength - Mathf.RoundToInt(DayLength * normalizedTime);
        secondsPast = DayLength - secondsPast;

        if (dayNightState == DayNightState.Night && nightComplete) //if it's night and night is complete we switch to daytime
        {
            nightComplete = false;
            normalizedTime = 0f;
            dayNightState = DayNightState.Transitioning;
            transitionToDay?.Invoke(dayNumber, transitionDelay);
            StartCoroutine(TransitionToDay());
        }
        else if (normalizedTime >= 1f && dayNightState == DayNightState.Day)
        {
            dayNightState = DayNightState.Transitioning;
            transitionToNight?.Invoke(dayNumber, transitionDelay);
            StartCoroutine(TransitionToNight());
        }
    }

    private IEnumerator TransitionToDay()
    {
        yield return new WaitForSeconds(1f);

        if(dayNightState == DayNightState.Day) //sometimes gets called twice
            yield break;

        dayNightState = DayNightState.Day;
        toggleDay?.Invoke(dayNumber);
        SetGameSpeed(speedWhenNightStarted, false);
    }
    
    private IEnumerator TransitionToNight()
    {
        yield return new WaitForSeconds(transitionDelay);
        if(GameConstants.GameSpeed > 1f)
        {
            MessagePanel.ShowMessage("Night Game Speed: 1x", null);
            speedWhenNightStarted = (int)GameConstants.GameSpeed;
            SetGameSpeed(1, false);
        }
        dayNightState = DayNightState.Night;
        toggleNight?.Invoke(dayNumber);
        normalizedTime = 0f;
    }

    private void UpdateAmbientLight()
    {
        if(!adjustAmbientLight)
            return;

        Color ambientColor = skycolor.Evaluate(normalizedTime);

        RenderSettings.ambientSkyColor = ambientColor;
        RenderSettings.ambientEquatorColor = equatorcolor.Evaluate(normalizedTime);
        RenderSettings.ambientGroundColor = groundcolor.Evaluate(normalizedTime);
    }
    
    public static float GetTotalCycleLength()
    {
        return Instance.DayLength;
    }

    private void FirstUnitPlaced(Unit obj)
    {
        //paused = false;
        //UnitManager.unitPlaced -= FirstUnitPlaced;
    }

    [ButtonGroup("")]
    private void QuickDay()
    {
        dayLength = 20;
    }
    
    [ButtonGroup("")]
    private void NormalDay()
    {
        dayLength = 240;
    }

    public enum DayNightState
    {
        Day,
        Night,
        Transitioning
    }

    [Button]
    public void SetGameSpeed(int speed = 1, bool showMessage = true)
    {
        if(WindowPopup.BlockWindowHotkeys && Keyboard.current.anyKey.isPressed)
            return;

        if (isNight && speed > 1)
        {
            MessagePanel.ShowMessage("Night Game Speed: 1x", null);
            SFXManager.PlaySFX(SFXType.error);
            return;
        }

        if (speed <= 0)
            speed = 1;
        
        if(speed >= 1 && DayNightManager.dayNumber < 5)
        {
            GameConstants.GameSpeed = (float)speed + 0.2f;
        }
        else
            GameConstants.GameSpeed = speed;

        Time.timeScale = 1f;

        if(pauseButton == null)
            return;

        switch (speed)
        {
            case 1:
                pauseBlock.Color = unselectedColor;
                normalSpeedBlock.Color = SelectedColor;
                doubleSpeedBlock.Color = unselectedColor;
                tripleSpeedBlock.Color = unselectedColor;
                if (showMessage)
                    MessagePanel.ShowMessage("Game Speed: 1x", null);
                break;
            case 2:
                pauseBlock.Color = unselectedColor;
                normalSpeedBlock.Color = unselectedColor;
                doubleSpeedBlock.Color = SelectedColor;
                tripleSpeedBlock.Color = unselectedColor;
                if(showMessage)
                    MessagePanel.ShowMessage("Game Speed: 2x", null);
                break;
            case 3:
                pauseBlock.Color = unselectedColor;
                normalSpeedBlock.Color = unselectedColor;
                doubleSpeedBlock.Color = unselectedColor;
                tripleSpeedBlock.Color = SelectedColor;
                if(showMessage)
                    MessagePanel.ShowMessage("Game Speed: 3x", null);
                break;
            default:
                break;
        }
    }
    public void SetPause(bool pause, bool showMessage = true)
    {
        if (pause)
        {
            speedWhenPaused = (int)GameConstants.GameSpeed;
            PauseGame(showMessage);
        }
        else
        {
            Time.timeScale = 1f;
            if (isDay)
                SetGameSpeed(speedWhenNightStarted, showMessage);
            else
                SetGameSpeed(speedWhenPaused, showMessage);
        }
    }
    private void PauseGame()
    {
        PauseGame(true);
    }

    private void PauseGame(bool showMessage = true)
    {
        if(showMessage)
            MessagePanel.ShowMessage("Game Paused", null);

        Time.timeScale = 0f;

        if (pauseButton == null)//happens in the start scene
            return;

        pauseButton.GetComponent<UIBlock2D>().Color = SelectedColor;
        normalSpeedButton.GetComponent<UIBlock2D>().Color = unselectedColor;
        doubleSpeedButton.GetComponent<UIBlock2D>().Color = unselectedColor;
        tripleSpeedButton.GetComponent<UIBlock2D>().Color = unselectedColor;
    }

    private void PauseGame(InputAction.CallbackContext context)
    {
        if (WindowPopup.BlockWindowHotkeys && Keyboard.current.anyKey.isPressed)
            return; 

        if(Time.timeScale == 0f)
        {
            SetGameSpeed(Mathf.RoundToInt(GameConstants.GameSpeed));
        }
        else
        {
            speedWhenPaused = (int)GameConstants.GameSpeed;
            PauseGame();
        }
    }

    private void SetTripleSpeed(InputAction.CallbackContext context)
    {
        SetGameSpeed(3);
    }

    private void SetDoubleSpeed(InputAction.CallbackContext context)
    {
        SetGameSpeed(2);
    }

    private void SetNormalSpeed(InputAction.CallbackContext context)
    {
        SetGameSpeed(1);
    }

    private const string DAY_NUMBER = "DayNumber";
    private const string TIME_OF_DAY = "TimeOfDay";
    private const string DAY_NIGHT_STATE = "DayNightState";
    private const string PAUSED = "Paused";

    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this);
    }

    public void Save(string savePath, ES3Writer writer)
    {
        writer.Write<int>(DAY_NUMBER, dayNumber);
        writer.Write<float>(TIME_OF_DAY, totalTime);
        writer.Write<int>(DAY_NIGHT_STATE, (int)dayNightState);
        writer.Write<bool>(PAUSED, paused);
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if(ES3.KeyExists(DAY_NUMBER, loadPath))
            dayNumber = ES3.Load<int>(DAY_NUMBER, loadPath);
        if(ES3.KeyExists(TIME_OF_DAY, loadPath))
            totalTime = ES3.Load<float>(TIME_OF_DAY, loadPath);
        if(ES3.KeyExists(DAY_NIGHT_STATE, loadPath))
            dayNightState = (DayNightState)ES3.Load<int>(DAY_NIGHT_STATE, loadPath);
        if(ES3.KeyExists(PAUSED, loadPath))
            paused = ES3.Load<bool>(PAUSED, loadPath);

        if(dayNightState != DayNightState.Day)
        {
            dayNightState = DayNightState.Day;
            totalTime = dayNumber * DayLength - 20f;
        }

        if(totalTime >= (dayNumber + 1) * DayLength - 20f)
            totalTime = (dayNumber + 1) * DayLength - 20f;

        yield return null;
    }
}
