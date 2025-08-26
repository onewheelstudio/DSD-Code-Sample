using DG.Tweening;
using Nova;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static LoadingScreen;

[RequireComponent(typeof(ClipMask))]
public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private TextBlock loadingText;
    [SerializeField] private TextBlock messageText;
    [SerializeField] private Slider progressSlider;
    private ClipMask clipMask;
    [SerializeField] private Camera loadingScreenCamera;
    private bool loadingIsComplete = false; 

    [Header("Intro")]
    private int audioClipIndex = 0;
    [SerializeField] private ClipMask backgroundClipMask;
    [SerializeField] private ClipMask messageClipMask;
    [SerializeField] private TextBlock clickToContinue;

    [Header("Message Bits")]
    [SerializeField] private List<MessageData> messageDataList;
    [SerializeField] private UIBlock2D avatarBlock;
    [SerializeField] private TextBlock messageBlock;
    private AudioSource audioSource;

    [Header("Loading Game Bits")]
    [SerializeField] private List<MessageData> loadingGameMessages;

    [Header("Mute Button")]
    [SerializeField] private Button muteButton;
    private UIBlock2D muteButtonIcon;
    [SerializeField] private Texture2D volumeOnIcon;
    [SerializeField] private Texture2D volumeOffIcon;
    private bool isMuted = false;

    public static event Action IntroComplete;
    private Tween clickToContinueTween;

    private void Awake()
    {
        clipMask = this.GetComponent<ClipMask>();
        audioSource = this.GetComponent<AudioSource>();
        clipMask.SetAlpha(0f);
        muteButtonIcon = muteButton.GetComponent<UIBlock2D>();
        isMuted = ES3.Load<bool>("loadingScreenMuted", GameConstants.preferencesPath, false);
        SetMute(isMuted);
        muteButton.gameObject.SetActive(false);
        UpdateProgessMessage("");
    }

    private void Start()
    {
        clickToContinue.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        UpdateProgressValue(0f);
        WorldLevelManager.loadingProgress += UpdateProgress;
        WorldLevelManager.loadingStart += StartLoading;
        WorldLevelManager.loadingEnd += EndLoading;
        LandmassGenerator.generationStarted += StartLoading;
        LandmassGenerator.generationProgress += UpdateProgress;
        LandmassGenerator.generationComplete += EndLoading;

        SaveLoadManager.LoadComplete += EndLoading;
        SaveLoadManager.UpdateProgress += UpdateProgressValue;
        SaveLoadManager.postUpdateMessage += UpdateProgessMessage;

        muteButton.Clicked += ToggleAudio;

        clipMask.DoFade(1f, 0.2f);

        LoadMessage(0);
    }



    private void OnDisable()
    {
        WorldLevelManager.loadingProgress -= UpdateProgress;
        WorldLevelManager.loadingStart -= StartLoading;
        WorldLevelManager.loadingEnd -= EndLoading;
        LandmassGenerator.generationStarted -= StartLoading;
        LandmassGenerator.generationProgress -= UpdateProgress;
        LandmassGenerator.generationComplete -= EndLoading;

        SaveLoadManager.LoadComplete -= EndLoading;
        SaveLoadManager.UpdateProgress -= UpdateProgressValue;
        SaveLoadManager.postUpdateMessage -= UpdateProgessMessage;

        muteButton.RemoveAllListeners();

        DOTween.Kill(this, true);
        clickToContinueTween.Kill();
        ES3.Save<bool>("loadingScreenMuted",isMuted, GameConstants.preferencesPath);
    }
    private void StartLoading()
    {
        clipMask.DoFade(1f, 0.2f);
        StartCoroutine(DoLoadingText());
        loadingScreenCamera.gameObject.SetActive(true);
    }

    private void EndLoading()
    {
        progressSlider.gameObject.SetActive(false);
        loadingIsComplete = true;
        ShowClickToContinue();
    }

    private void UpdateProgress(float value, string message)
    {
        progressSlider.Value = value * 100f;
        messageText.Text = message;
    }

    private void UpdateProgressValue(float value)
    {
        progressSlider.Value = value * 100f;
    }
    
    private void UpdateProgessMessage(string text = "")
    {
        messageText.Text = text;
    }

    private IEnumerator DoLoadingText()
    {
        while (true)
        {
            loadingText.Text = "Loading Level";
            yield return new WaitForSeconds(0.25f);
            loadingText.Text = "Loading Level.";
            yield return new WaitForSeconds(0.25f);
            loadingText.Text = "Loading Level..";
            yield return new WaitForSeconds(0.25f);
            loadingText.Text = "Loading Level...";
            yield return new WaitForSeconds(0.25f);
        }
    }

    [Button]
    public void PlayIntro()
    {
        audioClipIndex = 0;
        backgroundClipMask.SetAlpha(0f);
        messageClipMask.SetAlpha(0f);
        if(SaveLoadManager.Loading)
            StartCoroutine(DoGameLoadingIntro());
        else
            StartCoroutine(DoNewGameIntro());
    }

    private IEnumerator DoNewGameIntro()
    {
        StartCoroutine(FadeIn(backgroundClipMask));
        yield return StartCoroutine(FadeIn(messageClipMask));
        StartCoroutine(LoadAsync());
        StartCoroutine(WaitTurnOnMuteButton());

        PlayMessage(messageDataList[audioClipIndex]);
        audioClipIndex++;
        float timeToStop = Time.realtimeSinceStartup + audioSource.clip.length + 0.5f;
        yield return new WaitUntil(() => Time.realtimeSinceStartup > timeToStop || (Mouse.current.leftButton.wasPressedThisFrame && loadingIsComplete));
        yield return null;

        PlayMessage(messageDataList[audioClipIndex]);
        audioClipIndex++;
        timeToStop = Time.realtimeSinceStartup + audioSource.clip.length + 0.5f;
        yield return new WaitUntil(() => Time.realtimeSinceStartup > timeToStop || (Mouse.current.leftButton.wasPressedThisFrame && loadingIsComplete));
        yield return null;

        //PlayMessage(messageDataList[audioClipIndex]);
        //audioClipIndex++;
        //timeToStop = Time.realtimeSinceStartup + audioSource.clip.length + 0.5f;
        //yield return new WaitUntil(() => Time.realtimeSinceStartup > timeToStop || (Mouse.current.leftButton.wasPressedThisFrame && loadingIsComplete));
        //yield return null;
        //audioSource.Stop();

        yield return new WaitUntil(() => loadingIsComplete);
        StartCoroutine(FadeOut(backgroundClipMask));
        yield return StartCoroutine(FadeOut(messageClipMask));
        this.gameObject.SetActive(false);
        this.loadingScreenCamera.gameObject.SetActive(false);
        this.transform.parent.gameObject.SetActive(false);
        IntroComplete?.Invoke();
        Destroy(this.transform.parent.gameObject);
    }

    private IEnumerator DoGameLoadingIntro()
    {
        StartCoroutine(FadeIn(backgroundClipMask));
        StartCoroutine(WaitTurnOnMuteButton());
        float timeToStop = Time.realtimeSinceStartup + audioSource.clip.length + 0.5f;

        int messageToPlay = UnityEngine.Random.Range(0, loadingGameMessages.Count);
        PlayMessage(loadingGameMessages[messageToPlay]);
        StartCoroutine(FadeIn(messageClipMask));
        audioClipIndex++;
        timeToStop = Time.realtimeSinceStartup + audioSource.clip.length + 0.5f;
        yield return new WaitUntil(() => Time.realtimeSinceStartup > timeToStop || (Mouse.current.leftButton.wasPressedThisFrame && loadingIsComplete));
        yield return null;
        
        audioSource.Stop();

        yield return new WaitUntil(() => loadingIsComplete);
        StartCoroutine(FadeOut(backgroundClipMask));
        yield return StartCoroutine(FadeOut(messageClipMask));
        this.gameObject.SetActive(false);
        this.loadingScreenCamera.gameObject.SetActive(false);
        this.transform.parent.gameObject.SetActive(false);
        IntroComplete?.Invoke();
        Destroy(this.transform.parent.gameObject);
    }

    private IEnumerator FadeIn(ClipMask clipMask)
    {
        while (clipMask.Tint.a < 1f)
        {
            clipMask.SetAlpha(clipMask.Tint.a + Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator FadeOut(ClipMask clipMask)
    {
        while (clipMask.Tint.a > 0f)
        {
            clipMask.SetAlpha(clipMask.Tint.a - Time.deltaTime);
            yield return null;
        }
    }


    private void ShowClickToContinue()
    {
        clickToContinue.gameObject.SetActive(true);
        clickToContinueTween = clickToContinue.DoFade(0f, 1f).SetLoops(-1, LoopType.Yoyo);
    }

    IEnumerator LoadAsync()
    {
        int currentIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        AsyncOperation async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(1, UnityEngine.SceneManagement.LoadSceneMode.Single);

        while (!async.isDone)
        {
            yield return null;
        }

        //UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(0);
    }

    IEnumerator WaitTurnOnMuteButton()
    {
        yield return new WaitForSeconds(1f);
        while (FindFirstObjectByType<PCInputManager>() == null && loadingIsComplete == false)
        {
            yield return null;
        }
        muteButton.gameObject.SetActive(true);
    }

    private void ToggleAudio()
    {
        isMuted = !isMuted;
        SetMute(isMuted);
    }

    private void SetMute(bool isMuted)
    {
        this.isMuted = isMuted;
        audioSource.mute = this.isMuted;
        muteButtonIcon.SetImage(this.isMuted ? volumeOffIcon : volumeOnIcon);
    }

    [Button]
    private void LoadMessage(int message)
    {
        avatarBlock.SetImage(messageDataList[message].avatar);
        messageBlock.Text = messageDataList[message].message;
        audioSource.clip = messageDataList[message].audioClip;
    }

    private void PlayMessage(MessageData messageData)
    {
        avatarBlock.SetImage(messageData.avatar);
        messageBlock.Text = messageData.message;
        audioSource.clip = messageData.audioClip;
        audioSource.Play();
    }

    [System.Serializable]
    public class MessageData
    {
        [TextArea(2,10)]
        public string message;
        public Sprite avatar;
        public AudioClip audioClip;
    }
}
