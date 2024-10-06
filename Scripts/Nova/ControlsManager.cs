using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Samples.RebindUI;

public class ControlsManager : WindowPopup
{
    public InputActionAsset cameraMovement;
    public InputActionAsset uiActions;
    private const string CONTROL_BINDINGS = "ControlBindings.ES3";

    public static event Action<string> CameraControlsUpdated;
    public static event Action<string> UIControlsUpdated;
    public static event Action ControlsLoaded;
    public static event Action ControlsReset;

    public override void OnEnable()
    {
        base.OnEnable();
        LoadBindings();
        RebindActionUI.RebindComplete += RebindComplete;
        base.CloseWindow();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        //SaveBindings();
        RebindActionUI.RebindComplete -= RebindComplete;
    }

    private void SaveBindings()
    {
        var rebinds = cameraMovement.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("cameraActionRebindings", rebinds);
        ES3.Save<string>("cameraActionRebindings", rebinds, CONTROL_BINDINGS);
        CameraControlsUpdated?.Invoke(rebinds);

        rebinds = uiActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("uiActionRebindings", rebinds);
        ES3.Save<string>("uiActionRebindings", rebinds, CONTROL_BINDINGS);
        UIControlsUpdated?.Invoke(rebinds);
    }

    private void LoadBindings()
    {
        if (!ES3.FileExists(CONTROL_BINDINGS))
            return;

        var rebinds = ES3.Load<string>("cameraActionRebindings", CONTROL_BINDINGS);
        if (!string.IsNullOrEmpty(rebinds))
            cameraMovement.LoadBindingOverridesFromJson(rebinds);

        rebinds = ES3.Load<string>("uiActionRebindings", CONTROL_BINDINGS);
        if (!string.IsNullOrEmpty(rebinds))
            uiActions.LoadBindingOverridesFromJson(rebinds);

    }

    public override void OpenWindow()
    {
        base.OpenWindow();
        ControlsLoaded?.Invoke();
    }

    public override void CloseWindow()
    {
        SaveBindings();
        base.CloseWindow();
    }

    private void RebindComplete(RebindActionUI uI)
    {
        SaveBindings();
    }

    public void ResetAllBindings()
    {
        ControlsReset();
    }
}
