using NovaSamples.UIControls;
using OWS.Nova;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class FirstTimeSetupWindow : WindowPopup
{
    [Header("Audio Sliders")]
    [SerializeField]
    private Slider musicVolume;
    [SerializeField]
    private Slider sfxVolume;
    [SerializeField] private Button testSFX;
    [SerializeField]
    private Slider uiVolume;
    [SerializeField] private Button testUI;
    [SerializeField]
    private Slider voiceVolume;
    [SerializeField] private Button testVoice;

    [Header("Audio Mixers")]
    [SerializeField]
    private AudioMixer musicAudioMixer;
    [SerializeField]
    private AudioMixer sfxAudioMixer;
    [SerializeField]
    private AudioMixer uiAudioMixer;
    [SerializeField]
    private AudioMixer voiceAudioMixer;

    [Header("Clips")]
    [SerializeField] private AudioClip sfxClip;
    [SerializeField] private AudioClip uiClip;
    [SerializeField] private AudioClip voiceClip;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource voiceSource;

    [Header("Controls")]
    [SerializeField] private ToggleSwitch edgeScrollingToggle;
    public static event Action<bool> DoEdgeScrolling;


    private new void OnEnable()
    {
        musicVolume.OnValueChanged.AddListener((UnityAction) => musicAudioMixer.SetVolume(musicVolume.Value));
        sfxVolume.OnValueChanged.AddListener((UnityAction) => sfxAudioMixer.SetVolume(sfxVolume.Value));
        uiVolume.OnValueChanged.AddListener((UnityAction) => uiAudioMixer.SetVolume(uiVolume.Value));
        voiceVolume.OnValueChanged.AddListener((UnityAction) => voiceAudioMixer.SetVolume(voiceVolume.Value));

        if (ES3.FileExists(GameConstants.preferencesPath))
        {
            //slider values are saved then converted to decibels when slider value changes... ugh.
            musicVolume.Value = ES3.Load<float>("musicVolume", GameConstants.preferencesPath, 0.2f);
            sfxVolume.Value = ES3.Load<float>("sfxVolume", GameConstants.preferencesPath, 0.7f);
            uiVolume.Value = ES3.Load<float>("uiVolume", GameConstants.preferencesPath, 0.75f);
            voiceVolume.Value = ES3.Load<float>("voiceVolume", GameConstants.preferencesPath, 1f);
        }
        else
        {
            musicVolume.Value = 0.2f;
            sfxVolume.Value = 0.7f;
            uiVolume.Value = 0.75f;
            voiceVolume.Value = 1f;
        }

        testSFX.Clicked += TestSFX;
        testUI.Clicked += TestUI;
        testVoice.Clicked += TestVoice;

        if (ES3.FileExists(GameConstants.preferencesPath))
        {
            edgeScrollingToggle.ToggledOn = ES3.Load<bool>("doEdgeScrolling", GameConstants.preferencesPath, false);
        }

        edgeScrollingToggle.Toggled += ToggleEdgeScrolling;

        if(ES3.Load<bool>("FirstTimeSetup", GameConstants.preferencesPath, false))
            CloseWindow();
    }

    private new void OnDisable()
    {
        ES3.Save<float>("musicVolume", musicVolume.Value, GameConstants.preferencesPath);
        ES3.Save<float>("sfxVolume", sfxVolume.Value, GameConstants.preferencesPath);
        ES3.Save<float>("uiVolume", uiVolume.Value, GameConstants.preferencesPath);
        ES3.Save<float>("voiceVolume", voiceVolume.Value, GameConstants.preferencesPath);
        ES3.Save<bool>("doEdgeScrolling", edgeScrollingToggle.ToggledOn, GameConstants.preferencesPath);
        ES3.Save<bool>("FirstTimeSetup", true, GameConstants.preferencesPath);

        testSFX.Clicked -= TestSFX;
        testUI.Clicked -= TestUI;
        testVoice.Clicked -= TestVoice;
    }

    private void TestSFX()
    {
        sfxSource.PlayOneShot(sfxClip);
    }

    private void TestUI()
    {
        uiSource.PlayOneShot(uiClip);
    }

    private void TestVoice()
    {
        if(!voiceSource.isPlaying)
            voiceSource.PlayOneShot(voiceClip);
    }

    public override void CloseWindow()
    {
        ES3.Save<bool>("FirstTimeSetup", true, GameConstants.preferencesPath);
        base.CloseWindow();
    }

    protected void ToggleWindow(InputAction.CallbackContext obj)
    {
        if (instanceIsOpen)
            CloseWindow();
        else if (!isOpen)
            OpenWindow();
    }

    public override void OpenWindow()
    {
        if (instanceIsOpen || isOpen)
            return;

        base.OpenWindow();
    }

    private void ToggleEdgeScrolling(ToggleSwitch @switch, bool value)
    {
        DoEdgeScrolling?.Invoke(value);
    }

    [Button]
    private void ResetFirstTime()
    {
        ES3.Save<bool>("FirstTimeSetup", false, GameConstants.preferencesPath);
    }
}
