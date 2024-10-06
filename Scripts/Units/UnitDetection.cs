using HexGame.Grid;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HexGame.Units
{
    [RequireComponent(typeof(SphereCollider))]
    public class UnitDetection : MonoBehaviour
    {
        public bool isPlayerUnit = false;
        [SerializeField]
        private List<Unit> targetList = new List<Unit>();
        [SerializeField]
        public List<EnemyUnitType> typesToDetect = new List<EnemyUnitType>();
        [HideIf("isPlayerUnit")]
        public List<PlayerUnitType> typesToIgnore = new List<PlayerUnitType>() { PlayerUnitType.landMine };
        [SerializeField]
        private bool usePhysicalRange = false;
        private float minRange = 0f;
        private float maxRange = 0f;

        private void OnEnable()
        {
            if (this.GetComponentInParent<PlayerUnit>() == null)
                isPlayerUnit = false;
            else
                isPlayerUnit = true;

            if(usePhysicalRange)
                this.GetComponent<SphereCollider>().radius = this.GetComponentInParent<Unit>().GetStat(Stat.maxRange);
            else
            {
                maxRange = GetMaxRange();
                this.GetComponent<SphereCollider>().radius = maxRange;
            }

            if(usePhysicalRange)
                this.GetComponent<SphereCollider>().radius = this.GetComponentInParent<Unit>().GetStat(Stat.minRange);
            else
                minRange = GetMinRange();
        }

        private float GetMaxRange()
        {
            return (Hex3.SQRT3 / 2f * (1 + 2 * this.GetComponentInParent<Unit>().GetStat(Stat.maxRange))) / this.transform.parent.localScale.x;
        }
        
        private float GetMinRange()
        {
            return (Hex3.SQRT3 / 2f * (1 + 2 * this.GetComponentInParent<Unit>().GetStat(Stat.minRange))) / this.transform.parent.localScale.x;
        }

        private void OnDisable()
        {
            //needed since object can be reused
            targetList.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isPlayerUnit && other.gameObject.TryGetComponent<PlayerUnit>(out PlayerUnit playerUnit) 
                && !targetList.Contains(playerUnit)
                && !typesToIgnore.Contains(playerUnit.unitType))
                targetList.Add(playerUnit);
            else if (isPlayerUnit && other.gameObject.TryGetComponent<EnemyUnit>(out EnemyUnit enemyUnit) && !targetList.Contains(enemyUnit)
                && CanDetectEnemyUnit(enemyUnit))
                targetList.Add(enemyUnit);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isPlayerUnit && other.gameObject.TryGetComponent<PlayerUnit>(out PlayerUnit playerUnit))
                targetList.Remove(playerUnit);
            else if (isPlayerUnit && other.gameObject.TryGetComponent<EnemyUnit>(out EnemyUnit enemyUnit))
                targetList.Remove(enemyUnit);
        }

        private bool CanDetectEnemyUnit(EnemyUnit enemy)
        {
            return typesToDetect.Contains(enemy.type);
        }

        public Unit GetNearestTarget()
        {
            CleanUpPlayerUnitList();

            if (targetList.Count == 0)
                return null;

            return GetTargetList().OrderBy(target => (target.transform.position - this.transform.position).sqrMagnitude).FirstOrDefault();
        }

        [Button]
        public List<Unit> GetTargetList()
        {
            CleanUpPlayerUnitList();
            List<Unit> tempList = new List<Unit>();
            for (int i = 0; i < targetList.Count; i++)
            {
                if (!targetList[i].gameObject.activeInHierarchy)
                    continue;

                int distance = Hex3.DistanceBetween(targetList[i].transform.root.position, this.transform.position);
                if (distance < minRange || distance > maxRange)
                    continue;

                tempList.Add(targetList[i]);
            }   

            return tempList;

            //return targetList.Where(target => Hex3.DistanceBetween(target.transform.root.position, this.transform.position) > minRange)
            //                 .Where(target => Hex3.DistanceBetween(target.transform.root.position, this.transform.position) < maxRange)
            //                 .ToList();
        }

        public bool TargetIsInList(Unit target)
        {
            return GetTargetList().Contains(target);
        }

        public bool HasTargetInRange()
        {
            return GetTargetList().Count > 0;
        }

        private void CleanUpPlayerUnitList()
        {
            for(int i = targetList.Count - 1; i >= 0; i--)
            {
                if (targetList[i] == null || targetList[i].gameObject.activeSelf == false)
                    targetList.RemoveAt(i);
            }
        }

    }
}