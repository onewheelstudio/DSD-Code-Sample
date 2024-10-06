using HexGame.Units;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DamageVisualizationBehavior : UnitBehavior
{
    private PlayerUnit playerUnit;
    [SerializeField] private bool allowRepairs = true;
    [Header("Smoke Effects")]
    [SerializeField] private List<Smoke> smokePositions = new List<Smoke>();
    private static ObjectPool<PoolObject> smokePool;
    [SerializeField] private GameObject smokePrefab;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach (var pos in smokePositions)
        {
            Gizmos.DrawSphere(pos.position, 0.1f);
        }
    }

    private void Awake()
    {
        playerUnit = this.GetComponent<PlayerUnit>();
        if(smokePrefab != null)
            smokePool ??= new ObjectPool<PoolObject>(smokePrefab);
    }

    public override void StartBehavior()
    {
        _isFunctional = true;
        playerUnit.unitDamaged += HPChanged;
        playerUnit.unitRepaired += HPChanged;
    }

    public override void StopBehavior()
    {
        _isFunctional = false;
        playerUnit.unitDamaged -= HPChanged;
        playerUnit.unitRepaired -= HPChanged;
    }

    private void AdjustSmoke()
    {
        if(smokePositions.Count == 0)
            return;

        float percentDamage =  (playerUnit.GetStat(Stat.hitPoints) - (playerUnit.GetHP())) / playerUnit.GetStat(Stat.hitPoints);
        float smokePercent = 1f / smokePositions.Count;
        int smokeToActivate = Mathf.RoundToInt(percentDamage / smokePercent);

        for (int i = 0; i < smokePositions.Count; i++)
        {
            if(i < smokeToActivate && smokePositions[i].smoke == null)
            {
                smokePositions[i].smoke = smokePool.PullGameObject();
                smokePositions[i].smoke.transform.SetParent(this.transform);
                smokePositions[i].smoke.transform.localPosition = smokePositions[i].position;
            }
            else if (smokePositions[i].smoke != null)
            {
                smokePositions[i].smoke.SetActive(false);
                smokePositions[i].smoke = null;
            }
        }

    }

    [Button]
    private void HPChanged(Unit unit, float damage)
    {
        AdjustSmoke();
    }

    [Button]
    private void StorePositions()
    {
        if(Application.isPlaying)
            return;

        Transform[] positions = this.transform.GetComponentsInChildren<Transform>(true);
        for (int i = positions.Length - 1; i >= 0; i--)
        {
            if (positions[i].name.Contains("Smoke and Sparks")
                && !smokePositions.Any(x => x.position == positions[i].localPosition))
            {
                smokePositions.Add(new Smoke() { position = positions[i].localPosition });
                //DestroyImmediate(positions[i].gameObject);
            }
        }
    }

    [System.Serializable]
    public class Smoke
    {
        public Vector3 position;
        public GameObject smoke;
    }

}
