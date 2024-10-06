using HexGame.Units;
using Newtonsoft.Json.Bson;
using Nova;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class StateOfTheGame : WindowPopup
{
    public static event Action GameStarted;
    public static bool gameStarted = false;
    public Button closeButton;

    public GameSettings gameSettings;
    [SerializeField] private TextBlock messageBlock;
    [SerializeField, TextArea(3, 10)] private string demoMessage;
    [SerializeField, TextArea(3, 10)] private string earlyAccessMessage;

    [SerializeField] private List<TriggerBase> noTutorialTriggers = new();
    [SerializeField] private List<PlayerUnitType> unitsToUnlock = new();
    public static event Action TutorialSkipped;

    private void Awake()
    {
        gameStarted = false;
    }

    public override void OnEnable()
    {
        LoadingScreen.IntroComplete += IntroComplete;
        base.OnEnable();
        OpenWindow();
        if(FindFirstObjectByType<LoadingScreen>() == null)
            IntroComplete();
    }

    public override void OnDisable()
    {
        closeButton.OnClicked.RemoveListener(CloseWindow);
        LoadingScreen.IntroComplete -= IntroComplete;
        base.OnDisable();
    }

    private void IntroComplete()
    {
        closeButton.OnClicked.AddListener(CloseWindow);
    }

    public override void OpenWindow()
    {
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
    }

    [Button]
    private void SkipTutorial()
    {
        TutorialSkipped?.Invoke();
        foreach (var trigger in noTutorialTriggers)
        {
            trigger.DoTrigger();
        }

        BuildMenu bm = FindObjectOfType<BuildMenu>();
        foreach (var unitType in unitsToUnlock)
        {
            bm.UnLockUnit(unitType);
        }
    }
}
