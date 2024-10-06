using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[ManageableData]
[CreateAssetMenu(menuName = "Hex/Upgrades/Global Upgrade")]
public class GlobalUpgrade : UpgradeBase
{
    public Stat statType;
    public float statValue;
    public bool isPercent = false;

}
