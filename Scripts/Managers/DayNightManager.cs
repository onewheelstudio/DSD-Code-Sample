using Sirenix.OdinInspector;
using System;
using UnityEngine;
using DG.Tweening;
using HexGame.Units;
using System.Collections;
using NovaSamples.UIControls;
using UnityEngine.InputSystem;
using Nova;

public class DayNightManager : MonoBehaviour
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
    public static bool isDay => instance.dayNightState == DayNightState.Day;
    public static bool isNight => instance.dayNightState == DayNightState.Night;
    private bool paused = false;
    private bool nightComplete = false;

    [SerializeField]
    private float dayIntensity = 0.5f;
    [SerializeField]
    private float nightIntensity = 0.2f;

    [SerializeField,DisableIf("@true"),ProgressBar(0f, 1f)]
    private float normalizedTime;

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

    public static event Action<int, float> transitionToDay;
    public static event Action<int> toggleDay;
    public static event Action<int, float> transitionToNight;
    public static event Action<int> toggleNight;
    public static event Action<float, bool> percentLeft;
    private float transitionDelay = 5f;

    [Header("Speed Controls")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button normalSpeedButton;
    [SerializeField] private Button doubleSpeedButton;
    [SerializeField] private Button tripleSpeedButton;
    [SerializeField] private Color unselectedColor;
    [SerializeField] private Color SelectedColor;

    private UIControlActions uiControlActions;

    private void Awake()
    {
        instance = this;
        //ensure that we have a normal day/night cycle in builds
        if (!Application.isEditor)
            NormalDay();

        percentLeft?.Invoke(0.5f, dayNightState == DayNightState.Day);

        if (dayNightState != DayNightState.Night)
            sunLight.DOIntensity(dayIntensity, DayLength / 10f);
        else
            sunLight.DOIntensity(nightIntensity, DayLength / 10f);

        sunLight.color = sunGradient.Evaluate(10f/DayLength);

        paused = true;
        dayNightState = DayNightState.Transitioning;

        uiControlActions = new UIControlActions();
        if(pauseButton != null)
        SetGameSpeed(1);

        CheatCodes.AddButton(QuickDay, "Quick Day");
        CheatCodes.AddButton(NormalDay, "Normal Day");
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
            pauseButton.clicked += PauseGame;
            normalSpeedButton.clicked += () => SetGameSpeed(1);
            doubleSpeedButton.clicked += () => SetGameSpeed(2);
            tripleSpeedButton.clicked += () => SetGameSpeed(3);
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
        timeOfDay = 20;
        normalizedTime = 0.2f;
        UpdateAmbientLight();
        sunLight.intensity = dayIntensity;
        //sunLight.color = sunGradient.Evaluate(normalizedTime);
    }

    private void OnDisable()
    {
        UnitManager.unitPlaced -= FirstUnitPlaced;
        EnemySpawnManager.AllEnemiesKilled -= NightComplete;
        BuildingTutorialComplete.buildingTutorialComplete -= StartClock;
        StateOfTheGame.TutorialSkipped -= TutorialSkipped;
        DOTween.Kill(this,true);
        DOTween.Kill(sunLight,true);
        
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
        if (paused)
            return;

        if (!paused && dayNightState == DayNightState.Day)
            totalTime += Time.deltaTime * Mathf.Round(GameConstants.GameSpeed);

        timeOfDay = totalTime - dayNumber * DayLength;
        normalizedTime = dayNightState == DayNightState.Day ? timeOfDay / DayLength : 1;
        dayNumber = Mathf.FloorToInt(totalTime / DayLength);
        sunLight.color = sunGradient.Evaluate(normalizedTime);

        UpdateClock();
        UpdateAmbientLight();

        if(dayNightState == DayNightState.Day)
            percentLeft?.Invoke(1f - normalizedTime, dayNightState == DayNightState.Day);
        else
            percentLeft?.Invoke(0f, dayNightState == DayNightState.Day);
    }

    private void UpdateClock()
    {
        secondRemaining = DayLength - Mathf.RoundToInt(DayLength * normalizedTime);
        secondsPast = DayLength - secondsPast;

        if (dayNightState != DayNightState.Day && nightComplete) //if it's night and night is complete we switch to daytime
        {
            nightComplete = false;
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
        dayNightState = DayNightState.Day;
        toggleDay?.Invoke(dayNumber);
        sunLight.DOIntensity(dayIntensity, DayLength / 10f);
    }
    
    private IEnumerator TransitionToNight()
    {
        yield return new WaitForSeconds(transitionDelay);
        if(GameConstants.GameSpeed > 1f)
        {
            MessagePanel.ShowMessage("Night Game Speed: 1x", null);
            SetGameSpeed(1);
        }
        dayNightState = DayNightState.Night;
        toggleNight?.Invoke(dayNumber);
        sunLight.DOIntensity(nightIntensity, DayLength / 10f);
        normalizedTime = 0f;
    }

    private void UpdateAmbientLight()
    {
        if(!adjustAmbientLight)
            return;

        RenderSettings.ambientSkyColor = skycolor.Evaluate(GetNormalizedTime());
        RenderSettings.ambientEquatorColor = equatorcolor.Evaluate(GetNormalizedTime());
        RenderSettings.ambientGroundColor = groundcolor.Evaluate(GetNormalizedTime());
    }

    [Button]
    private void Pause()
    {
        paused = !paused;
    }

    public float GetNormalizedTime()
    {
        return normalizedTime;
    }

    public static float GetTotalCycleLength()
    {
        if(DayNightManager.instance == null)
            DayNightManager.instance = FindObjectOfType<DayNightManager>();

        return instance.DayLength;
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
    public void SetGameSpeed(int speed = 1)
    {
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
                pauseButton.GetComponent<UIBlock2D>().Color = unselectedColor;
                normalSpeedButton.GetComponent<UIBlock2D>().Color = SelectedColor;
                doubleSpeedButton.GetComponent<UIBlock2D>().Color = unselectedColor;
                tripleSpeedButton.GetComponent<UIBlock2D>().Color = unselectedColor;
                MessagePanel.ShowMessage("Game Speed: 1x", null);
                break;
            case 2:
                pauseButton.GetComponent<UIBlock2D>().Color = unselectedColor;
                normalSpeedButton.GetComponent<UIBlock2D>().Color = unselectedColor;
                doubleSpeedButton.GetComponent<UIBlock2D>().Color = SelectedColor;
                tripleSpeedButton.GetComponent<UIBlock2D>().Color = unselectedColor;
                MessagePanel.ShowMessage("Game Speed: 2x", null);
                break;
            case 3:
                pauseButton.GetComponent<UIBlock2D>().Color = unselectedColor;
                normalSpeedButton.GetComponent<UIBlock2D>().Color = unselectedColor;
                doubleSpeedButton.GetComponent<UIBlock2D>().Color = unselectedColor;
                tripleSpeedButton.GetComponent<UIBlock2D>().Color = SelectedColor;
                MessagePanel.ShowMessage("Game Speed: 3x", null);
                break;
            default:
                break;
        }
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
        pauseButton.GetComponent<UIBlock2D>().Color = SelectedColor;
        normalSpeedButton.GetComponent<UIBlock2D>().Color = unselectedColor;
        doubleSpeedButton.GetComponent<UIBlock2D>().Color = unselectedColor;
        tripleSpeedButton.GetComponent<UIBlock2D>().Color = unselectedColor;
    }

    private void PauseGame(InputAction.CallbackContext context)
    {
        PauseGame();
        MessagePanel.ShowMessage("Game Paused", null);
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
}
