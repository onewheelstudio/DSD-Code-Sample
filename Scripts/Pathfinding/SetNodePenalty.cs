using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Sirenix.OdinInspector;

public class SetNodePenalty : MonoBehaviour
{
    [SerializeField, Min(0)] private uint penalty;
    [SerializeField] private bool walkable = true;

    private void OnEnable()
    {
        StartCoroutine(DelayPenalty());
    }

    private IEnumerator DelayPenalty()
    {
        yield return null;
        SetPenalty();
    }

    [Button]
    private void SetPenalty()
    {
        AstarPath.active.AddWorkItem(new AstarWorkItem(() => {
            // Safe to update graphs here
            var node = AstarPath.active.GetNearest(transform.position).node;

            node.Walkable = walkable;
            node.Penalty = penalty;
        }));
    }
}
