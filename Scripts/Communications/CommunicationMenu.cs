using Nova;
using NovaSamples.UIControls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CommunicationMenu : WindowPopup
{
    [Header("UI Bits")]
    [SerializeField]private TextBlock textBlock;
    [SerializeField]private Button muteButton;
    private UIBlock2D muteButtonIcon;
    [SerializeField]private Button backButton;
    [SerializeField]private Button forwardButton;
    [Header("Other")]
    [SerializeField]private AudioSource audioSource;
    private Communication currentlyPlaying;
    private static List<Communication> Communications = new List<Communication> { };
    private static List<Communication> NeedToPlay = new List<Communication>();
    private float timeLeft = 0;
    private WaitForSeconds waitTime = new WaitForSeconds(1f);
    private int currentIndex = 0;
    public bool isPlaying => audioSource.isPlaying;
    private static CommunicationMenu instance;
    private DayNightManager dayNightManager;

    [Header("Icons")]
    [SerializeField] private Texture2D volumeOnIcon;
    [SerializeField] private Texture2D volumeOffIcon;

    [Header("Avatar")]
    [SerializeField] private UIBlock2D avatarBlock;
    [SerializeField] private Texture2D defaultAvatar;

    private HexTechTree techTree;


    private void Awake()
    {
        Communications.Clear();
        instance = this;
        muteButtonIcon = muteButton.GetComponent<UIBlock2D>();
        dayNightManager = FindObjectOfType<DayNightManager>();

        if (ES3.FileExists(GameConstants.preferencesPath))
            SetToggle(ES3.Load<bool>("MuteVoiceOver", GameConstants.preferencesPath, false));

        techTree = FindObjectOfType<HexTechTree>(true);
    }

    public override void OnEnable()
    {
        muteButton.OnClicked.AddListener(ToggleMute);
        backButton.OnClicked.AddListener(Back);
        forwardButton.OnClicked.AddListener(Forward);

        base.CloseWindow();
        base.OnEnable();
    }

    public override void OnDisable()
    {
        muteButton.OnClicked.RemoveAllListeners();
        backButton.OnClicked.RemoveAllListeners();
        forwardButton.OnClicked.RemoveAllListeners();

        base.OnDisable();
        StopAllCoroutines();
    }

    private void Update()
    {
        //avoid playing while tech tree is open
        if (techTree.instanceIsOpen)
            return;

        //is there anything to play or are we already playing?
        if (NeedToPlay.Count == 0 || audioSource.isPlaying || !Application.isFocused)
            return;

        //wait 2 seconds since last comm finished
        if (Communications.Count > 0 && Communications[^1].finishAt + 2 > Time.realtimeSinceStartup)
            return;

        //check for more urgent comms
        foreach (var comm in NeedToPlay)
        {
            if(!comm.waitUntilDay)
            {
                NeedToPlay.Remove(comm);
                Communications.Add(comm);
                PlayCommunication(comm);
                return;
            }
        }

        //give the player a few seconds after combat before playing
        if (DayNightManager.secondsPast > 8 && DayNightManager.DayNumber >= 1)
            return;

        //ensure we have enough time to listen to message
        if (NeedToPlay[0].playLength + 20 > DayNightManager.secondRemaining && DayNightManager.DayNumber >= 1)
            return;

        //pasted all the tests so play!
        Communication playNext = NeedToPlay[0];
        Communications.Add(playNext);
        NeedToPlay.Remove(playNext); 
        PlayCommunication(playNext);
    }

    public static void AddCommunication(CommunicationBase communication, bool waitUntilDay = true, Action callback = null)
    {
        if (instance == null)
            return;

        if(communication == null)
            return;

        if (Communications.Any(c => c.communication == communication && !c.communication.canPlayMoreThanOnce))
            return;

        NeedToPlay.Add(new Communication(communication, waitUntilDay, callback));
    }

    private void PlayCommunication(Communication comm, bool interruptAudio = false)
    {
        if (audioSource.isPlaying && !interruptAudio)
            return;



        CommunicationBase communication = comm.communication;

        communication.Initiallize();
        avatarBlock.SetImage(comm.communication.AvatarImage != null ? communication.AvatarImage : defaultAvatar);
        comm.Callback?.Invoke();
        OpenWindow();

        textBlock.Text = communication.Text;
        avatarBlock.SetImage(communication.AvatarImage != null ? communication.AvatarImage : defaultAvatar);
        audioSource.clip = comm.clip;
        currentIndex = Communications.IndexOf(comm);
        audioSource.Play();
        comm.startedAt = Time.realtimeSinceStartup;

        communication.Complete();

        if (communication.GetDirectives().Count > 0)
        {
            DirectiveBase lastDirective = communication.GetDirectives()[communication.GetDirectives().Count - 1];
            lastDirective.directiveCompleted += (d) => base.CloseWindow();
            lastDirective.directiveCompleted += (d) => audioSource.Stop();
            lastDirective.directiveCompleted += (d) => comm.FudgeStartTime();
        }
        else if(communication.GetDirectives().Count == 0 && communication.nextCommunication == null)
        {
            StartCoroutine(DelayClosing(15f));
        }
    }

    private IEnumerator DelayClosing(float delay)
    {
        yield return new WaitForSeconds(delay);
        base.CloseWindow();
    }

    private void ToggleMute()
    {
        audioSource.mute = !audioSource.mute;
        SetToggle(audioSource.mute);
        ES3.Save<bool>("MuteVoiceOver", audioSource.mute, GameConstants.preferencesPath);
    }

    private void SetToggle(bool mute)
    {
        audioSource.mute = mute;
        if (mute)
        {
            muteButtonIcon.SetImage(volumeOffIcon);
            muteButtonIcon.Color = ColorManager.GetColor(ColorCode.red);
        }
        else
        {
            muteButtonIcon.SetImage(volumeOnIcon);
            muteButtonIcon.Color = Color.white;
        }
    }

    private void Back()
    {
        if (currentIndex == 0)
            return;

        PlayCommunication(Communications[currentIndex - 1],true);
    }

    private void Forward()
    {
        if (currentIndex >= Communications.Count - 1)
            return;

        PlayCommunication(Communications[currentIndex + 1], true);
    }

    public override void OpenWindow()
    {
        base.OpenWindow();
    }

    public override void CloseWindow()
    {
        base.CloseWindow();
    }

    private class Communication
    {
        public Communication(CommunicationBase communication, bool waitUntilDay, Action callback)
        {
            this.communication = communication;
            this.waitUntilDay = waitUntilDay;
            this.Callback = callback;
        }

        public CommunicationBase communication;
        public bool waitUntilDay = true;
        public float startedAt = -1f;
        public float finishAt => startedAt + communication.AudioClip.length;
        public AudioClip clip => communication.AudioClip;
        public float playLength => communication.AudioClip.length;
        public Action Callback;

        /// <summary>
        /// Reset the start time so we don't have to wait for next clip to start playing
        /// </summary>
        public void FudgeStartTime()
        {
            startedAt = Time.realtimeSinceStartup - playLength;
        }
    }
}
