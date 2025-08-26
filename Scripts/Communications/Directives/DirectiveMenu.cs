using DG.Tweening;
using Nova;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(ClipMask))]
public class DirectiveMenu : MonoBehaviour, ISaveData
{
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

    public static event Action<DirectiveQuest> QuestAdded;
    public static event Action<DirectiveBase> DirectiveAdded;


    private SupplyShipManager supplyShipManager;
    public int MaxQuests
    {
        get
        {
            if(supplyShipManager == null)
                supplyShipManager = FindFirstObjectByType<SupplyShipManager>();

            return Mathf.Max(2, supplyShipManager.SupplyShipCount + 1);
        }
    }

    public bool LoadComplete => loadComplete;
    private bool loadComplete = false;

    private Dictionary<DirectiveQuest, QuestTimerInfo> timerInfo = new Dictionary<DirectiveQuest, QuestTimerInfo>();

    private void Awake()
    {
        clipMask = GetComponent<ClipMask>();
        RegisterDataSaving();
    }

    private void OnEnable()
    {
        DirectiveQuest.questCompleted += QuestComplete;

        directiveDisplay.AddDataBinder<string, DirectiveGoalVisuals>(DisplayDirectives);
        questDisplay.AddDataBinder<DirectiveQuest, DirectiveVisuals>(DisplayQuestDirectives);

        directiveDisplay.SetDataSource(directiveList);

        StartCoroutine(Timer());
    }


    private void OnDisable()
    {
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
                timer.Value.timeRemaining -= 1f * GameConstants.GameSpeed;
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
        target.ToggleTimer(evt.UserData.UseTimeLimit && !evt.UserData.IsFailed);
        target.cancelButton.gameObject.SetActive(!evt.UserData.isCorporate);

        if(evt.UserData.UseTimeLimit && !evt.UserData.IsFailed)
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
            target.container.DoPositionX(0, 0.25f).SetUpdate(true).SetEase(Ease.InOutCirc);
            target.clipMask.SetAlpha(0f);
            target.clipMask.DoFade(1f, 0.25f).SetUpdate(true).SetEase(Ease.InOutCirc);
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

        if(!quest.CanBeAssigned())
        {
            Debug.LogError($"{quest.name} quest cannot be assigned.");
            return false;
        }

        int assignedCount = this.assignedQuest != null ? 1 : 0;

        int nonCorpQuests = questList.Count(q => !q.isCorporate);

        if (!forceAdd && nonCorpQuests >= MaxQuests + assignedCount)
        {
            MessagePanel.ShowMessage($"Maximum number of Directives is {MaxQuests}", null);
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

        //questDisplay.SetDataSource(questList);
        questList = SortQuests(questList);
        questDisplay.SetDataSource(questList);
        QuestAdded?.Invoke(quest);
        SFXManager.PlaySFX(SFXType.newDirective);
        return true;
    }

    /// <summary>
    /// functions that sorts the quest list. Corporate quests first, autotrader next, then all others.
    /// </summary>
    /// <param name="questList"></param>
    private List<DirectiveQuest> SortQuests(List<DirectiveQuest> questList)
    {
        return questList.OrderByDescending(q => q.isCorporate)
                        .ThenByDescending(q => q.isAutoTrader)
                        //.ThenByDescending(q => !q.isCorporate && !q.isAutoTrader)
                        .ToList();
    }

    public void AddDirective(DirectiveBase directive)
    {
        if (directive == null)
            return;

        directive.Initialize();
        directive.directiveUpdated += DirectiveUpdated;
        directiveList.Add(directive);
        directiveAdded = true;
        DirectiveAdded?.Invoke(directive);

        directiveDisplay.SetDataSource(GetDirectiveStrings(directiveList));
        directiveAdded = false;
        DOTween.Kill(directiveClipMask);


        directiveClipMask.DoFade(0.1f, 1f)
                         .SetEase(Ease.Linear)
                         .SetLoops(10, LoopType.Yoyo)
                         .SetUpdate(true)
                         .OnComplete(() => directiveClipMask.SetAlpha(1f));

    }

    public void RemoveQuest(DirectiveQuest quest)
    {
        questList.Remove(quest);
        quest.directiveUpdated -= DirectiveUpdated;
        questDisplay.SetDataSource(questList);

        QuestTimeExpired(quest);
    }

    public void QuestTimeExpired(DirectiveQuest quest)
    {
        if (quest.UseTimeLimit)
            timerInfo.Remove(quest);
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
        if(directiveDisplay != null)
            directiveDisplay.SetDataSource(GetDirectiveStrings(directiveList));
        if(questDisplay != null)
            questDisplay.SetDataSource(questList);

        if (SaveLoadManager.Loading)
            return;

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

    public ReadOnlyCollection<DirectiveQuest> GetQuestList()
    {
        return questList.AsReadOnly();
    }

    public bool CanAddQuest()
    {
        int nonCorpQuests = questList.Count(q => !q.isCorporate);
        return nonCorpQuests < MaxQuests;
    }

    private const string DIRECTIVE_QUEST_DATA = "DirectiveQuestData";
    private const string DIRECTIVE_DATA = "DirectiveData";
    public void RegisterDataSaving()
    {
        //needs to happen after units are loaded
        SaveLoadManager.RegisterData(this, 1000);
    }

    public void Save(string savePath, ES3Writer writer)
    {
        writer.Write<List<DirectiveQuest>>(DIRECTIVE_QUEST_DATA, questList);
        writer.Write<List<DirectiveBase>>(DIRECTIVE_DATA, directiveList);
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if(ES3.KeyExists(DIRECTIVE_QUEST_DATA, loadPath))
        {
            var tempQuestList = ES3.Load<List<DirectiveQuest>>(DIRECTIVE_QUEST_DATA, loadPath, new List<DirectiveQuest>());
            tempQuestList = SortQuests(tempQuestList);

            //work backward through the list in case we remove any
            for (int i = tempQuestList.Count - 1; i >= 0; i--)
            {
                if(ListContainsDirective(questList, tempQuestList[i]))
                    continue; //we already have this directive in the list

                questList.Add(tempQuestList[i]);
                postUpdateMessage?.Invoke($"Sorting Directives {i + 1} of {tempQuestList.Count}");
                tempQuestList[i].Initialize();
                tempQuestList[i].directiveUpdated += DirectiveUpdated;
                QuestAdded?.Invoke(tempQuestList[i]);
                DirectiveUpdated(tempQuestList[i]);
            }
        }

        yield return null;

        if (ES3.KeyExists(DIRECTIVE_DATA, loadPath))
        {
            var tempDirectiveList = ES3.Load<List<DirectiveBase>>(DIRECTIVE_DATA, loadPath, new List<DirectiveBase>());

            //work backward through the list in case we remove any
            for (int i = tempDirectiveList.Count - 1; i >= 0; i--)
            {
                if(ListContainsDirective(directiveList, tempDirectiveList[i]))
                    continue; //we already have this directive in the list

                directiveList.Add(tempDirectiveList[i]);
                tempDirectiveList[i].Initialize();
                tempDirectiveList[i].directiveUpdated += DirectiveUpdated;
                DirectiveAdded?.Invoke(tempDirectiveList[i]);
                DirectiveUpdated(tempDirectiveList[i]);
            }
        }
        yield return null;
    }

    private bool ListContainsDirective(List<DirectiveBase> list, DirectiveBase directive)
    {
        if(list.Contains(directive))
            return true;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].DisplayTestToString() == directive.DisplayTestToString())
                return true;
        }

        return false;
    }
    
    private bool ListContainsDirective(List<DirectiveQuest> list, DirectiveQuest directive)
    {
        if(list.Contains(directive))
            return true;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].DisplayTestToString() == directive.DisplayTestToString())
                return true;
        }

        return false;
    }

    public void ProjectFailed()
    {
        foreach (var quest in questList)
        {
            if (quest is SpecialProjectDirective spd && !spd.IsComplete().All(c => c == true))
            {
                spd.Failed();
                break;
            }
        }
    }

    public class QuestTimerInfo
    {
        public DirectiveQuest quest;
        public DirectiveVisuals visuals;
        public float timeRemaining; 
    }

}
