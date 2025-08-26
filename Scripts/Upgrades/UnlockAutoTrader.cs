using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Upgrades/DoTriggerUpgrade")]
public class UnlockAutoTrader : Upgrade
{
    public static event Action OnUnlockAutoTrader;
    [SerializeField] private TriggerBase trigger;
    [SerializeField] private Texture2D icon;
    [SerializeField] private CommunicationBase communication;
    public Texture2D Icon => icon;

    public override void DoUpgrade()
    {
        trigger?.DoTrigger();
        if(!SaveLoadManager.Loading)
            CommunicationMenu.AddCommunication(communication);
        OnUnlockAutoTrader?.Invoke();
    }
}
