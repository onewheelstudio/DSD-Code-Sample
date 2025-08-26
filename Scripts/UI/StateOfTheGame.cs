using HexGame.Units;
using Nova;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateOfTheGame : WindowPopup, ISaveData
{
    public static event Action GameStarted;
    public static bool gameStarted = false;
    public Button closeButton;
    public Button skipTutorial;

    public GameSettings gameSettings;
    [SerializeField] private TextBlock messageBlock;
    [SerializeField, TextArea(3, 10)] private string demoMessage;
    [SerializeField, TextArea(3, 10)] private string earlyAccessMessage;

    [SerializeField] private List<TriggerBase> noTutorialTriggers = new();
    [SerializeField] private List<TriggerBase> afterHQTriggers = new();
    [SerializeField] private List<PlayerUnitType> unitsToUnlock = new();
    public static event Action TutorialSkipped;
    public static bool tutorialSkipped = false;

    [SerializeField] private UIBlock messageParent;
    [SerializeField] private UIBlock skipTutorialMessage;
    [SerializeField] private Button letsPlayButton;
    private bool skippedTutorialBefore = false;
    private const string TUTORIAL_SKIPPED_BEFORE= "TutorialSkippedBefore";

    private void Awake()
    {
        gameStarted = false;
        RegisterDataSaving();
        skippedTutorialBefore = PlayerPrefs.GetInt(TUTORIAL_SKIPPED_BEFORE, 0) == 1;
    }

    private void OnDestroy()
    {
        gameStarted = false;
        tutorialSkipped = false;
    }

    public override void OnEnable()
    {
        LoadingScreen.IntroComplete += IntroComplete;
        base.OnEnable();
        PlayerUnit.unitCreated += UnitCreated;

        if (!SaveLoadManager.Loading)
        {
            OpenWindow();
            if (FindFirstObjectByType<LoadingScreen>() == null)
                IntroComplete();
            skipTutorial.gameObject.SetActive(CanSkipTutorial());
        }
        else
            CloseWindow();
    }

    public override void OnDisable()
    {
        closeButton.OnClicked.RemoveListener(CloseWindow);
        skipTutorial.RemoveAllListeners();
        LoadingScreen.IntroComplete -= IntroComplete;
        PlayerUnit.unitCreated -= UnitCreated;
        base.OnDisable();
    }

    private void UnitCreated(Unit unit)
    {
        if(!tutorialSkipped)
        {
            return;
        }

        if (unit is PlayerUnit playerUnit && playerUnit.unitType == PlayerUnitType.hq)
        {
            AfterHQTriggers();

            BuildMenu bm = FindObjectOfType<BuildMenu>();
            foreach (var unitType in unitsToUnlock)
            {
                bm.UnLockUnit(unitType);
            }

            HexTechTree.ChangeTechCredits(0); //forces UI refresh?

            PlayerUnit.unitCreated -= UnitCreated;
        }
    }

    private void IntroComplete()
    {
        closeButton.OnClicked.AddListener(CloseWindow);
        skipTutorial.Clicked += CheckSkipTutorial;
    }

    public override void OpenWindow()
    {
        tutorialSkipped = false;
        messageBlock.Text = GetMessage();
        base.OpenWindow();
    }

    private string GetMessage()
    {
        if (gameSettings.IsDemo)
            return demoMessage;
        else if (gameSettings.IsEarlyAccess)
            return earlyAccessMessage;
        else
            return string.Empty;
    }

    public override void CloseWindow()
    {
        gameStarted = true;
        GameStarted?.Invoke();
        base.CloseWindow();
        GameStarted = null; //reset for possible scene reload
    }

    private void CheckSkipTutorial()
    {
        if (skippedTutorialBefore)
        {
            SkipTutorial();
            return;
        }
        else
        {
            messageParent.gameObject.SetActive(false);
            skipTutorialMessage.gameObject.SetActive(true);
            PlayerPrefs.SetInt(TUTORIAL_SKIPPED_BEFORE, 1);
            skippedTutorialBefore = true;
            letsPlayButton.Clicked += CloseWindow;
            letsPlayButton.Clicked += SkipTutorial;
        }
    }

    [Button]
    private void SkipTutorial()
    {
        tutorialSkipped = true;
        TutorialSkipped?.Invoke();
        foreach (var trigger in noTutorialTriggers)
        {
            trigger.DoTrigger();
        }
        CloseWindow();
    }

    private void AfterHQTriggers()
    {
        foreach (var trigger in afterHQTriggers)
        {
            trigger.DoTrigger();
        }
    }

    private bool CanSkipTutorial()
    {
        if(!gameSettings.IsDemo)
            return true;
        else if (PlayerPrefs.GetInt("TUTORIAL_COMPLETE", 0) == 1)
            return true;
        else
            return SteamStatsAndAchievements.IsAchievementUnlocked("TUTORIAL_COMPLETE");
    }

    private const string TUTORIAL_SKIP_STRING = "TutorialSkip";

    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this, -1);
    }

    public void Save(string savePath, ES3Writer writer)
    {
        writer.Write<bool>(TUTORIAL_SKIP_STRING, tutorialSkipped);
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        yield return null;
        if (ES3.KeyExists(TUTORIAL_SKIP_STRING, loadPath))
            tutorialSkipped = ES3.Load<bool>(TUTORIAL_SKIP_STRING, loadPath, false);


        if (tutorialSkipped)
            SkipTutorial();

        yield return null;
    }
}
