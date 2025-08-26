using HexGame.Resources;
using System.Collections.Generic;
using UnityEngine;

namespace HexGame.Units
{
    public class MissileTowerBehavior : UnitBehavior
    {
        [SerializeField] private LayerMask enemyLayer;
        private MissileTurret[] turretList;
        private UnitDetection unitDetection;
        private UnitStorageBehavior storageBehavior;

        private List<Unit> enemyList = new List<Unit>();

        private void Awake()
        {
            storageBehavior.AddDeliverType(ResourceType.Energy);
        }

        protected void OnEnable()
        {
            turretList = GetComponentsInChildren<MissileTurret>();
            unitDetection = GetComponentInChildren<UnitDetection>();
            storageBehavior = GetComponent<UnitStorageBehavior>();

            DayNightManager.toggleDay += RequestResources;

            foreach (var t in turretList)
            {
                t.MissileCreated += storageBehavior.CheckResourceLevels;
            }
        }

        private void OnDisable()
        {
            DayNightManager.toggleDay -= RequestResources;
            foreach (var t in turretList)
            {
                t.MissileCreated -= storageBehavior.CheckResourceLevels;
            }
        }

        private void Update()
        {
            if (!isFunctional)
                return;

            if(!DayNightManager.isNight)
                return;

            UpdateEnemyList();

            foreach (var t in turretList)
            {
                if (t.NeedsTarget())
                    t.SetTarget(GetTarget());
            }

        }

        private Unit GetTarget()
        {
            Unit target = null;
            float distance = Mathf.Infinity;

            foreach (var enemy in enemyList)
            {
                if (enemy == null || !enemy.gameObject.activeSelf)
                    continue;

                float dist = (enemy.transform.position - this.transform.position).sqrMagnitude;
                //removed the "can see" as it seemly was causing issues with enemies not being seen, but directly above player unit
                if (dist < distance && dist > GetStat(Stat.minRange))// && CanSeeTarget(enemy))
                {
                    distance = dist;
                    target = enemy;
                }
            }
            enemyList.Remove(target);
            return target;
        }

        private bool CanSeeTarget(Unit target)
        {
            Ray ray = new Ray(this.transform.position + Vector3.up * 0.5f, target.transform.position + Vector3.up * 0.1f - this.transform.position - Vector3.up * 0.5f);

            if (Physics.Raycast(ray, out RaycastHit hit, GetStat(Stat.maxRange), enemyLayer))
            {
                if (enemyLayer == (enemyLayer | 1 << hit.transform.gameObject.layer))
                    return true;
            }

            return false;
        }

        private void UpdateEnemyList()
        {
            //enemyList.RemoveAll(enemy => enemy == null);
            //enemyList.RemoveAll(enemy => enemy.gameObject.activeSelf == false);
            if(unitDetection.GetTargetList().Count != enemyList.Count)
                enemyList = new List<Unit>(unitDetection.GetTargetList());
        }

        public override void StartBehavior()
        {
            isFunctional = true;
            storageBehavior.CheckResourceLevels();
        }

        public override void StopBehavior()
        {
            isFunctional = false;
        }

        private void RequestResources(int obj)
        {
            storageBehavior.CheckResourceLevels();
        }
    }

}