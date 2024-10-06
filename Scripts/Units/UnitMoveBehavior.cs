using HexGame.Grid;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using HexGame.Resources;

namespace HexGame.Units
{
    [RequireComponent(typeof(HoverMoveBehavior))]
    public class UnitMoveBehavior : UnitBehavior, IHaveTarget, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField]
        private Vector3 destination;
        private HoverMoveBehavior hmb;
        private bool isSelected;
        public event Action<Vector3> destinationSet;

        private void OnEnable()
        {
            hmb = GetComponent<HoverMoveBehavior>();
            if(unit == null)
            {
                //really dumb 
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.gameObject == this.gameObject)
                isSelected = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isSelected)
                return;

            if (UnitManager.UnitAtMouseLocation())
                return;

            isSelected = false;
            destination = ((Hex3)eventData.pointerCurrentRaycast.worldPosition).ToVector3();
            if (!CanMoveToTileType(HexTileManager.GetHexTileAtLocation(destination).TileType))
                return;
            
            hmb.SetDestination(destination);
            destinationSet?.Invoke(destination);
        }

        private bool CanMoveToTileType(HexTileType tileType)
        {
            return unit.PlacementListContains(tileType);
        }

        public void SetTarget(Unit target)
        {
        }

        public override void StartBehavior()
        {
            isFunctional = true;
        }

        public override void StopBehavior()
        {
            isFunctional = false;
        }

    }

}