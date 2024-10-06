using Nova;
using NovaSamples.UIControls;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class GameTipsWindow : WindowPopup
{
    public ListView tipList;
    public List<CommunicationBase> tips = new List<CommunicationBase>();
    private static GameTipsWindow instance;
    private static bool showGameTips = true;

    private new void OnEnable()
    {
        base.OnEnable();
        tipList.AddDataBinder<TipCommunication, ButtonVisuals>(DisplayTips);
        GameSettingsWindow.showGameTips += ShowGameTips;
    }

    private new void OnDisable()
    {
        GameSettingsWindow.showGameTips -= ShowGameTips;
        base.OnDisable();
    }

    private void ShowGameTips(bool showGameTips)
    {
        GameTipsWindow.showGameTips = showGameTips;
    }

    private void DisplayTips(Data.OnBind<TipCommunication> evt, ButtonVisuals target, int index)
    {
        target.Label.Text = evt.UserData.tipHint;
        target.Background.GetComponent<Button>().OnClicked.RemoveAllListeners();
        target.Background.GetComponent<Button>().OnClicked.AddListener(() => ReadTip(evt.UserData, target));
    }

    public static void AddTip(TipCommunication tip)
    {
        if (tip == null || !showGameTips)
            return;

        if(instance == null)
            instance = FindObjectOfType<GameTipsWindow>();

        instance.AddTip(tip);
    }

    private void AddTip(CommunicationBase tip)
    {
        tips.Add(tip);
        tipList.SetDataSource(tips);
    }

    private void ReadTip(TipCommunication tip, ButtonVisuals visuals)
    {
        //clear tip if right clicked
        if(Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.rightButton.wasReleasedThisFrame)
        {
            RemoveTip(tip);
            return;
        }

        visuals.Label.Text = "Waiting to Play";
        CommunicationMenu.AddCommunication(tip, false, () => RemoveTip(tip));
    }

    private void RemoveTip(TipCommunication tip)
    {
        tips.Remove(tip);
        tipList.SetDataSource(tips);
    }
}
