using HexGame.Units;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[Manageable]
public class SFXManager : MonoBehaviour
{
    private static SFXManager sfxManager;
    private static List<AudioSource> audioSourceList = new List<AudioSource>();

    [TabGroup("UI")]
    [SerializeField]
    private SFX message;
    [SerializeField]
    [TabGroup("UI")]
    private SFX click;
    [TabGroup("Units")]
    [SerializeField]
    private SFX unitSelection;
    [TabGroup("Units")]
    [SerializeField]
    private SFX buildingPlacement;
    [TabGroup("Other")]
    [SerializeField, TabGroup("Other")]
    private SFX placeTile;
    [SerializeField, TabGroup("Other")]
    private SFX startNight;
    [SerializeField, TabGroup("Other")]
    private SFX startDay;
    [SerializeField, TabGroup("Other")]
    private SFX directiveComplete;
    [SerializeField, TabGroup("Other")]
    private SFX directiveUpdated;
    [SerializeField, TabGroup("Other")] private SFX directiveAdded;
    [SerializeField, TabGroup("Other")] private SFX newDirective;
    [SerializeField, TabGroup("Other")] private SFX error;
    [SerializeField, TabGroup("Other")] private SFX resourceReveal;
    [SerializeField, TabGroup("Enemies")] private SFX enemyDeath;

    //Audiosource pooling
    [SerializeField] private SFXAudioSource sfxAudioSourcePrefab;
    private static ObjectPool<SFXAudioSource> audioSourcePool;
    private static List<SFXAudioSource> activeSFXAudio = new List<SFXAudioSource>();

    private void Awake()
    {
        audioSourcePool = new ObjectPool<SFXAudioSource>(sfxAudioSourcePrefab, 5);
        activeSFXAudio = new List<SFXAudioSource>();
    }

    private void OnEnable()
    {
        if (sfxManager == null)
            sfxManager = this;

        UnitSelectionManager.unitSelected += PlayUnitSelected;
        UnitManager.unitPlaced += BuildingPlaced;
        DayNightManager.transitionToDay += PlayStartOfDay;
        DayNightManager.transitionToNight += PlayStartOfNight;
    }


    private void OnDisable()
    {
        UnitSelectionManager.unitSelected -= PlayUnitSelected;
        UnitManager.unitPlaced -= BuildingPlaced;
        DayNightManager.transitionToDay -= PlayStartOfDay;
        DayNightManager.transitionToNight -= PlayStartOfNight;
    }

    private void PlayUnitSelected(PlayerUnit unit)
    {
        unitSelection.PlayClip(GetAudioSource(), true);
    }

    private void BuildingPlaced(Unit obj)
    {
        if (obj is PlayerUnit playerUnit && playerUnit.unitType == PlayerUnitType.buildingSpot)
            buildingPlacement.PlayClip(GetAudioSource(), true);
        else
            Debug.Log("You need to add some finished building effects");
    }

    private void PlayStartOfNight(int dayNumber, float delay)
    {
        //startNight.PlayClip(GetAudioSource(), true);
        StartCoroutine(PlayDelayed(startNight, delay));
    }

    private void PlayStartOfDay(int dayNumber, float delay)
    {
        StartCoroutine(PlayDelayed(startDay, 2f));
    }

    private IEnumerator PlayDelayed(SFX sfx, float delay)
    {
        yield return new WaitForSeconds(delay);
        sfx.PlayClip(GetAudioSource(), true);
    }

    public static void PlaySFX(SFXType sfxType, bool interrupt = true)
    {
        if (sfxManager == null)
            return;

        sfxManager.Play(sfxType, interrupt);
    }

    private void Play(SFXType sfxType, bool interrupt = false)
    {
        SFX sfxToPlay = null;

        switch (sfxType)
        {
            case SFXType.tilePlace:
                sfxToPlay = placeTile;
                break;
            case SFXType.message:
                sfxToPlay = message;
                break;
            case SFXType.click:
                sfxToPlay = click;
                break;
            case SFXType.DirectiveAdded:
                sfxToPlay = directiveAdded;
                break;
            case SFXType.newDirective:
                sfxToPlay = newDirective;
                break;
            case SFXType.error:
                sfxToPlay = error;
                break;
            case SFXType.DirectiveComplete:
                sfxToPlay = directiveComplete;
                break;
            case SFXType.DirectiveUpdated:
                sfxToPlay = directiveUpdated;
                break;
            case SFXType.buildingPlace:
                break;
            case SFXType.unitSelection:
                break;
            case SFXType.ResourceReveal:
                sfxToPlay = resourceReveal;
                    break;
            case SFXType.enemyDeath:
                sfxToPlay = enemyDeath;
                break;
            default:
                break;
        }

        if (sfxToPlay != null)
            sfxToPlay.PlayClip(GetAudioSource(), interrupt);
    }

    private AudioSource GetAudioSource()
    {
        for (int i = 0; i < audioSourceList.Count; i++)
        {
            if (audioSourceList[i] == null)
            {
                audioSourceList[i] = this.gameObject.AddComponent<AudioSource>();
                continue;
            }

            if (!audioSourceList[i].isPlaying)
                return audioSourceList[i];
        }

        AudioSource audioSource = this.gameObject.AddComponent<AudioSource>();
        audioSourceList.Add(audioSource);
        return audioSource;
    }

    public static AudioSource PlaceSFXAudioSource(Vector3 position)
    {
        UpdateActiveSFXAudio();
        SFXAudioSource sfxAudioSource = audioSourcePool.Pull();
        sfxAudioSource.transform.position = position;
        activeSFXAudio.Add(sfxAudioSource);
        return sfxAudioSource.AudioSource;
    }

    private static void UpdateActiveSFXAudio()
    {
        for (int i = activeSFXAudio.Count - 2; i > 0; i--)
        {
            if (!activeSFXAudio[i].AudioSource.isPlaying)
            {
                activeSFXAudio[i].gameObject.SetActive(false);
                activeSFXAudio.RemoveAt(i);
            }
        }
    }

    [System.Serializable]
    public class SFX
    {
        public List<AudioClip> clips = new List<AudioClip>();
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0f, 0.2f)]
        public float volumeVariange = 0.05f;
        [Range(0f, 2f)]
        public float pitch = 1f;
        [Range(0f, 0.2f)]
        public float pitchVariange = 0.05f;
        [SerializeField]
        protected AudioMixer audioMixer;

        public SFX(SFX sfx)
        {
            clips = sfx.clips;
            volume = sfx.volume;
            volumeVariange = sfx.volumeVariange;
            pitch = sfx.pitch;
            pitchVariange = sfx.pitchVariange;
            audioMixer = sfx.audioMixer;
        }

        public void PlayClip(AudioSource audioSource, bool interrupt = false)
        {
            if (clips == null || clips.Count == 0)
                return;

            if (!audioSource.isPlaying || interrupt)
            {
                audioSource.clip = clips[Random.Range(0, clips.Count)];
                audioSource.volume = volume + Random.Range(-volumeVariange, volumeVariange);
                audioSource.pitch = pitch + Random.Range(-volumeVariange, volumeVariange);
                audioSource.outputAudioMixerGroup = audioMixer?.outputAudioMixerGroup;
                audioSource.Play();
            }
        }

        public void PlayClip(AudioSource audioSource, float setVolume = 0f, bool interrupt = false)
        {
            if (clips.Count == 0)
                return;

            if (setVolume <= 0.01f)
                setVolume = volume;

            if (!audioSource.isPlaying || interrupt)
            {
                audioSource.clip = clips[Random.Range(0, clips.Count)];
                audioSource.volume = setVolume + Random.Range(-volumeVariange, volumeVariange);
                audioSource.pitch = pitch + Random.Range(-volumeVariange, volumeVariange);
                audioSource.outputAudioMixerGroup = audioMixer?.outputAudioMixerGroup;
                audioSource.Play();
            }
        }



    }
}
public enum SFXType
{
    tilePlace,
    buildingPlace,
    message,
    click,
    unitSelection,
    DirectiveAdded,
    newDirective,
    error,
    DirectiveComplete,
    DirectiveUpdated,
    ResourceReveal,
    enemyDeath,
}
