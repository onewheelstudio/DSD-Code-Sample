using HexGame.Resources;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PriceChangeTrigger", menuName = "Hex/Triggers/Price Change Trigger")]
public class PriceChangeTrigger : TriggerBase
{
    [SerializeField] private ResourceType resource;
    [SerializeField] private float percentageChange;
    [InfoBox("Duration is in full days after the trigger is invoked.")]
    [SerializeField] private bool isPermanent;
    [SerializeField, HideIf("isPermanent")] private int duration;

    public static event Action<ResourceType, float, int> OnPriceChange;

    public override void DoTrigger()
    {
        if (isPermanent)
            OnPriceChange?.Invoke(resource, percentageChange, int.MaxValue);
        else
            OnPriceChange?.Invoke(resource, percentageChange, duration);
    }
}
