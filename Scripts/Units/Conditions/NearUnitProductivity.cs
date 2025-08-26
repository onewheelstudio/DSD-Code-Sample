using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Conditions/Near Unit Productivity")]
public class NearUnitProductivity : ProductivityCondition
{
    [SerializeField] private PlayerUnitType unitType;
    [SerializeField, Range(1, 3)]
    private int range = 1;
    [Sirenix.OdinInspector.InfoBox("Should the boost/penalty add up?")]
    [SerializeField] private bool additive = false;
    public override float ProductivityMultiplier(ResourceProductionBehavior rpb)
    {
        if (range == 0)
            return 1f;

        float _boost = 1f;
        List<Hex3> neighbors = HexTileManager.GetHex3WithInRange(rpb.Position, 1, range);
        foreach (var hex3 in neighbors)
        {
            if (UnitManager.TryGetPlayerUnitAtLocation(hex3, out PlayerUnit playerUnit) && playerUnit.unitType == this.unitType)
            {
                if (!additive)
                    return boost;
                else
                    _boost *= this.boost;
            }
        }

        return _boost;
    }
}
