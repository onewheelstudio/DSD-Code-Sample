using HexGame.Grid;
using HexGame.Resources;
using Pathfinding;
using System.Collections.Generic;
using UnityEngine;

namespace HexGame.Units
{
    public class TargetingBehavior : UnitBehavior, IHaveTarget
    {
        private UnitDetection unitDetection;
        [SerializeField]
        public Unit target { get; private set; }
        protected SetDestination setDestination;
        public bool HasPath => setDestination.HasPath;
        private Seeker seeker;
        private UnitState currentState;
        private UnitBehavior[] behaviors;
        private WaitForSeconds selfDestructWait = new WaitForSeconds(5f);
        private bool noPath = false;
        public bool NoPath => noPath;

        private void OnEnable()
        {
            if (unitDetection == null)
                unitDetection = this.GetComponentInChildren<UnitDetection>();
            if (seeker == null)
                seeker = this.GetComponent<Seeker>();
        }

        private void OnDisable()
        {
            behaviors = this.GetComponents<UnitBehavior>();
            StopAllCoroutines();
            target = null;
        }

        public override void StartBehavior()
        {
            _isFunctional = true;
            target = null;
        }

        public override void StopBehavior()
        {
            _isFunctional = false;
        }

        private void Update()
        {
            if (!_isFunctional)
                return;

            if (target == null || !target.gameObject.activeInHierarchy || !HasPath)
                SetTarget(GetTarget());
            else if (unitDetection.HasTargetInRange() && !unitDetection.TargetIsInList(target))
                SetTarget(unitDetection.GetNearestTarget());

            switch (currentState)
            {
                case UnitState.movingToTarget:
                    break;
                case UnitState.searchingForNewTarget:
                    break;
                default:
                    break;
            }
        }

        protected Unit GetTarget()
        {
            if (!unitDetection.HasTargetInRange())
                return EnemyTargeting.GetHighestValueTarget(this.transform.position, this.GetMinRangeInHex());
            else
                return unitDetection.GetNearestTarget();
        }

        public void SetTarget(Unit target)
        {
            SetAllTargets(target);
            if (target == null)
                return;

            if (setDestination == null)
                setDestination = this.GetComponent<SetDestination>();

            foreach (var behavior in this.GetComponents<IHaveTarget>())
            {
                if (behavior as UnitBehavior != this)
                    behavior.SetTarget(target);
            }

            HexTile tile = HexTileManager.GetHexTileAtLocation(target.transform.position);
            if (tile != null && tile.TileType == HexTileType.hill)
            {
                Debug.Log("Target is on a hill");
                //NavTargetUnit navTarget = target.GetComponentInChildren<NavTargetUnit>();
                //if(navTarget != null)
                //    target = navTarget;
                //else
                //{
                //    navTarget = PlaceNavTargetUnit(target);
                //    target = navTarget;
                //}
            }

            this.target = target;
            //setDestination.SetTarget(target.transform);
            seeker.StartPath(this.transform.position, target.transform.position, PathComplete);
        }


        //finds closest walkable tile to target and places NavTargetUnit on it
        private NavTargetUnit PlaceNavTargetUnit(Unit target)
        {
            List<Hex3> neighbors = Hex3.GetNeighborLocations(target.transform.position);
            float distance = float.MaxValue;
            HexTile location = null;
            foreach (var neighbor in neighbors)
            {
                HexTile tile = HexTileManager.GetHexTileAtLocation(neighbor);
                if (tile == null || !tile.Walkable)
                    continue;

                float dist = (tile.transform.position - this.transform.position).magnitude;
                if (dist < distance)
                {
                    distance = dist;
                    location = tile;
                }
            }

            if (location == null)
            {
                Debug.LogError("No valid location for NavTargetUnit", this.gameObject);
                return null;
            }
            else
            {
                GameObject navTarget = new GameObject("NavTargetUnit");
                navTarget.transform.position = location.transform.position;
                navTarget.transform.SetParent(target.transform);
                return navTarget.AddComponent<NavTargetUnit>();
            }
        }

        private void SetAllTargets(Unit target)
        {
            if (behaviors == null || behaviors.Length == 0)
                return;

            foreach (var behavior in behaviors)
            {
                if (behavior is IHaveTarget iHaveTarget && behavior != this)
                    iHaveTarget.SetTarget(target);
            }
        }

        private void PathComplete(Path p)
        {
            if (PathGoesToTarget(p, target))
            {
                currentState = UnitState.movingToTarget;
                setDestination.SetPath(p, target.transform);
                noPath = false;
            }
            else
            {
                currentState = UnitState.searchingForNewTarget;
                target = null; //can no longer get to target
                noPath = true;
                Debug.LogError("Unit Does not have a target", this.gameObject);
            }
        }

        private bool PathGoesToTarget(Path p, Unit target)
        {
            if (target == null || p == null || p.path == null || p.path.Count == 0)
            {
                return false;
            }

            if (((Vector3)p.path[p.path.Count - 1].position - target.transform.position).sqrMagnitude < 1f)
                return true;

            HexTile targetTile = HexTileManager.GetHexTileAtLocation(target.transform.position);
            if (targetTile == null)
                return false;

            if (targetTile.TileType == HexTileType.hill)
            {
                HexTile hexTile = HexTileManager.GetHexTileAtLocation(p.vectorPath[^2]);
                if (hexTile == null)
                    return false;

                return hexTile.Walkable;
            }
            else
            {
                return false;
            }
        }

        private bool TargetInsideMinRange(Unit target)
        {
            if (target == null)
                return false;

            return (target.transform.position - this.transform.position).sqrMagnitude < GetStat(Stat.minRange) * GetStat(Stat.minRange);
        }

        private bool TargetInsideMaxRange(Unit target)
        {
            if (target == null)
                return false;

            return (target.transform.position - this.transform.position).sqrMagnitude < GetStat(Stat.maxRange) * GetStat(Stat.maxRange);
        }
    }

}