using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class DirectiveBase : ScriptableObject, ISelfValidator
{
    protected string guid;
    public string GUID => guid ??= Guid.NewGuid().ToString();
    public event Action<DirectiveBase> directiveUpdated;
    public event Action<DirectiveBase> directiveCompleted;
    [SerializeField] protected CommunicationBase OnStartCommunication;
    [SerializeField] protected CommunicationBase OnCompleteCommunication;
    public List<TriggerBase> OnCompleteTrigger;

    [Header("Notes")]
    [SerializeField, TextArea(3, 10)] protected string notes;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(guid))
            guid = Guid.NewGuid().ToString();
    }

    public virtual void Initialize()
    {
        directiveUpdated = null;
        directiveCompleted = null;
    }

    public abstract void OnComplete();
    public abstract List<bool> IsComplete();
    public abstract List<string> DisplayText();

    public string DisplayTestToString()
    {
        string text = "";
        foreach (var _text in DisplayText())
        {
            text += _text;
        }

        return text;
    }

    protected void DirectiveUpdated()
    {
        directiveUpdated?.Invoke(this);
    }

    public virtual void Validate(SelfValidationResult result)
    {
        if(OnCompleteTrigger != null && OnCompleteTrigger.Count > 0)
        {
            foreach (var trigger in OnCompleteTrigger)
            {
                if (trigger == null)
                    result.AddError("Directive Trigger is null");
            }
        }

        if (OnCompleteTrigger.Count == 0 && OnStartCommunication == null && OnCompleteCommunication == null)
            result.AddWarning("This directive doesn't do anything when completed...");
        
    }

    [Button]
    private void AddDirective()
    {
        DirectiveMenu dm = FindObjectOfType<DirectiveMenu>();
        dm.AddDirective(this);
    }

    public void InvokeOnComplete()
    {
        directiveCompleted?.Invoke(this);
    }
}


