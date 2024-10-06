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
    [SerializeField] private List<AudioClip> audioClipList;
    [SerializeField] private List<MessageData> messageDataList;
    [SerializeField] private UIBlock2D avatarBlock;
    [SerializeField] private TextBlock messageBlock;
    private AudioSource audioSource;

    public static event Action IntroComplete;

    private void Awake()
    {
        clipMask = this.GetComponent<ClipMask>();
        audioSource = this.GetComponent<AudioSource>();
        clipMask.SetAlpha(0f);
    }

    private void Start()
    {
        PlayIntro();
        clickToContinue.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        UpdateProgress(0f);
        WorldLevelManager.loadingProgress += UpdateProgress;
        WorldLevelManager.loadingStart += StartLoading;
        WorldLevelManager.loadingEnd += EndLoading;
        LandmassCreator.generationStarted += StartLoading;
        LandmassCreator.generationProgress += UpdateProgress;
        LandmassCreator.generationComplete += EndLoading;
        clipMask.DoFade(1f, 0.2f);

        LoadMessage(0);
    }

    private void OnDisable()
    {
        WorldLevelManager.loadingProgress -= UpdateProgress;
        WorldLevelManager.loadingStart -= StartLoading;
        WorldLevelManager.loadingEnd -= EndLoading;
        LandmassCreator.generationStarted -= StartLoading;
        LandmassCreator.generationProgress -= UpdateProgress;
        LandmassCreator.generationComplete -= EndLoading;

        DOTween.Kill(clickToContinue.gameObject);
        DOTween.Kill(this, true);
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

    private void UpdateProgress(float value, string text = "")
    {
        progressSlider.Value = value * 100f;
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
    private void PlayIntro()
    {
        audioClipIndex = 0;
        backgroundClipMask.SetAlpha(0f);
        messageClipMask.SetAlpha(0f);
        StartCoroutine(DoIntro());
    }

    private IEnumerator DoIntro()
    {
        StartCoroutine(FadeIn(backgroundClipMask));
        yield return StartCoroutine(FadeIn(messageClipMask));
        StartCoroutine(LoadAsync());

        PlayMessage(messageDataList[audioClipIndex]);
        audioClipIndex++;
        float timeToStop = Time.realtimeSinceStartup + audioSource.clip.length + 0.5f;
        yield return new WaitUntil(() => Time.realtimeSinceStartup > timeToStop || Mouse.current.leftButton.wasPressedThisFrame);
        yield return null;

        PlayMessage(messageDataList[audioClipIndex]);
        audioClipIndex++;
        timeToStop = Time.realtimeSinceStartup + audioSource.clip.length + 0.5f;
        yield return new WaitUntil(() => Time.realtimeSinceStartup > timeToStop || Mouse.current.leftButton.wasPressedThisFrame);
        yield return null;

        PlayMessage(messageDataList[audioClipIndex]);
        audioClipIndex++;
        timeToStop = Time.realtimeSinceStartup + audioSource.clip.length + 0.5f;
        yield return new WaitUntil(() => Time.realtimeSinceStartup > timeToStop || Mouse.current.leftButton.wasPressedThisFrame);
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
        Tween tween = clickToContinue.DoFade(0f, 1f).SetLoops(-1, LoopType.Yoyo);
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
