using DG.Tweening;
using NovaSamples.UIControls;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Button))]
public class QuitButton : MonoBehaviour
{
    [SerializeField] private bool showFeedbackWindow = false;
    private FeedBackWindow feedbackWindow;
    public static bool IsQuitting => isQuitting;
    private static bool isQuitting = false;

    private void OnEnable()
    {
        if(showFeedbackWindow)
        {
            feedbackWindow = FindFirstObjectByType<FeedBackWindow>(FindObjectsInactive.Include);
            this.GetComponent<Button>().Clicked += feedbackWindow.OpenFeedbackOnClose;
            this.GetComponent<Button>().Clicked += AutoSaveOnQuit;

            EndScreenPanel endScreenPanel = FindFirstObjectByType<EndScreenPanel>();
            if(endScreenPanel != null)
                this.GetComponent<Button>().Clicked += endScreenPanel.OpenWindow;
        }
        else
            this.GetComponent<Button>().Clicked += Quit;

    }

    private void Quit()
    {
        DOTween.KillAll();
        AutoSaveOnQuit();

        StartCoroutine(DelayedQuit());
    }

    IEnumerator DelayedQuit()
    {
        while(SaveLoadManager.Saving)
            yield return null;

        PlayerAnalytics pa = FindFirstObjectByType<PlayerAnalytics>();
        if (pa != null)
            pa.SubmitAnalytics();
        else
            Application.Quit();
    }

    private void AutoSaveOnQuit()
    {
        SaveLoadManager saveLoadManager = FindFirstObjectByType<SaveLoadManager>();
        if (saveLoadManager != null)
            saveLoadManager.AutoSave();
    }
}
