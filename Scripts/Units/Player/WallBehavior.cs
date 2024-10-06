using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Resources;
using Pathfinding;

namespace HexGame.Units
{
    public class WallBehavior : UnitBehavior, IUnitBehavior
    {
        public override void StartBehavior()
        {
            if(Application.isPlaying)
                HelperFunctions.UpdatePathfindingGrid(this.gameObject);

            isFunctional = true;

            //AstarPath.active.AddWorkItem(new AstarWorkItem(ctx =>
            //{
            //    Hex3 tile1 = this.transform.position + this.transform.forward;
            //    Hex3 tile2 = this.transform.position - this.transform.forward;

            //    var node1 = AstarPath.active.data.gridGraph.GetNearest(tile1).node as GridNode;
            //    var node2 = AstarPath.active.data.gridGraph.GetNearest(tile2).node as GridNode;

            //    int rot = Mathf.RoundToInt(this.transform.rotation.eulerAngles.y);

            //    switch (rot)
            //    {
            //        case 90:
            //            node1.SetConnectionInternal(3, false);
            //            node2.SetConnectionInternal(1, false);
            //            break;
            //        case 30:
            //            node1.SetConnectionInternal(7, false);
            //            node2.SetConnectionInternal(5, false);
            //            break;
            //        case 330:
            //            node1.SetConnectionInternal(0, false);
            //            node2.SetConnectionInternal(2, false);
            //            break;
            //        default:
            //            break;
            //    }
            //}));
        }

        public override void StopBehavior()
        {
            if(Application.isPlaying)
                HelperFunctions.UpdatePathfindingGrid(this.gameObject);

            isFunctional = false;

            //AstarPath.active.AddWorkItem(new AstarWorkItem(ctx => {
            //    // Connect two nodes
            //    var node1 = AstarPath.active.GetNearest(this.transform.position + this.transform.forward).node;
            //    var node2 = AstarPath.active.GetNearest(this.transform.position - this.transform.forward).node;
            //    var cost = (uint)(node2.position - node1.position).costMagnitude;
            //    node1.AddConnection(node2, cost);
            //    node2.AddConnection(node1, cost);
            //}));
        } 
    }
}
