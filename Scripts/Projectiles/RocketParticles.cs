using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OWS.ObjectPooling;

public class RocketParticles : MonoBehaviour
{
    [SerializeField] private GameObject particles;
    private static ObjectPool<PoolObject> particlePool;
    private Transform childParticles;

    private void Awake()
    {
        particlePool = new ObjectPool<PoolObject>(particles);
    }

    private void OnEnable()
    {
        PoolObject po = particlePool.Pull();
        po.transform.SetParent(this.transform);
        po.transform.position = this.transform.position;
        childParticles = po.transform;
    }

    private void OnDisable()
    {
        Debug.Log("Disabled");
        childParticles.SetParent(null);
    }

}
