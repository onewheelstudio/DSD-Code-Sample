using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OWS.ObjectPooling;

namespace HexGame.Units
{

    public class Tank : PlayerUnit
    {
        [SerializeField]
        private float reloadTime = 2f;
        [SerializeField]
        private ProjectileData projectileData;

        [SerializeField]
        private bool canFire = true;
        List<Unit> nearbyEnemies = new List<Unit>();
        [SerializeField]
        private Transform target;
        [SerializeField]
        private Transform launchPoint;

        private static ObjectPool<PoolObject> projectilePool;

        private void Awake()
        {
            if (projectilePool == null)
                projectilePool = new ObjectPool<PoolObject>(projectileData.projectilePrefab);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<EnemyUnit>(out EnemyUnit enemy))
                nearbyEnemies.Add(enemy);

        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<EnemyUnit>(out EnemyUnit enemy))
                nearbyEnemies.Remove(enemy);
        }

        private void Update()
        {
            if (canFire && nearbyEnemies.Count > 0)
            {
                Fire(GetNearestEnemy());
            }
        }

        private Transform GetNearestEnemy()
        {
            Transform nearestEnemy = null;
            float distance = Mathf.Infinity;
            int num = nearbyEnemies.Count > 20 ? 20 : nearbyEnemies.Count;

            for (int i = 0; i < num; i++)
            {
                if (nearbyEnemies[i] == null)
                    continue;

                float dist = (nearbyEnemies[i].transform.position - this.transform.position).sqrMagnitude;
                if (dist < distance)
                {
                    distance = dist;
                    nearestEnemy = nearbyEnemies[i].transform;
                }
            }

            target = nearestEnemy;
            return nearestEnemy;
        }

        private void EnemyListCleanUp()
        {
            nearbyEnemies.RemoveAll(enemy => enemy == null);
        }

        private void Fire(Transform target)
        {
            if (!canFire || target == null)
                return;

            GameObject projectile = projectilePool.PullGameObject();
            projectile.transform.position = this.launchPoint.position;
            projectile.transform.LookAt(target);
            canFire = false;
            EnemyListCleanUp();
            StartCoroutine(ReloadTimer());
        }

        IEnumerator ReloadTimer()
        {
            if (canFire)
                yield break;
            else
            {
                yield return new WaitForSeconds(reloadTime);
                canFire = true;
            }
        }
    }

}