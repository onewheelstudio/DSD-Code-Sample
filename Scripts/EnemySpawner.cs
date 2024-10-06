using HexGame.Grid;
using OWS.ObjectPooling;
using System;
using System.Collections;
using System.Collections.Generic;
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

        private void Awake()
        {
            esm = FindObjectOfType<EnemySpawnManager>();
        }

        public void DoSpawn(int powerLevel, Vector3 position)
        {
            List<Wave> waves = esm.GetSpawnWaves(powerLevel);
            StartCoroutine(SpawnWaves(waves, position));
        }

        private IEnumerator SpawnWaves(List<Wave> waveList, Vector3 postion)
        {
            foreach (var wave in waveList)
            {
                target = EnemyTargeting.GetHighestValueTarget(postion);
                //MessagePanel.ShowMessage("Enemy Spawning", this.gameObject);
                for (int i = 0; i < wave.number; i++)
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