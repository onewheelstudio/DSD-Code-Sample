using HexGame.Units;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Directives/Destroy Enemy Directive")]
public class DestroyEnemyDirective : DirectiveBase
{
    [SerializeField] private EnemyUnitType enemyType;
    [SerializeField] private int totalToDestroy;
    private int amountDestroyed;

    public override void Initialize()
    {
        EnemyUnit.enemyUnitKilled += EnemyUnitKilled;
        amountDestroyed = 0;
        if(OnStartCommunication != null)
            CommunicationMenu.AddCommunication(OnStartCommunication);
    }
    public override void OnComplete()
    {
        EnemyUnit.enemyUnitKilled -= EnemyUnitKilled;
        if(OnCompleteCommunication != null)
            CommunicationMenu.AddCommunication(OnCompleteCommunication);
    }

    private void EnemyUnitKilled(EnemyUnit unit)
    {
        if(unit.type != enemyType)
            return;

        amountDestroyed++;
    }

    public override List<string> DisplayText()
    {
        return new List<string>() { $"Destroy {enemyType.ToNiceString()}: {amountDestroyed}/{totalToDestroy}" };
    }

    public override List<bool> IsComplete()
    {
        return new List<bool>() { amountDestroyed >= totalToDestroy };
    }
}
