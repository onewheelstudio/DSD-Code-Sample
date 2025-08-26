using OWS.Nova;
using Sirenix.OdinInspector;
using UnityEngine;

public class DataSharingWindow : WindowPopup
{
    [SerializeField] private ToggleSwitch shareGameData;
    [SerializeField] private ToggleSwitch shareSystemInfo;

    private void Start()
    {
        CloseWindow();

        //bool shownBefore = ES3.Load<bool>("DataSharingWindowShownBefore", GameConstants.preferencesPath,false);

        //if (shownBefore)
        //    CloseWindow();
        //else
        //    OpenWindow();
    }

    public override void OnEnable()
    {
        shareGameData.Toggled += SavePreferences;
        shareSystemInfo.Toggled += SavePreferences;
        LoadPreferences();
        base.OnEnable();
    }

    public override void OnDisable()
    {
        shareGameData.Toggled -= SavePreferences;
        shareSystemInfo.Toggled -= SavePreferences;
        SavePreferences();
        ES3.Save<bool>("DataSharingWindowShownBefore", true, GameConstants.preferencesPath);
        base.OnDisable();
    }

    private void SavePreferences(ToggleSwitch @switch, bool arg2)
    {
        SavePreferences();
    }

    private void LoadPreferences()
    {
        bool _shareGameData = ES3.Load<bool>("ShareGameData", GameConstants.preferencesPath, true);
        bool _shareSystemInfo = ES3.Load<bool>("ShareSystemInfo", GameConstants.preferencesPath, true);
        shareGameData.SetValueWithOutCallback(_shareGameData);
        shareSystemInfo.SetValueWithOutCallback(_shareSystemInfo);
    }
    private void SavePreferences()
    {
        ES3.Save<bool>("ShareGameData", shareGameData.ToggledOn, GameConstants.preferencesPath);
        ES3.Save<bool>("ShareSystemInfo", shareSystemInfo.ToggledOn, GameConstants.preferencesPath);
    }

    public override void CloseWindow()
    {
        SavePreferences();
        base.CloseWindow();
    }

    [Button]
    private void ResetShown()
    {
        ES3.Save<bool>("DataSharingWindowShownBefore", false, GameConstants.preferencesPath);
    }
}
