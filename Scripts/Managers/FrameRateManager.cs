using Nova;
using NovaSamples.UIControls;
using OWS.Nova;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class FrameRateManager : MonoBehaviour
{
    [SerializeField] private int VSyncCount = 1;
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private ToggleSwitch useVsync;
    [SerializeField] private Transform frameRateParent;
    [SerializeField] private Slider targetFrameRateSlider;
    [SerializeField] private TextBlock fpsDisplay;
    [SerializeField] private int[] fpsOptions = new int[] { 30, 60, 90, 120, 144, 240 };

    private void Awake()
    {
        VSyncCount = PlayerPrefs.GetInt("UseVsync", 0);
        targetFrameRate = PlayerPrefs.GetInt("TargetFrameRate", 60);
        useVsync.SetValueWithOutCallback(VSyncCount == 1);
        targetFrameRateSlider.Value = Array.IndexOf(fpsOptions, targetFrameRate);
        fpsDisplay.Text = targetFrameRate.ToString();
        frameRateParent.gameObject.SetActive(VSyncCount == 0);
        SetFrameRateTarget();
    }

    private void OnEnable()
    {
        useVsync.Toggled += VsyncToggled;
        targetFrameRateSlider.ValueChanged += TargetFrameRateChanged;
    }

    private void TargetFrameRateChanged(float obj)
    {
        int index = (int)obj;
        targetFrameRate = fpsOptions[index];
        fpsDisplay.Text = targetFrameRate.ToString();
        PlayerPrefs.SetInt("TargetFrameRate", targetFrameRate);
        Application.targetFrameRate = targetFrameRate;
    }

    private void VsyncToggled(ToggleSwitch @switch, bool useVsync)
    {
        VSyncCount = useVsync ? 1 : 0;
        PlayerPrefs.SetInt("UseVsync", VSyncCount);
        QualitySettings.vSyncCount = VSyncCount;
        frameRateParent.gameObject.SetActive(!useVsync);
    }

    [Button]
    private void SetFrameRateTarget()
    {
        QualitySettings.vSyncCount = VSyncCount;
        Application.targetFrameRate = targetFrameRate;
    }

    [Button]
    private void MatchMonitor()
    {
        QualitySettings.vSyncCount = 0;
        VSyncCount = 0;
        Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
        targetFrameRate = Application.targetFrameRate;
    }
}
