using DG.Tweening;
using NovaSamples.UIControls;
using UnityEngine;

[RequireComponent(typeof(Button))]
public class QuitButton : MonoBehaviour
{
    [SerializeField] private bool showFeedbackWindow = false;
    [SerializeField] private PlayTestFeedBackWindow feedbackWindow;
    private GameSettingsManager gameSettingsManager;

    private void OnEnable()
    {
        gameSettingsManager = FindFirstObjectByType<GameSettingsManager>();
        this.GetComponent<Button>().clicked += Quit;
        if(showFeedbackWindow)
            feedbackWindow = FindObjectOfType<PlayTestFeedBackWindow>(true);
    }

    private void Quit()
    {
        DOTween.KillAll();
        Application.Quit();
    }
}
