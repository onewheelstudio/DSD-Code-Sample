using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[ManageableData]
[CreateAssetMenu(menuName = "Hex/Upgrades/Leader Upgrades")]
public class LeaderUpgrades : SerializedScriptableObject
{
    [BoxGroup("Details")]
    [HorizontalGroup("Details/Info", MaxWidth = 100)]
    [SerializeField]
    [PreviewField(ObjectFieldAlignment.Left)]
    [HideLabel]
    [RequiredIn(PrefabKind.PrefabInstance)]
    public Sprite avatar { get; private set; }

    [ShowInInspector]
    [LabelWidth(125)]
    [VerticalGroup("Details/Info/Right")]
    public string leaderName;

    [ShowInInspector]
    [LabelWidth(125)]
    [VerticalGroup("Details/Info/Right")]
    public string backgroundStory { get; private set; }

    public List<StatsUpgrade> statUpgrades = new List<StatsUpgrade>();
    public List<GlobalUpgrade> globalUpgrades = new List<GlobalUpgrade>();
    public List<UnitUnlockUpgrade> unitUnlockUpgrades = new List<UnitUnlockUpgrade>();

    [Button]
    public void DoUpgrade()
    {
        foreach (var statUpgrade in statUpgrades)
        {
            statUpgrade.DoUpgrade();
        }

        foreach (var unitUnlock in unitUnlockUpgrades)
        {
            unitUnlock.DoUpgrade();
        }

        foreach (var globalUpgrade in globalUpgrades)
        {
            Stats.UnlockGlobalUpgrade(globalUpgrade);
        }
    }
}
