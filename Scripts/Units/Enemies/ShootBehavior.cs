using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Resources;
using OWS.ObjectPooling;

namespace HexGame.Units
{
    [RequireComponent(typeof(TargetingBehavior))]
    public class ShootBehavior : UnitBehavior
    {
        private TargetingBehavior targetingBehavior;
        private UnitDetection unitDetection;
        private Unit target => targetingBehavior.target;
        [SerializeField]
        private ProjectileData projectileData;
        [SerializeField]
        private Transform launchPoint;
        private bool canFire = true;

        private EnemyUnit enemyUnit;
        private EnemyGroup enemyGroup => enemyUnit?.EnemyGroup;

        [SerializeField] private GameObject lightingStrike;
        private static ObjectPool<Lighting> lightingStrikePool;

        private void Awake()
        {
            enemyUnit = this.GetComponent<EnemyUnit>();
            if (lightingStrikePool == null && lightingStrike != null)
                lightingStrikePool = new ObjectPool<Lighting>(lightingStrike);
        }

        private void OnEnable()
        {
            if (targetingBehavior == null)
                targetingBehavior = this.GetComponent<TargetingBehavior>();
            if (unitDetection == null)
                unitDetection = this.GetComponentInChildren<UnitDetection>();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        public override void StartBehavior()
        {
            _isFunctional = true;
            canFire = true; //important for respawn
        }

        public override void StopBehavior()
        {
            _isFunctional = false;
        }

        private void Update()
        {
            if (!_isFunctional)
                return;

            if (target != null && canFire && unitDetection.TargetIsInList(target))
                StartCoroutine(Shoot(target.transform));
        }

        protected IEnumerator Shoot(Transform target)
        {
            if (!canFire || target == null)
                yield break;

            //if(!PlayerResources.TryUseAllResources(projectileData.GetProjectileCost()))
            //    yield break;

            canFire = false;
            StartCoroutine(ReloadTimer());

            Vector3 lookAtTarget = target.position;
            lookAtTarget.y = 0.25f + GetHillOffset(target);
            this.transform.LookAt(lookAtTarget, Vector3.up);

            foreach (var subUnit in enemyGroup.subUnits)
            {
                GameObject projectile = projectileData.GetProjectile();
                projectile.GetComponent<Projectile>().SetStats(GetStat(Stat.maxRange), GetStat(Stat.damage));
                projectile.transform.position = subUnit.LaunchPoint;
                projectile.transform.LookAt(lookAtTarget);
                yield return new WaitForSeconds(Random.Range(0.0f, 0.5f));
            }
        }

        private float GetHillOffset(Transform target)
        {
            HexTile targetTile = HexTileManager.GetHexTileAtLocation(target.position);
            if (targetTile == null) //not sure why this would happen, but it seems to occasionally :)
                return 0f;
            return targetTile.TileType == HexTileType.hill ? UnitManager.HillOffset : 0f;
        }

        protected IEnumerator ReloadTimer()
        {
            if (canFire)
                yield break;
            else
            {
                yield return new WaitForSeconds(this.GetStat(Stat.reloadTime));
                yield return null; //wait one extra frame avoid errors is reload time is zero.
                canFire = true;
            }
        }
    }
}
