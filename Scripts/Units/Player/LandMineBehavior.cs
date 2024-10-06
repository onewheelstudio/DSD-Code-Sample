using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System.Threading.Tasks;
using UnityEngine;

namespace HexGame.Units
{
    public class LandMineBehavior : UnitBehavior
    {
        [SerializeField] private float timer = 5f;
        private bool timerRunning = false;
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private float explosionScale = 1f;
        [SerializeField] private float explosionOffset = 0.05f;

        [SerializeField] private Material waitingMaterial;
        [SerializeField] private Material armedMaterial;
        private MeshRenderer meshRenderer;

        private static ObjectPool<PoolObject> explosionPool;
        private Collider[] buffer;
        private UnitDetection unitDetection;

        private void Awake()
        {
            explosionPool = new ObjectPool<PoolObject>(explosionPrefab);
            this.meshRenderer = this.GetComponentInChildren<MeshRenderer>();
            this.unitDetection = this.GetComponentInChildren<UnitDetection>();
        }

        public override void StartBehavior()
        {
            Material[] materials = meshRenderer.sharedMaterials;
            materials[1] = waitingMaterial;
            meshRenderer.sharedMaterials = materials;
            isFunctional = true;
            timerRunning = false;
        }

        public override void StopBehavior()
        {
            isFunctional = false;
        }

        private void FixedUpdate()
        {
            if(timerRunning || !isFunctional)
                return;

            Unit enemyUnit = unitDetection.GetNearestTarget();
            if((unit.transform.position - this.transform.position).sqrMagnitude < this.GetStat(Stat.maxRange) * this.GetStat(Stat.maxRange))
                SetTimer();
        }

        [Button]
        private async void SetTimer()
        {
            timerRunning = true;
            Material[] materials = meshRenderer.sharedMaterials;
            materials[1] = armedMaterial;
            meshRenderer.sharedMaterials = materials;
            await Task.Delay((int)timer * 1000);
            DoExplosion();
        }

        protected void DoExplosion()
        {
            int size = Physics.OverlapSphereNonAlloc(this.transform.position, this.GetStat(Stat.aoeRange), buffer);

            if (size > 0) //no colliders hit so no AOE
                DoDamage(buffer);

            GameObject explosion = explosionPool.PullGameObject();
            explosion.transform.position = this.transform.position + Vector3.up * explosionOffset;
            explosion.transform.localScale = explosionScale * Vector3.one;
            this.gameObject.SetActive(false);
        }

        protected void DoDamage(Collider collider)
        {
            if (collider == null)
                return;

            if (collider.TryGetComponent<Unit>(out Unit unit))
                unit.DoDamage(this.GetStat(Stat.damage));
        }

        protected void DoDamage(Collider[] collidersHit)
        {
            if (collidersHit == null)
                return;

            foreach (var collider in collidersHit)
            {
                DoDamage(collider);
            }
        }
    }
}