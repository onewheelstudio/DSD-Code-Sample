using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatsInfo", menuName = "Hex/Stats/StatsInfo")]
public class StatsInfo : SerializedScriptableObject
{
    [SerializeField]
    private Dictionary<Stat, StatInfo> statInfo = new Dictionary<Stat, StatInfo>();

    public StatInfo GetStatInfo(Stat stat)
    {
        if (statInfo.TryGetValue(stat, out StatInfo info))
        {
            return info;
        }
        else
        {
            Debug.LogError("Stat " + stat + " not found in " + this.name);
            return null;
        }
    }

    [Button]
    private void AddAllStats()
    {
        foreach (Stat stat in System.Enum.GetValues(typeof(Stat)))
        {
            if(statInfo.ContainsKey(stat) == false)
            {
                statInfo.Add(stat, new StatInfo(stat));
            }
        }
    }

    public class StatInfo
    {
        public Stat stat;
        [PreviewField(100)]
        public Sprite icon;
        public string description;

        public StatInfo(Stat stat)
        {
            this.stat = stat;
        }
    }
}
