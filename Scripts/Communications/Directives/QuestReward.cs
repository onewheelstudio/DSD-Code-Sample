using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class QuestReward
{
    [Min(0)]
    public int repReward = 100;
    [Min(0)]
    public int techCreditsReward = 1;

    public QuestReward()
    {
        this.repReward = 100;
        this.techCreditsReward = 1;
    }

    public QuestReward(int repReward, int techCreditsReward)
    {
        this.repReward = repReward;
        this.techCreditsReward = techCreditsReward;
    }
}
