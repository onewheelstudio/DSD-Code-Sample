using Pathfinding;
using System;
using System.Collections;
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
        private Seeker seeker;
        private UnitState currentState;
        private UnitBehavior[] behaviors;
        private bool noPath = false;
        private Coroutine selfDestructTimer;
        private WaitForSeconds selfDestructWait = new WaitForSeconds(5f);
        private Vector3 targetLocation;

        private void OnEnable()
        {
            if (unitDetection == null)
                unitDetection = this.GetComponentInChildren<UnitDetection>();
            if (seeker == null)
                seeker = this.GetComponent<Seeker>();

            //seeker.pathCallback += PathComplete;
        }

        private void OnDisable()
        {
            //seeker.pathCallback -= PathComplete;
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
            //checks if target is in range - will not be on the list if not in range
            //if (unitDetection.TargetIsInList(target) && TargetInsideMaxRange(target))
            //    setDestination.SetTargetLocation(this.transform.position);
            //else
            //    target = null;


            if (target == null || !target.gameObject.activeInHierarchy)
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
            //if (target != null && target.gameObject.activeInHierarchy)
            //    return target;

            if (!unitDetection.HasTargetInRange())
                return EnemyTargeting.GetHighestValueTarget(this.transform.position, this.GetMinRangeInHex());
            else
                return unitDetection.GetNearestTarget();
        }

        public void SetTarget(Unit target)
        {
            if (targetLocation == unit.transform.position)
                return;
            targetLocation = unit.transform.position;

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

            this.target = target;
            //setDestination.SetTarget(target.transform);
            seeker.StartPath(this.transform.position, target.transform.position, PathComplete);
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
                noPath = false;
                if(selfDestructTimer != null)
                    StopCoroutine(selfDestructTimer);
                currentState = UnitState.movingToTarget;
                setDestination.SetPath(p, target.transform);
            }
            else
            {
                currentState = UnitState.searchingForNewTarget;
                target = null; //can no longer get to target
                if (!noPath)
                    selfDestructTimer = StartCoroutine(CountDownTimer());
                noPath = true;
                Debug.LogError("Unit Does not have a target", this.gameObject);
            }
        }

        private IEnumerator CountDownTimer()
        {
            yield return selfDestructWait;
            if (noPath)
            {
                this.gameObject.GetComponent<EnemyUnit>().DoDamage(1000);
                Debug.LogError("Unit self destructed", this.gameObject);
            }
        }

        private bool PathGoesToTarget(Path p, Unit target)
        {
            if (target == null || p.path.Count == 0)
            {
                return false;
            }

            if (((Vector3)p.path[p.path.Count - 1].position - target.transform.position).sqrMagnitude < 1f)
                return true;
            else
            {
                return false;
            }
        }

        private bool TargetInsideMinRange(Unit target)
        {
            if(target == null)
                return false;

            return (target.transform.position - this.transform.position).sqrMagnitude < GetStat(Stat.minRange) * GetStat(Stat.minRange);
        }
        
        private bool TargetInsideMaxRange(Unit target)
        {
            if(target == null)
                return false;

            return (target.transform.position - this.transform.position).sqrMagnitude < GetStat(Stat.maxRange) * GetStat(Stat.maxRange);
        }
    }

}