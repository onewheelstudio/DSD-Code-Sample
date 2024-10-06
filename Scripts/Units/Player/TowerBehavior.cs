using HexGame.Resources;
using System.Collections.Generic;
using UnityEngine;
using static HexGame.Resources.ResourceProductionBehavior;

namespace HexGame.Units
{
    public class TowerBehavior : UnitBehavior
    {
        [SerializeField] private LayerMask enemyLayer;
        private Turret[] turretList;
        private UnitDetection unitDetection;
        private UnitStorageBehavior storageBehavior;

        private List<Unit> enemyList = new List<Unit>();

        [SerializeField]
        protected List<ResourceType> resourcesNeeded = new List<ResourceType>();

        private float startTime;

        protected void OnEnable()
        {
            turretList = GetComponentsInChildren<Turret>();
            unitDetection = GetComponentInChildren<UnitDetection>();
            storageBehavior = GetComponent<UnitStorageBehavior>();
            storageBehavior.resourceDelivered += ResourceDelivered;
            DayNightManager.toggleDay += RequestResources;
            DayNightManager.toggleDay += SetWarningStatus;

        }

        private void OnDisable()
        {
            DayNightManager.toggleDay -= RequestResources;
            DayNightManager.toggleDay -= SetWarningStatus;
            storageBehavior.resourceDelivered -= ResourceDelivered;
        }

        private void RequestResources(int obj)
        {
            storageBehavior.CheckResourceLevels();
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
            if(unitDetection.GetTargetList().Count != enemyList.Count)
                enemyList = new List<Unit>(unitDetection.GetTargetList());
        }

        public override void StartBehavior()
        {
            isFunctional = true;
            storageBehavior.CheckResourceLevels();
            SetWarningStatus();
        }

        public override void StopBehavior()
        {
            isFunctional = false;
        }

        private void ResourceDelivered(UnitStorageBehavior behavior, ResourceAmount amount)
        {
            SetWarningStatus();
        }

        public bool CanIShoot()
        {
            issueList.Clear();

            float efficiency = storageBehavior.efficiency;
            if (efficiency <= 0.01f)
                issueList.Add(ProductionIssue.noWorkers);
            else if (efficiency < 1f)
                issueList.Add(ProductionIssue.missingWorkers);

            ResourceAmount resourceNeeded = new ResourceAmount(resourcesNeeded[0], (int)this.unit.GetStat(Stat.maxStorage));

            if (!storageBehavior.HasResource(resourceNeeded))
                issueList.Add(ProductionIssue.missingResources);

            if (issueList.Count == 1 && issueList[0] == ProductionIssue.missingWorkers)
                return true;

            return issueList.Count == 0;
        }

        private void SetWarningStatus(int obj)
        {
            SetWarningStatus();
        }

        private void SetWarningStatus()
        {
            CanIShoot();

            if (!hasWarningIcon)
            {
                warningIconInstance = UnitManager.warningIcons.PullGameObject(this.transform.position, Quaternion.identity).GetComponent<WarningIcons>();
                warningIconInstance.transform.SetParent(this.transform);
            }

            warningIconInstance.SetWarnings(issueList);
        }
    }

}