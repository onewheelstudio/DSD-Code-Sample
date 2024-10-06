using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class CommunicationBase : ScriptableObject
{
    [Header("Before Actions")]
    public TriggerBase beforeTrigger;

    [SerializeField]private AudioClip audioClip;
    public AudioClip AudioClip => audioClip;
    [TextArea(5,6),SerializeField]private string text;
    public string Text => text;
    [HideInInspector, NonSerialized]
    public bool hasPlayed = false;
    public bool canPlayMoreThanOnce = false;
    [SerializeField] protected List<DirectiveBase> directivesToUnlock = new List<DirectiveBase>();

    [Header("Immediate Actions")]
    public CommunicationBase nextCommunication;
    public TriggerBase trigger;

    [Header("Notes")]
    [SerializeField, TextArea(3, 10)] protected string notes;

    [SerializeField] private Texture2D avatarImage;
    public Texture2D AvatarImage => avatarImage;

    public abstract void Initiallize();
    public virtual void Complete()
    {
        if (this.hasPlayed)
            return;

        this.hasPlayed = true;
        DirectiveMenu dm = FindObjectOfType<DirectiveMenu>();

        foreach (var directive in directivesToUnlock)
        {
            if(directive is DirectiveQuest)
                dm.TryAddQuest(directive as DirectiveQuest);
            else
                dm.AddDirective(directive);
        }
    }

    public List<DirectiveBase> GetDirectives()
    {
        return directivesToUnlock;
    }

    [Button]
    private void AddCommunication()
    {
        CommunicationMenu.AddCommunication(this);
    }
}
