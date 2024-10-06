using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private GameObject sfxPrefab;
    private static ObjectPool<AudioPoolObject> sfxPool;

    [SerializeField] private AudioClip startScene;
    [SerializeField] private List<AudioClip> dayTimeClips = new List<AudioClip>();
    [SerializeField] private List<AudioClip> nightTimeClips = new List<AudioClip>();
    private List<AudioSource> audioSources = new List<AudioSource>();
    private AudioSource currentAudio;
    [SerializeField,EnableIf("@false")] private AudioClip currentClip;
    [SerializeField] private AudioMixerGroup musicAudioMixer;

    [Header("Mixers")]
    [SerializeField] private AudioMixer musicVolume;
    [SerializeField] private AudioMixer sfxVolume;
    [SerializeField] private AudioMixer voiceVolume;
    private bool isPlayingDay;
    private Coroutine waitUntilDone;

    private void Awake()
    {
        sfxPool = new ObjectPool<AudioPoolObject>(sfxPrefab);
    }

    private void Start()
    {
        if (ES3.FileExists(GameConstants.preferencesPath))
        {
            //slider values of 0.001 to 1 are saved then converted to decibels
            AudioListener.volume = ES3.Load<float>("masterVolume", GameConstants.preferencesPath, 1f);
            musicVolume.SetVolume(ES3.Load<float>("musicVolume", GameConstants.preferencesPath, 0.25f));
            sfxVolume.SetVolume(ES3.Load<float>("sfxVolume", GameConstants.preferencesPath, 0.5f));
            voiceVolume.SetVolume(ES3.Load<float>("voiceVolume", GameConstants.preferencesPath, 0.65f));
        }
        else
        {
            AudioListener.volume = 1f;
            musicVolume.SetVolume(0.25f);
            sfxVolume.SetVolume(0.5f);
            voiceVolume.SetVolume(0.65f);
        }
    }

    [Button]
    private void ResetAudioPrefences()
    {
        
        AudioListener.volume = 1f;
        ES3.Save("musicVolume", 0.25f, GameConstants.preferencesPath);
        ES3.Save("sfxVolume", 0.5f, GameConstants.preferencesPath);
        ES3.Save("voiceVolume", 0.65f, GameConstants.preferencesPath);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private void OnDisable()
    {
        DayNightManager.transitionToDay -= PlayDayTime;
        DayNightManager.transitionToNight -= PlayNightTime;
        SceneManager.sceneLoaded -= SceneLoaded;
    }

    //private void Update()
    //{
    //    if (currentAudio.isPlaying)
    //        return;

    //    if (!DayNightManager.isNight)
    //        PlayDayTime();
    //    else
    //        PlayNightTime();
    //}

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.buildIndex == 0)
        {
            PlayIntroMusic();
            DayNightManager.transitionToDay -= PlayDayTime;
            DayNightManager.transitionToNight -= PlayNightTime;
        }
        else if (scene.buildIndex == 1) //don't like this
        {
            DayNightManager.transitionToDay += PlayDayTime;
            DayNightManager.transitionToNight += PlayNightTime;

            if (!DayNightManager.isNight)
                PlayDayTime();
            else
                PlayNightTime();
        }
    }

    private void PlayNightTime(int daynumber = 0, float delay = 0)
    {
        PlayFromList(nightTimeClips, delay);
    }

    private void PlayDayTime(int dayNumber = 0, float delay = 0)
    {
        PlayFromList(dayTimeClips, delay);
    }

    private void PlayIntroMusic()
    {
        PlayClip(startScene, 5f, true);
    }

    private void PlayFromList(List<AudioClip> clips, float fadeTime)
    {
        AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Count)];
        PlayClip(clip, fadeTime);
    }

    private void PlayClip(AudioClip clip, float fadeTime, bool looping = false)
    {
        if(clip == null)
            return;

        if(currentAudio)
            StartCoroutine(FadeAudio(currentAudio, 0f, fadeTime));
        currentAudio = GetAudioSource();
        if (currentAudio == null)
            return;

        if(waitUntilDone != null)
            StopCoroutine(waitUntilDone);

        currentAudio.clip = clip;
        currentClip = clip;
        currentAudio.loop = looping;
        currentAudio.Play();
        currentAudio.volume = 0f;
        StartCoroutine(FadeAudio(currentAudio, 1f, fadeTime));
        waitUntilDone = StartCoroutine(WaitUntilDone(currentAudio)); 
    }

    private IEnumerator WaitUntilDone(AudioSource audioSource)
    {
        AudioClip clip = audioSource.clip;
        yield return null;
        yield return new WaitForSeconds(audioSource.clip.length + 1f);

        if(clip != audioSource.clip)
            yield break;

        yield return null;
        if (!DayNightManager.isNight)
            PlayDayTime();
        else
            PlayNightTime();
    }

    public static void Play(AudioClip clip, float volume, Vector3 position)
    {
        if (sfxPool != null)
            sfxPool.Pull(position).PlayAudio(clip, volume);
    }

    public static void Play(AudioClip clip, float volume, float pitch, Vector3 position)
    {
        if(sfxPool != null)
            sfxPool.Pull(position).PlayAudio(clip, volume, pitch);
    }

    private AudioSource GetAudioSource()
    {
        if(this == null || this.gameObject == null)
            return null;

        for (int i = 0; i < audioSources.Count; i++)
        {
            if (audioSources[i] == null)
            {
                audioSources[i] = this.gameObject.AddComponent<AudioSource>();
                audioSources[i].outputAudioMixerGroup = musicAudioMixer;
                continue;
            }

            if (!audioSources[i].isPlaying && audioSources[i] != currentAudio)
                return audioSources[i];
        }

        AudioSource audioSource = this.gameObject.AddComponent<AudioSource>();
        audioSource.outputAudioMixerGroup = musicAudioMixer;
        audioSources.Add(audioSource);
        return audioSource;
    }

    private IEnumerator FadeAudio(AudioSource audioSource, float finalVolume, float fadeTime)
    {
        float volumePerSecond = (finalVolume - audioSource.volume) / fadeTime;
        float time = 0;
        while (time < fadeTime)
        {
            audioSource.volume += volumePerSecond * Time.deltaTime;
            time += Time.deltaTime;
            yield return null;
        }

        audioSource.volume = finalVolume;
        if (finalVolume == 0f)
            audioSource.Stop();
    }

}
