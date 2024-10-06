using HexGame.Units;
using UnityEngine;

[System.Serializable]
public class EnemyRequirement
{
    public EnemyUnitType enemyType;
    public int requiredAmount;
    [HideInInspector]
    public int currentAmount;

    public EnemyRequirement(EnemyUnitType type, int amount)
    {
        this.enemyType = type;
        this.requiredAmount = amount;
        this.currentAmount = 0;
    }
}
