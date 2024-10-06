using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class UpdateGraphTest : MonoBehaviour
{
    [SerializeField]
    private Collider collider;

    [Button]
    private void UpdateGraph()
    {
        Bounds bound = collider.bounds;
        var gou = new Pathfinding.GraphUpdateObject(bound);

        AstarPath.active.UpdateGraphs(gou);
    }
}
