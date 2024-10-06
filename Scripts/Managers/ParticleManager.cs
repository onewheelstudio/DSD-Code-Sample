using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{

    [SerializeField, AssetsOnly] private GameObject shieldRecharge;
    private ParticlePool shieldRechargePool;

    private void Awake()
    {
        shieldRechargePool = new ParticlePool(shieldRecharge);
    }

    public GameObject GetRechargeParticles(Vector3 position)
    {
        GameObject particles = shieldRechargePool.GetParticles();
        particles.transform.position = position;
        return particles;
    }

    [System.Serializable]
    public class ParticlePool
    {
        public GameObject particles;
        public ObjectPool<PoolObject> particlePool;

        public ParticlePool(GameObject particles)
        {
            this.particles = particles;
            this.particlePool = new ObjectPool<PoolObject>(particles, 5);
        }

        public GameObject GetParticles()
        {
            return particlePool.PullGameObject();   
        }
    }
}
