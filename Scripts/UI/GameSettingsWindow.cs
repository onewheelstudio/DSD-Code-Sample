using Nova;
using NovaSamples.UIControls;
using OWS.Nova;
using System;
using UnityEngine;

public class GameSettingsWindow : WindowPopup
{
    [SerializeField] private Slider UIScaling;
    [SerializeField] private ScreenSpace mainUICanvas;
    
    [SerializeField] private ToggleSwitch gameHintsToggle;
    [SerializeField] private ToggleSwitch gameTipsToggle;
    public static event Action<bool> showGameHints;
    public static event Action<bool> showGameTips;



    private void Awake()
    {
        UIScaling.OnValueChanged.AddListener((UnityAction) => AdjustUIScaling(UIScaling.Value));

        if (ES3.FileExists(GameConstants.preferencesPath))
        {
            UIScaling.Value = ES3.Load<float>("uiScaling", GameConstants.preferencesPath, 1f);
            gameHintsToggle.ToggledOn = ES3.Load<bool>("showGameHints", GameConstants.preferencesPath, true);
            gameTipsToggle.ToggledOn = ES3.Load<bool>("showGameTips", GameConstants.preferencesPath, true);
            ShowGameHints(gameHintsToggle, gameHintsToggle.ToggledOn);
            ShowGameTips(gameTipsToggle, gameTipsToggle.ToggledOn);
        }
    }

    private new void OnEnable()
    {
        base.OnEnable();
        gameHintsToggle.Toggled += ShowGameHints;
        gameTipsToggle.Toggled += ShowGameHints;
        CloseWindow();
    }

    private new void OnDisable()
    {
        ES3.Save<float>("uiScaling", UIScaling.Value, GameConstants.preferencesPath);
        gameHintsToggle.Toggled -= ShowGameHints;
        ES3.Save<bool>("showGameHints", gameHintsToggle.ToggledOn, GameConstants.preferencesPath);
        ES3.Save<bool>("showGameTips", gameHintsToggle.ToggledOn, GameConstants.preferencesPath);
        base.OnDisable();
    }

    private void AdjustUIScaling(float value)
    {
        value = Mathf.Clamp(value, 0f, 2f);
        float width = 1920f * (-0.5f * value + 1.5f);
        mainUICanvas.ReferenceResolution = new Vector2(width, 1080f);
    }

    private void ShowGameHints(ToggleSwitch toggle, bool isOn)
    {
        showGameHints?.Invoke(isOn);
    }
    
    private void ShowGameTips(ToggleSwitch toggle, bool isOn)
    {
        showGameTips?.Invoke(isOn);
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
