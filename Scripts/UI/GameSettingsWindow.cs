using Nova;
using NovaSamples.UIControls;
using OWS.Nova;
using System;
using UnityEngine;

public class GameSettingsWindow : WindowPopup
{
    [Header("UI Scaling")]
    [SerializeField] private ToggleSwitch useUIScaling;
    [SerializeField] private Slider UIScaling;
    [SerializeField] private ScreenSpace mainUICanvas;

    [Header("Other Bits")]
    [SerializeField] private ToggleSwitch gameHintsToggle;
    [SerializeField] private ToggleSwitch gameTipsToggle;
    [SerializeField] private ToggleSwitch edgeScrollingToggle;
    [SerializeField] private Slider edgeScrollSpeed;

    [Header("Auto Save")]
    [SerializeField] private ToggleSwitch useAutoSave;
    [SerializeField] private Slider autoSaveInterval;
    [SerializeField] private TextBlock autoSaveIntervalText;
    
    
    public static event Action<float> EdgeScrollSpeedChanged;
    public static event Action<bool> showGameHints;
    public static event Action<bool> showGameTips;
    public static event Action<bool> DoEdgeScrolling;
    public static event Action<bool> UseAutoSave;
    public static event Action<int> AutoSaveIntervalChanged;

    private void Awake()
    {
        UIScaling.OnValueChanged.AddListener((UnityAction) => AdjustUIScaling(UIScaling.Value));
        edgeScrollSpeed.OnValueChanged.AddListener((UnityAction) => AdjustEdgeScrollSpeed(edgeScrollSpeed.Value));
        autoSaveInterval.OnValueChanged.AddListener((UnityAction) => AutoSaveIntervalSet(autoSaveInterval.Value));

        if (ES3.FileExists(GameConstants.preferencesPath))
        {
            UIScaling.Value = ES3.Load<float>("uiScalingValue", GameConstants.preferencesPath, 0.25f);
            useUIScaling.ToggledOn = ES3.Load<bool>("useUIScaling", GameConstants.preferencesPath, true);
            
            gameHintsToggle.ToggledOn = ES3.Load<bool>("showGameHints", GameConstants.preferencesPath, true);
            ShowGameHints(gameHintsToggle, gameHintsToggle.ToggledOn);
            
            gameTipsToggle.ToggledOn = ES3.Load<bool>("showGameTips", GameConstants.preferencesPath, true);
            ShowGameTips(gameTipsToggle, gameTipsToggle.ToggledOn);
            
            edgeScrollingToggle.ToggledOn = ES3.Load<bool>("doEdgeScrolling", GameConstants.preferencesPath, false);
            edgeScrollSpeed.Value = ES3.Load<float>("edgeScrollSpeed", GameConstants.preferencesPath, 2.25f);
            
            useAutoSave.ToggledOn = ES3.Load<bool>("useAutoSave", GameConstants.preferencesPath, true);
            autoSaveInterval.Value = ES3.Load<int>("autoSaveInterval", GameConstants.preferencesPath, 1);
            ToggleAutoSave(useAutoSave, useAutoSave.ToggledOn);
        }
    }

    private new void OnEnable()
    {
        base.OnEnable();
        useUIScaling.Toggled += ToggleUIScaling;
        gameHintsToggle.Toggled += ShowGameHints;
        gameTipsToggle.Toggled += ShowGameTips;
        edgeScrollingToggle.Toggled += ToggleEdgeScrolling;
        useAutoSave.Toggled += ToggleAutoSave;
        CloseWindow();
    }

    private new void OnDisable()
    {
        ES3.Save<float>("uiScalingValue", UIScaling.Value, GameConstants.preferencesPath);
        ES3.Save<bool>("uiScaling", useUIScaling.ToggledOn, GameConstants.preferencesPath);
        useUIScaling.Toggled -= ToggleUIScaling;
        gameHintsToggle.Toggled -= ShowGameHints;
        gameTipsToggle.Toggled -= ShowGameTips;
        ES3.Save<bool>("showGameHints", gameHintsToggle.ToggledOn, GameConstants.preferencesPath);
        ES3.Save<bool>("showGameTips", gameHintsToggle.ToggledOn, GameConstants.preferencesPath);
        ES3.Save<bool>("doEdgeScrolling", edgeScrollingToggle.ToggledOn, GameConstants.preferencesPath);
        ES3.Save<float>("edgeScrollSpeed", edgeScrollSpeed.Value, GameConstants.preferencesPath);

        ES3.Save<bool>("useAutoSave", useAutoSave.ToggledOn, GameConstants.preferencesPath);
        ES3.Save<int>("autoSaveInterval", (int)autoSaveInterval.Value, GameConstants.preferencesPath);

        base.OnDisable();
    }

    private void ToggleUIScaling(ToggleSwitch @switch, bool useUIScaling)
    {
        UIScaling.transform.parent.gameObject.SetActive(useUIScaling);
        if (!useUIScaling)
            AdjustUIScaling(1f);
    }

    private void AdjustUIScaling(float value)
    {
        value = Mathf.Clamp(value, 0f, 2f);
        float width = 1920f * (-0.5f * value + 1.5f);
        mainUICanvas.ReferenceResolution = new Vector2(width, 1080f);
    }

    private void AdjustEdgeScrollSpeed(float value)
    {
        value = Mathf.Clamp(value, 0.5f, 5f);
        EdgeScrollSpeedChanged?.Invoke(value);
    }

    private void ShowGameHints(ToggleSwitch toggle, bool isOn)
    {
        showGameHints?.Invoke(isOn);
    }
    
    private void ShowGameTips(ToggleSwitch toggle, bool isOn)
    {
        showGameTips?.Invoke(isOn);
    }

    private void ToggleEdgeScrolling(ToggleSwitch @switch, bool value)
    {
        DoEdgeScrolling?.Invoke(value);
        edgeScrollSpeed.transform.parent.gameObject.SetActive(value);
    }

    private void ToggleAutoSave(ToggleSwitch @switch, bool useAutoSave)
    {
        UseAutoSave?.Invoke(useAutoSave);
        autoSaveInterval.transform.parent.gameObject.SetActive(useAutoSave);
    }

    private void AutoSaveIntervalSet(float value)
    {
        if(value == 1)
            autoSaveIntervalText.Text = $"{value.ToString()} Day";
        else
            autoSaveIntervalText.Text = $"{value.ToString()} Days";
        AutoSaveIntervalChanged?.Invoke((int)value);
    }

    public override void OpenWindow()
    {
        blockWindowHotkeys = true;
        base.OpenWindow();
    }

    public override void CloseWindow()
    {
        blockWindowHotkeys = false;
        base.CloseWindow();
    }
}
