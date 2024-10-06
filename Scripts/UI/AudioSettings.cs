using NovaSamples.UIControls;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class AudioSettings : WindowPopup
{
    [Header("Audio Sliders")]
    [SerializeField]
    private Slider masterVolume;
    [SerializeField]
    private Slider musicVolume;
    [SerializeField]
    private Slider sfxVolume;
    [SerializeField]
    private Slider uiVolume;
    [SerializeField]
    private Slider voiceVolume;

    [Header("Audio Mixers")]
    [SerializeField]
    private AudioMixer musicAudioMixer;
    [SerializeField]
    private AudioMixer sfxAudioMixer;
    [SerializeField]
    private AudioMixer uiAudioMixer;
    [SerializeField]
    private AudioMixer voiceAudioMixer;

    private new void OnEnable()
    {
        if (novaGroup == null)
            novaGroup = this.GetComponent<NovaGroup>();

        masterVolume.OnValueChanged.AddListener((UnityAction) => AdjustMasterVolume(masterVolume.Value));
        musicVolume.OnValueChanged.AddListener((UnityAction) => musicAudioMixer.SetVolume(musicVolume.Value));
        sfxVolume.OnValueChanged.AddListener((UnityAction) => sfxAudioMixer.SetVolume(sfxVolume.Value));
        uiVolume.OnValueChanged.AddListener((UnityAction) => uiAudioMixer.SetVolume(uiVolume.Value));
        voiceVolume.OnValueChanged.AddListener((UnityAction) => voiceAudioMixer.SetVolume(voiceVolume.Value));

        if (ES3.FileExists(GameConstants.preferencesPath))
        {
            //slider values are saved then converted to decibels when slider value changes... ugh.
            masterVolume.Value = ES3.Load<float>("masterVolume", GameConstants.preferencesPath, 0.7f);
            musicVolume.Value = ES3.Load<float>("musicVolume", GameConstants.preferencesPath, 0.2f);
            sfxVolume.Value = ES3.Load<float>("sfxVolume", GameConstants.preferencesPath, 0.7f);
            uiVolume.Value = ES3.Load<float>("uiVolume", GameConstants.preferencesPath, 0.75f);
            voiceVolume.Value = ES3.Load<float>("voiceVolume", GameConstants.preferencesPath, 1f);
        }

        CloseWindow();
    }

    private new void OnDisable()
    {
        if (ES3.FileExists(GameConstants.preferencesPath))
        {
            //saving slider values - will be converted to decibels on load
            ES3.Save<float>("masterVolume",masterVolume.Value, GameConstants.preferencesPath);
            ES3.Save<float>("musicVolume", musicVolume.Value, GameConstants.preferencesPath);
            ES3.Save<float>("sfxVolume", sfxVolume.Value, GameConstants.preferencesPath);
            ES3.Save<float>("uiVolume", uiVolume.Value, GameConstants.preferencesPath);
            ES3.Save<float>("voiceVolume", voiceVolume.Value, GameConstants.preferencesPath);
        }
    }
    
    public override void CloseWindow()
    {
        base.CloseWindow();
    }

    protected void ToggleWindow(InputAction.CallbackContext obj)
    {
        if(instanceIsOpen)
            CloseWindow();
        else if(!isOpen)
            OpenWindow();
    }

    public override void OpenWindow()
    {
        if (instanceIsOpen || isOpen)
            return;

        base.OpenWindow();
    }

    public void AdjustMasterVolume(float value)
    {
        AudioListener.volume = value;
    }
}
