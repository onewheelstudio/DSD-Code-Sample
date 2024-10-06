using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using OWS.ObjectPooling;
using System;

namespace HexGame.Units
{
    public class KinematicShootBehavior : UnitBehavior
    {
        [SerializeField]
        private ProjectileData projectileData;
        private static ObjectPool<KinematicProjectile> projectilePool;
        [SerializeField]
        private Transform launchPosition;
        [SerializeField]
        private List<Unit> enemyList = new List<Unit>();
        private UnitDetection unitDetection;
        private UnitStorageBehavior storageBehavior;
        private bool canFire = true;
        private bool usePrediction = false;

        public override void StartBehavior()
        {
            _isFunctional = true;
            storageBehavior.CheckResourceLevels();
        }

        public override void StopBehavior()
        {
            _isFunctional = false;
        }

        private void OnEnable()
        {
            if (projectilePool == null)
                projectilePool = new ObjectPool<KinematicProjectile>(projectileData.projectilePrefab);

            if (unitDetection == null)
                unitDetection = this.GetComponentInChildren<UnitDetection>();

            if (storageBehavior == null)
                storageBehavior = this.GetComponent<UnitStorageBehavior>();

            DayNightManager.toggleDay += RequestResources;
        }

        private void OnDisable()
        {
            DayNightManager.toggleDay -= RequestResources;
        }

        private void RequestResources(int obj)
        {
            storageBehavior.CheckResourceLevels();
        }

        private void Update()
        {
            if (!_isFunctional)
                return;

            if(!DayNightManager.isNight)
                return;

            UpdateEnemyList();
            if (enemyList.Count > 0)
                ShootAtUnit(GetTarget());
        }

        private Vector3 GetTargetVelocity(Unit target)
        {
            if (target.TryGetComponent<Pathfinding.AIBase>(out Pathfinding.AIBase aiBase))
            {
                return aiBase.velocity;
            }

            return Vector3.zero;
        }

        private void ShootAtUnit(Unit target)
        {
            if (!canFire || target == null)
                return;

            if(usePrediction)
                ShootAtPosition(PredictTargetPosition(target));
            else
                ShootAtPosition(target.transform.position);
            StartCoroutine(ShootCoolDown());
        }

        [Button]
        private void ShootAtPosition(Vector3 target)
        {
            Vector3 vectorToTarget = target - this.transform.position;
            float tangent = GetTangentOfAngle(target);

            if (tangent <= 0f)
                return;

            float y = (vectorToTarget).magnitude * tangent;

            Vector3 launchVector = new Vector3(vectorToTarget.x, y, vectorToTarget.z);
            launchVector = launchVector.normalized;

            KinematicProjectile projectile = projectilePool.Pull();
            projectile.Shoot(launchPosition.position, launchVector);
        }

        private Vector3 PredictTargetPosition(Unit target)
        {
            float angle = Mathf.Atan(GetTangentOfAngle(target.transform.position));
            float time = TimeOfFlight(target.transform.position, angle);
            Vector3 velocity = GetTargetVelocity(target);

            time = TimeOfFlight(target.transform.position + time * velocity, angle);

            return target.transform.position + time * velocity;
        }

        private float TimeOfFlight(Vector3 target, float angle)
        {
            float sineOfAngle = Mathf.Sin(angle);
            float speed = projectileData.GetStat(Stat.speed);
            float height = target.y - this.launchPosition.position.y;
            float g = Mathf.Abs(Physics.gravity.y);

            return (speed * sineOfAngle + Mathf.Sqrt((speed * speed * sineOfAngle * sineOfAngle) + 2 * g * height)) / g;
        }

        private float GetTangentOfAngle(Vector3 target)
        {
            float speed = projectileData.GetStat(Stat.speed);
            float xDist = (new Vector3(target.x - this.transform.position.x, 0f, target.z - this.transform.position.z)).magnitude;
            float yDist = Mathf.Abs(target.y - this.transform.position.y);
            float g = Mathf.Abs(Physics.gravity.y);
            float inside = Mathf.Pow(speed, 4) - g * (g * xDist * xDist + 2 * yDist * speed * speed);

            if (inside < 0)
                return -1;

            float top = speed * speed + Mathf.Sqrt(inside);
            float bottom = g * xDist;
            float tangent = top / bottom;
            return tangent;
        }

        private void UpdateEnemyList()
        {
            //enemyList.RemoveAll(enemy => enemy == null);
            //enemyList.RemoveAll(enemy => enemy.gameObject.activeSelf == false);
            enemyList = new List<Unit>(unitDetection.GetTargetList());
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
                if (dist < distance && dist > GetStat(Stat.minRange))
                {
                    distance = dist;
                    target = enemy;
                }
            }
            enemyList.Remove(target);
            return target;
        }

        private IEnumerator ShootCoolDown()
        {
            canFire = false;
            yield return new WaitForSeconds(GetStat(Stat.reloadTime));
            canFire = true;
        }

    }
}