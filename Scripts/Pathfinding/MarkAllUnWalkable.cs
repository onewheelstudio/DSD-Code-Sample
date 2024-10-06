using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Pathfinding;

public class MarkAllUnWalkable : MonoBehaviour
{
    [SerializeField] private bool walkable = true;

    [Button]
    private void SetGraphWalkable()
    {
        AstarPath.active.AddWorkItem(new AstarWorkItem(ctx => {
            var gg = AstarPath.active.data.gridGraph;
            for (int z = 0; z < gg.depth; z++)
            {
                for (int x = 0; x < gg.width; x++)
                {
                    var node = gg.GetNode(x, z);
                    // This example uses perlin noise to generate the map
                    node.Walkable = walkable;
                }
            }

            // Recalculate all grid connections
            // This is required because we have updated the walkability of some nodes
            gg.GetNodes(node => gg.CalculateConnections((GridNodeBase)node));

            // If you are only updating one or a few nodes you may want to use
            // gg.CalculateConnectionsForCellAndNeighbours only on those nodes instead for performance.
        }));
    }
}
