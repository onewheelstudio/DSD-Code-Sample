using HexGame.Units;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class EnemyGroup
{
    public List<EnemySubUnit> subUnits = new List<EnemySubUnit>();

    public void TryGetSubUnits(GameObject parentObject)
    {
        this.subUnits = parentObject.GetComponent<FollowParent>().GetSubUnits();
    }
}
