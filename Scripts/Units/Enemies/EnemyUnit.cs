using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System;
using System.Linq;
using UnityEngine;

namespace HexGame.Units
{
    public class EnemyUnit : Unit, IPlaceable, IPoolable<EnemyUnit>
    {
        public EnemyUnitType type;
        private System.Action<EnemyUnit> pushToPool;
        private FogUnit[] fogList;
        public bool isVisible => IsGroupVisible();

        [SerializeField] private EnemyGroup enemyGroup;
        public EnemyGroup EnemyGroup => enemyGroup;
        public static event Action<EnemyUnit> enemyUnitSpawned;
        public static event Action<EnemyUnit> enemyUnitKilled;
        public event Action ThisUnitDied;

        private void Awake()
        {
            fogList = GetComponentsInChildren<FogUnit>(); 
        }

        private new void OnEnable()
        {
            base.OnEnable();
            this.localStats[Stat.hitPoints] *= enemyGroup.subUnits.Count;
            foreach (var subUnit in enemyGroup.subUnits)
            {
                if (subUnit == null)
                    break;
                subUnit.TurnOn();
            }
            enemyUnitSpawned?.Invoke(this);
        }

        private new void OnDisable()
        {
            foreach (var subUnit in enemyGroup.subUnits)
            {
                if (subUnit == null)
                    break;
                subUnit.TurnOff();
            }
            base.OnDisable();
        }

        [Button]
        public void Initialize(Action<EnemyUnit> pushToPool)
        {
            this.pushToPool = pushToPool;
            base.Intialize();
            this.localStats[Stat.hitPoints] *= enemyGroup.subUnits.Count;

            if (this.TryGetComponent(out Pathfinding.AIPath pathfinding))
            {
                pathfinding.maxSpeed = this.GetStat(Stat.speed);
                if (pathfinding.maxSpeed < 0.01f)
                    Debug.LogWarning($"Max Speed set too low on {this.gameObject.name} : {this.GetStat(Stat.speed)} : {pathfinding.maxSpeed}");
            }
        }

        public void DoDamage(float damage, EnemySubUnit enemySubUnit)
        {
            if (DayNightManager.isDay)
                damage *= 2f;
            float armorModification = GetStat(Stat.shield) <= 0 ? 1f : Mathf.Pow(0.99f, GetStat(Stat.shield));
            damage *= armorModification;
            
            this.stats.DoDeathParticles(enemySubUnit.TargetPoint); // fix this its getting call WAY too many times

            if (damage > this.stats.GetStat(Stat.hitPoints))
            {
                damage = this.stats.GetStat(Stat.hitPoints);
                enemySubUnit.TurnOff();
            }

            if (localStats.TryGetValue(Stat.hitPoints, out float hitPoints))
            {
                localStats[Stat.hitPoints] = hitPoints - damage * armorModification;
                int numDead = enemyGroup.subUnits.Count - Mathf.CeilToInt(localStats[Stat.hitPoints] / enemyGroup.subUnits.Count);
                for (int i = 0; i < numDead; i++)
                {
                    if (i >= enemyGroup.subUnits.Count) //is this causing the "too many" issue?
                        continue;

                    enemyGroup.subUnits[i].TurnOff();
                }
            }

            if (enemyGroup.subUnits.Count(x => !x.IsDead) == 0)
                Die();
        }

        public override void DoDamage(float damage)
        {
            if(invuneralble)
                return;

            float armorModification = GetStat(Stat.shield) <= 0 ? 1f : Mathf.Pow(0.99f, GetStat(Stat.shield));
            //damage is local so change local stats
            if (localStats.TryGetValue(Stat.hitPoints, out float hitPoints))
            {
                localStats[Stat.hitPoints] = hitPoints - damage * armorModification;
                CheckForDead();
            }
            else
                Debug.LogError($"Trying to damage {gameObject.name} and there is no instance stat for Hit Points");

            if (enemyGroup.subUnits.Count(x => !x.IsDead) == 0 && enemyGroup.subUnits.Count > 0)
                Die();
        }

        public override Transform GetTarget()
        {
            foreach (var subUnit in enemyGroup.subUnits)
            {
                if(subUnit.gameObject.activeSelf)
                    return subUnit.transform;
            }

            return this.transform;
        }

        public void ReturnToPool()
        {
            pushToPool?.Invoke(this);
        }

        public void SelfDestruct()
        {
            foreach (var subUnit in enemyGroup.subUnits)
            {
                if (subUnit == null)
                    break;
                subUnit.TurnOff();
            }

            Die();
        }

        protected override void Die()
        {
            enemyUnitKilled?.Invoke(this);
            ThisUnitDied?.Invoke();
            stats.DoDeathParticles(this.transform.position);
            ReturnToPool();
            this.gameObject.SetActive(false);
            //base.Die(); //not using due to order of operations and making sure object goes back to the pool before getting turned off
        }

        [Button]
        //apparently you do this manually. That might be dumb.
        private void UpdateEnemyGroup()
        {
            this.enemyGroup.subUnits.Clear();
            this.enemyGroup.subUnits = this.GetComponent<FollowParent>().GetSubUnits();
        }

        private bool IsGroupVisible()
        {
            for (int i = 0; fogList.Length > i; i++)
            {
                if (!fogList[i].IsDown)
                    return true;
            }

            return false;
        }
    } 
}
