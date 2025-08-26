using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Resources;
using HexGame.Grid;
using HexGame.Units;
using System;

[CreateAssetMenu(menuName = "Hex/Conditions/Near Unit Stat Boost")]
public class NearUnitStatBoost : StatBoost
{
    public PlayerUnitType unitType;
    [SerializeField, Range(1, 3)]
    protected int range = 1;
    [SerializeField] private bool allowMultipleBoosts = false;
    [NonSerialized] private List<Hex3> neighbors = new List<Hex3>();

    public override int Boost(GameObject unit)
    {
        if (range == 0)
            return 0;

        int totalBoost = 0;

        neighbors.Clear();
        HexTileManager.GetHex3WithInRange(unit.transform.position, 1, range, ref neighbors);
        foreach (var hex3 in neighbors)
        {
            if (UnitManager.TryGetPlayerUnitAtLocation(hex3, out PlayerUnit playerUnit) && playerUnit.unitType == this.unitType)
            {
                if (!allowMultipleBoosts)
                    return boost;

                totalBoost += boost;
            }
        }

        return totalBoost;
    }
}

public abstract class StatBoost : ScriptableObject
{
    [Range(-50, 50)]
    [SerializeField]
    protected int boost;
    public Stat stat;

    public abstract int Boost(GameObject unit);
}
