using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class CommunicationBase : ScriptableObject
{
    protected string guid;
    public string GUID => guid ??= Guid.NewGuid().ToString();
    [Header("Before Actions")]
    public TriggerBase beforeTrigger;

    [SerializeField]private AudioClip audioClip;
    public AudioClip AudioClip => audioClip;
    [TextArea(5,15),SerializeField]private string text;
    public string Text => text;
    [HideInInspector, NonSerialized]
    public bool hasPlayed = false;
    public bool canPlayMoreThanOnce = false;
    [Tooltip("Communication will not be blocked by time of day.")]
    public bool forcePlay = false;
    [SerializeField] protected List<DirectiveBase> directivesToUnlock = new List<DirectiveBase>();
    [NonSerialized] protected List<DirectiveBase> tempDirectivesToUnlock = new List<DirectiveBase>();

    [Header("Immediate Actions")]
    public CommunicationBase nextCommunication;
    public TriggerBase trigger;

    [Header("Notes")]
    [SerializeField, TextArea(3, 10)] protected string notes;

    [SerializeField] private Texture2D avatarImage;
    public Texture2D AvatarImage => avatarImage;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(guid))
            guid = Guid.NewGuid().ToString();
    }

    public abstract void Initiallize();
    public virtual void Complete()
    {
        if (this.hasPlayed)
            return;

        this.hasPlayed = true;
        DirectiveMenu dm = FindObjectOfType<DirectiveMenu>();

        if (tempDirectivesToUnlock.Count == 0)
            tempDirectivesToUnlock = GetCopyOfDirectives();

        foreach (var directive in tempDirectivesToUnlock)
        {
            if(directive is DirectiveQuest)
                dm.TryAddQuest(directive as DirectiveQuest);
            else
                dm.AddDirective(directive);
        }
    }

    public List<DirectiveBase> GetDirectives()
    {
        if (tempDirectivesToUnlock.Count == 0)
            tempDirectivesToUnlock = GetCopyOfDirectives();

        return tempDirectivesToUnlock;
    }

    [Button]
    private void AddCommunication()
    {
        CommunicationMenu.AddCommunication(this);
    }

    public void SetText(string text)
    {
        this.text = text;
    }

    public void SetAvatar(Texture2D avatar)
    {
        this.avatarImage = avatar;
    }

    protected List<DirectiveBase> GetCopyOfDirectives()
    {
        List<DirectiveBase> newDirectives = new List<DirectiveBase>();
        for (int i = 0; i < directivesToUnlock.Count; i++)
        {
            if (directivesToUnlock[i] == null)
                continue;

            newDirectives.Add(Instantiate(directivesToUnlock[i]));
        }

        return newDirectives;
    }
}
