using System.Collections.Generic;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    [SerializeField] private StatsInfo statsInfo;
    [SerializeField] private List<Stats> statsList;
    private const string statsPath = "Assets/Prefabs/Units/Stats/";

    public StatsInfo.StatInfo GetStatInfo(Stat stat)
    {
        return statsInfo.GetStatInfo(stat);
    }

    public Sprite GetStatIcon(Stat stat)
    {
        return statsInfo.GetStatInfo(stat).icon;
    }

}


