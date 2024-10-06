using Nova;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using Sirenix.Utilities;
using NovaSamples.UIControls;
using DG.Tweening;
using System.Collections.ObjectModel;

[RequireComponent(typeof(ClipMask))]
public class DirectiveMenu : MonoBehaviour
{
    [SerializeField] private Button openButton;
    private Animator openButtonAnimator;
    private UIBlock2D openButtonBlock;
    private ClipMask clipMask;
    [SerializeField] private ClipMask directiveClipMask;
    [SerializeField] private List<DirectiveBase> directiveList = new List<DirectiveBase>();
    private bool directiveAdded = false;
    [SerializeField] private List<DirectiveQuest> questList = new List<DirectiveQuest>();
    [SerializeField] private ListView directiveDisplay;
    private DirectiveQuest assignedQuest;

    [SerializeField] private ListView questDisplay;
    public static event Action<float> directiveTimer;
    private WaitForSeconds delay = new WaitForSeconds(1f);

    public static event Action<DirectiveQuest> questAdded;
    private int maxQuests = 2;
    public int MaxQuests => maxQuests;

    private Dictionary<DirectiveQuest, QuestTimerInfo> timerInfo = new Dictionary<DirectiveQuest, QuestTimerInfo>();

    private void Awake()
    {
        clipMask = GetComponent<ClipMask>();
        openButtonBlock = openButton.GetComponent<UIBlock2D>();
        openButtonAnimator = openButton.GetComponent<Animator>();
    }

    private void OnEnable()
    {
        LockDirectiveButton.lockDirectiveButton += ButtonOff;
        UnlockStockMarketButton.unlockStockMarketButton += ButtonOn;
        DirectiveQuest.questCompleted += QuestComplete;

        directiveDisplay.AddDataBinder<string, DirectiveGoalVisuals>(DisplayDirectives);
        questDisplay.AddDataBinder<DirectiveQuest, DirectiveVisuals>(DisplayQuestDirectives);

        directiveDisplay.SetDataSource(directiveList);

        StartCoroutine(Timer());
    }


    private void OnDisable()
    {
        LockDirectiveButton.lockDirectiveButton -= ButtonOff;
        UnlockStockMarketButton.unlockStockMarketButton -= ButtonOn;
        foreach (var directive in directiveList)
        {
            directive.directiveUpdated -= DirectiveUpdated;
        }
        foreach (var quest in questList)
        {
            quest.directiveUpdated -= DirectiveUpdated;
        }

        StopAllCoroutines();
        DOTween.Kill(this.gameObject);
        DOTween.Kill(clipMask);
    }

    private IEnumerator Timer()
    {
        while (true)
        {
            yield return delay;

            for(int i = timerInfo.Count - 1; i >= 0; i--)
            {
                var timer = timerInfo.ElementAt(i);
                timer.Value.timeRemaining -= 1f;
                timer.Value.visuals.SetTime(timer.Value.timeRemaining, timer.Value.timeRemaining / timer.Value.quest.TimeLimitSeconds);
                if (timer.Value.timeRemaining <= 0f)
                    timer.Value.quest.Failed();
            }
        }
    }

    private void DisplayQuestDirectives(Data.OnBind<DirectiveQuest> evt, DirectiveVisuals target, int index)
    {
        //last time used gets set when initialized
        //so did that happen in the last frame?
        if (Time.realtimeSinceStartup <= evt.UserData.LastUsedTime + Time.deltaTime)
        {
            float width = target.container.Size.Value.x;
            Vector3 position = target.container.Position.Value - new Vector3(width, 0, 0);
            target.container.TrySetLocalPosition(position);
            target.container.DoPositionX(0, 0.25f).SetEase(Ease.InOutCirc);
            target.clipMask.SetAlpha(0f);
            target.clipMask.DoFade(1f, 0.25f).SetEase(Ease.InOutCirc);
        }

        target.Initialize();
        target.UpdateDirective(evt.UserData);
        target.ToggleTimer(evt.UserData.UseTimeLimit);
        target.cancelButton.gameObject.SetActive(!evt.UserData.isCorporate);

        if(evt.UserData.UseTimeLimit)
            StartQuestTimer(evt.UserData, target);
    }

    private void StartQuestTimer(DirectiveQuest quest, DirectiveVisuals visuals)
    {
        if (timerInfo.TryGetValue(quest, out QuestTimerInfo qti))
        {
            qti.visuals = visuals;
            qti.visuals.SetTime(qti.timeRemaining, qti.timeRemaining / qti.quest.TimeLimitSeconds);
        }
        else
        {
            QuestTimerInfo questTimerInfo = new QuestTimerInfo();
            questTimerInfo.quest = quest;
            questTimerInfo.visuals = visuals;
            questTimerInfo.timeRemaining = quest.TimeLimitSeconds;
            timerInfo.Add(quest, questTimerInfo);
        }
    }

    private void DisplayDirectives(Data.OnBind<string> evt, DirectiveGoalVisuals target, int index)
    {
        if(index == directiveList.Count - 1 && directiveAdded)
        {
            float width = target.container.Size.Value.x;
            Vector3 position = target.container.Position.Value - new Vector3(width, 0, 0);
            target.container.TrySetLocalPosition(position);
            target.container.DoPositionX(0, 0.25f).SetEase(Ease.InOutCirc);
            target.clipMask.SetAlpha(0f);
            target.clipMask.DoFade(1f, 0.25f).SetEase(Ease.InOutCirc);
        }

        target.directiveText.Text = evt.UserData;
    }

    /// <summary>
    /// Adds a quest to the list of directives. Use force add for assigned quotas.
    /// </summary>
    /// <param name="quest"></param>
    /// <param name="forceAdd"></param>
    public bool TryAddQuest(DirectiveQuest quest, bool forceAdd = false)
    {
        if (quest == null)
            return false;

        if (forceAdd && this.assignedQuest != null)
            return false; //we already have an assigned quest

        int assignedCount = this.assignedQuest != null ? 1 : 0;

        int nonCorpQuests = questList.Count(q => !q.isCorporate);

        if (!forceAdd && nonCorpQuests >= maxQuests + assignedCount)
        {
            MessagePanel.ShowMessage($"Maximum number of Directives is {maxQuests}", null);
            //ButtonOff();
            return false;
        }

        quest.Initialize();
        quest.directiveUpdated += DirectiveUpdated;

        if(forceAdd)
        {
            this.assignedQuest = quest;
            questList.Insert(0, quest);
        }
        else
            questList.Add(quest);

        //have we added our last quest?
        //if (questList.Count >= maxQuests)
        //    ButtonOff();

        questDisplay.SetDataSource(questList);
        questAdded?.Invoke(quest);
        SFXManager.PlaySFX(SFXType.newDirective);
        return true;
    }

    public void AddDirective(DirectiveBase directive)
    {
        if (directive == null)
            return;

        directive.Initialize();
        directive.directiveUpdated += DirectiveUpdated;
        directiveList.Add(directive);
        directiveAdded = true;

        directiveDisplay.SetDataSource(GetDirectiveStrings(directiveList));
        directiveAdded = false;
        DOTween.Kill(directiveClipMask);
        //Sequence blinkDirectives = DOTween.Sequence();
        //blinkDirectives.Append(directiveClipMask.DoFade(0.25f, 1f).SetEase(Ease.InOutCirc));
        //blinkDirectives.Append(directiveClipMask.DoFade(1f, 1f).SetEase(Ease.InOutCirc));
        //blinkDirectives.SetLoops(5, LoopType.Yoyo);
        directiveClipMask.DoFade(0.1f, 1f)
                         .SetEase(Ease.Linear)
                         .SetLoops(10, LoopType.Yoyo)
                         .OnComplete(() => directiveClipMask.SetAlpha(1f));

    }

    public void RemoveQuest(DirectiveQuest quest)
    {
        questList.Remove(quest);
        quest.directiveUpdated -= DirectiveUpdated;
        questDisplay.SetDataSource(questList);

        if(quest.UseTimeLimit)
            timerInfo.Remove(quest);
        
        //if (questList.Count < maxQuests)
        //    ButtonOn();
    }

    public void RemoveDirective(DirectiveBase directive)
    {
        directiveList.Remove(directive);
        directive.directiveUpdated -= DirectiveUpdated;
        directiveDisplay.SetDataSource(GetDirectiveStrings(directiveList));
    }

    private List<string> GetDirectiveStrings(List<DirectiveBase> directiveList)
    {
        List<string> directiveStrings = new List<string>();
        foreach (var directive in directiveList)
        {
            List<bool> completeList = directive.IsComplete();
            List<string> displayTextList = directive.DisplayText();

            for (int i = 0; i < completeList.Count; i++)
            {
                //if (completeList[i] == false)
                    directiveStrings.Add(displayTextList[i]);
                //else
                    //directiveStrings.Add($"<s>{displayTextList[i]}</s>");
            }
        }

        return directiveStrings;
    }

    private void DirectiveUpdated(DirectiveBase directive)
    {
        //we are returning a list of bools, one for each requirement
        bool playComplete = false;
        if(directive.IsComplete().All(d => d == true))
        {
            directive.OnComplete();
            directive.InvokeOnComplete();

            if(directive is DirectiveQuest quest)
                questList.Remove(quest);
            //quests can be in directives so check to remove there too
            directiveList.Remove(directive);
            playComplete = true;
            directive.directiveUpdated -= DirectiveUpdated;
        }
        directiveDisplay.SetDataSource(GetDirectiveStrings(directiveList));
        questDisplay.SetDataSource(questList);

        if (playComplete)
            SFXManager.PlaySFX(SFXType.DirectiveComplete);
        else
            SFXManager.PlaySFX(SFXType.DirectiveUpdated);
    }

    private void QuestComplete(DirectiveQuest completedQuest)
    {
        if(this.assignedQuest == completedQuest)
            this.assignedQuest = null;

        if (completedQuest.UseTimeLimit)
            timerInfo.Remove(completedQuest);
    }

    private void ButtonOff()
    {
        openButton.GetComponent<Interactable>().ClickBehavior = ClickBehavior.None;
        openButtonBlock.Color = ColorManager.GetColor(ColorCode.buttonGreyOut);
        openButtonAnimator.SetTrigger("ButtonOff");
    }

    private void ButtonOn()
    {
        Interactable interactable = openButton.GetComponent<Interactable>();
        if (interactable.ClickBehavior == ClickBehavior.OnRelease)
            return; //we're already on

        openButton.GetComponent<Interactable>().ClickBehavior = ClickBehavior.OnRelease;
        openButtonAnimator.SetTrigger("Highlight");
    }

    public ReadOnlyCollection<DirectiveQuest> GetQuestList()
    {
        return questList.AsReadOnly();
    }

    public bool CanAddQuest()
    {
        int nonCorpQuests = questList.Count(q => !q.isCorporate);
        return nonCorpQuests < maxQuests;
    }

    public class QuestTimerInfo
    {
        public DirectiveQuest quest;
        public DirectiveVisuals visuals;
        public float timeRemaining; 
    }

}
