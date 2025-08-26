using HexGame.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavTargetUnit : Unit
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
