using Sirenix.OdinInspector;
using UnityEngine;

public abstract class TriggerBase : ScriptableObject
{
    [Button]
    public abstract void DoTrigger();
}
