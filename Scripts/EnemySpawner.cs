using HexGame.Grid;
using OWS.ObjectPooling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HexGame.Units
{
    public class EnemySpawner : MonoBehaviour
    {
        private Unit target;
        private float waveDelay = 3f;
        [SerializeField]
        private Dictionary<string, ObjectPool<EnemyUnit>> enemyPools = new Dictionary<string, ObjectPool<EnemyUnit>>();
        private EnemySpawnManager esm;
        public static event Action<EnemyUnit, Vector3> enemySpawned;

        public static event Action<Hex3> spawnFinished;
        [SerializeField] private float specialProjectMultiplier = 1.2f;

        private void Awake()
        {
            esm = FindFirstObjectByType<EnemySpawnManager>();
        }

        public void DoSpawn(int powerLevel, Vector3 position)
        {
            List<Wave> waves = esm.GetSpawnWaves(powerLevel);
            StartCoroutine(SpawnWaves(waves, position));
        }

        private IEnumerator SpawnWaves(List<Wave> waveList, Vector3 postion)
        {
            int totalCount = waveList.Sum(w => w.number);
            Debug.Log($"Spawning: {totalCount}");
            if (totalCount <= 0)
            {
                Debug.LogError("No enemies to spawn.");
                FinishSpawn();
                yield break;
            }

            foreach (var wave in waveList)
            {
                target = EnemyTargeting.GetHighestValueTarget(postion);
                //MessagePanel.ShowMessage("Enemy Spawning", this.gameObject);
                int count = wave.number;
                if (SpecialProjectBehavior.HasProject)
                {
                    count = Mathf.CeilToInt(count * specialProjectMultiplier);
                }

                for (int i = 0; i < count; i++)
                {
                    EnemyUnit newEnemy = esm.GetEnemy(wave.type);
                    if (newEnemy == null)
                        continue;
                    newEnemy.transform.position = postion;
                    newEnemy.GetComponent<EnemyUnit>().Place();
                    SetAllTargets(newEnemy, target);
                    EnemyIndicator.AddIndicatorObject(newEnemy.gameObject, IndicatorType.enemyUnit);
                    yield return null;
                }
            }

            FinishSpawn();
        }

        private EnemyUnit PullEnemyUnitByType(string prefabName)
        {
            if (enemyPools.TryGetValue(prefabName, out var enemyPool))
                return enemyPool.Pull();
            else
                return null;
        }

        private void SetAllTargets(EnemyUnit newEnemy, Unit target)
        {
            foreach (var behavior in newEnemy.gameObject.GetComponents<IHaveTarget>())
            {
                behavior.SetTarget(target);
            }
        }

        private void FinishSpawn()
        {
            //other bits...?
            spawnFinished?.Invoke(this.transform.position.ToHex3());
        }
    } 
}