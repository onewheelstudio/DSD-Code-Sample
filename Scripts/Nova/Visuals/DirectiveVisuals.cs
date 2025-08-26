using HexGame.Resources;
using Nova;
using NovaSamples.UIControls;
using System.Collections.Generic;
using UnityEngine;

public class DirectiveVisuals : ItemVisuals
{
    public UIBlock2D container;
    public ClipMask clipMask;
    [SerializeField] private ListView directiveList;
    [SerializeField] private UIBlock2D timerFillBar;
    [SerializeField] private TextBlock timerText;
    
    [Header("Rep Reward Visuals")]
    [SerializeField] private GameObject techCreditContainer;
    [SerializeField] private TextBlock techCreditText;
    [SerializeField] private GameObject repContainer;
    [SerializeField] private TextBlock repText;

    [Header("Resource Reward Visuals")]
    [SerializeField] private GameObject resourceContainer;
    [SerializeField] private TextBlock resourceText;
    [SerializeField] private UIBlock2D resourceIcon;
    private static PlayerResources ps;

    [Header("Other Bits")]
    public float startTime;
    public bool initialized = false;
    public float timeLimit;
    public TextBlock headerText;
    public Button markedLocationButton;

    [Header("Still other bits")]
    public Button cancelButton;
    private bool isCorporate = false;
    private bool isAutoTrader = false;
    [SerializeField] private Sprite corporateIcon;
    [SerializeField] private Sprite autoTraderIcon;
    [SerializeField] private Sprite regularIcon;

    public void Initialize()
    {
        if(!initialized)
            directiveList.AddDataBinder<string, DirectiveGoalVisuals>(DisplayDirectives);

        initialized = true;

        if(!ps)
            ps = GameObject.FindObjectOfType<PlayerResources>();
    }

    public void UpdateDirective(DirectiveQuest quest)
    {
        if(quest == null)
            return;

        isCorporate = quest.isCorporate;
        isAutoTrader = quest.isAutoTrader;
        headerText.Text = quest.headerText;
        markedLocationButton.RemoveClickListeners();
        markedLocationButton.gameObject.SetActive(!string.IsNullOrEmpty(quest.headerText));
        if (quest is DevelopResourceDirective drd)
            markedLocationButton.Clicked += drd.MoveToLocation;

        directiveList.SetDataSource(quest.DisplayText());
        SetTime(quest.TimeLimitSeconds, 1f);

        //turn things on and off based on the quest rewards
        techCreditContainer.SetActive(quest.useRepReward);
        repContainer.SetActive(quest.useRepReward);
        resourceContainer.SetActive(quest.useResourceReward);

        //fill in the reward values
        if(quest.TryGetRepReward(out QuestReward reward))
        {
            techCreditContainer.SetActive(reward.techCreditsReward > 0); 
            techCreditText.Text = $"+{reward.techCreditsReward}";
            repContainer.SetActive(reward.repReward > 0);
            repText.Text = $"+{reward.repReward}";
        }
        else if(quest.TrGetResourceRewards(out List<ResourceAmount> rewardResources))
        {
            resourceContainer.SetActive(rewardResources[0].amount > 0);
            resourceText.Text = $"+{rewardResources[0].amount}";

            resourceIcon.SetImage(ps.GetResourceTemplate(rewardResources[0].type).icon);
            resourceIcon.Color = ps.GetResourceTemplate(rewardResources[0].type).resourceColor;
        }

        cancelButton.RemoveClickListeners();
        cancelButton.Clicked += quest.Failed;
    }

    private void DisplayDirectives(Data.OnBind<string> evt, DirectiveGoalVisuals target, int index)
    {
        target.directiveText.Text = evt.UserData;
        if(isCorporate)
        {
            target.icon.SetImage(corporateIcon);
            target.icon.Color = ColorManager.GetColor(ColorCode.repuation);
        }
        else if(isAutoTrader)
        {
            target.icon.SetImage(autoTraderIcon);
            target.icon.Color = ColorManager.GetColor(ColorCode.techCredit);
        }
        else
        {
            target.icon.SetImage(regularIcon);
            target.icon.Color = Color.white;
        }
    }

    public void SetTime(float seconds, float percentRemaining)
    {
        timerText.Text = TimeToMinutes(seconds);
        timerFillBar.Size.Percent = new Vector2(percentRemaining, 1f);
    }

    private string TimeToMinutes(float seconds)
    {
        seconds = Mathf.RoundToInt(seconds);
        int minutes = Mathf.FloorToInt(seconds / 60);
        seconds = seconds - (minutes * 60);
        return $"{minutes}:{seconds.ToString("00")}";
    }

    public void ToggleTimer(bool isOn)
    {
        if(!isOn)//no timer
        {
            timerFillBar.Size.Percent = new Vector2(1f, 1f);
        }
        timerText.gameObject.SetActive(isOn);
    }
}
