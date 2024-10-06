using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Hex/GameSettings")]
public class GameSettings : ScriptableObject
{
    [SerializeField, OnValueChanged("@ToggleEvents()")] private bool isDemo = false;
    public bool IsDemo { get => isDemo; }
    [SerializeField] private int maxTierForDemo = 2;
    public int MaxTierForDemo { get => maxTierForDemo; }
    public static event System.Action<bool> demoToggled;

    [SerializeField, OnValueChanged("@ToggleEvents()")] private bool isEarlyAccess = false;
    public bool IsEarlyAccess { get => isEarlyAccess; }
    [SerializeField] private int maxTierForEarlyAccess = 10;
    public int MaxTierForEarlyAccess { get => maxTierForEarlyAccess; }
    public static event System.Action<bool> earlyAccessToggled;

    public void ToggleEvents()
    {
        demoToggled?.Invoke(isDemo);
        earlyAccessToggled?.Invoke(isEarlyAccess);
    }
}
