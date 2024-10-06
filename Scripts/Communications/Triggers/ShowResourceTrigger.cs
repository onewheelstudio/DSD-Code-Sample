using HexGame.Resources;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ShowResourceTrigger", menuName = "Hex/Triggers/ShowResourceTrigger")]
public class ShowResourceTrigger : TriggerBase
{
    [SerializeField] private ResourceType resourceType;
    public static event Action<ResourceType, bool> resourceStarToggled;
    [SerializeField] private bool showOnTrigger = true;
    public override void DoTrigger()
    {
        resourceStarToggled?.Invoke(resourceType, showOnTrigger);
    }
}
