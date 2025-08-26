using HexGame.Resources;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Special Project Directive", menuName = "Hex/Directives/SpecialProjectDirective")]
public class SpecialProjectDirective : DirectiveQuest
{
    private SpecialProjectBehavior specialProjectBehavior;
    [SerializeField] private SpecialProjectProduction project;
    [NonSerialized] private bool isComplete = false;
    [NonSerialized] private float progress = 0f;
    public override void Initialize()
    {
        base.Initialize();
        isComplete = false;
        progress = 0f;
        SpecialProjectBehavior.ProjectComplete += ProjectComplete;
        SpecialProjectBehavior.ProjectUpdated += ProjectUpdated;
        CommunicationMenu.AddCommunication(OnStartCommunication);
    }

    private void ProjectUpdated(SpecialProjectProduction production, float progress)
    {
        if(production == project)
        {
            this.progress = Mathf.FloorToInt(progress * 100);
            DirectiveUpdated();
        }
    }

    private void ProjectComplete(SpecialProjectProduction production)
    {
        if(production == project)
        {
            progress = 100;
            isComplete = true;
            DirectiveUpdated();
        }
    }

    public override List<string> DisplayText()
    {
        return new List<string> { $"Build {project.niceName} : {progress}%"};
    }

    public override List<bool> IsComplete()
    {
        return new List<bool> { isComplete };
    }

    public override void OnComplete()
    {
        SpecialProjectBehavior.ProjectComplete -= ProjectComplete;
        CommunicationMenu.AddCommunication(OnCompleteCommunication);
        OnCompleteTrigger.ForEach(t => t.DoTrigger());
    }
}
