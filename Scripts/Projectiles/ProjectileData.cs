using UnityEngine;
using System.Collections.Generic;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using HexGame.Resources;

[CreateAssetMenu(fileName = "new projectile data", menuName ="Projectile Data")]
[ManageableData]
public class ProjectileData : Stats
{
    [Required]
    public GameObject explosionPrefab;
    public float explosionScale = 1f;
    public float explosionOffset = 0f;
    public GameObject projectilePrefab;
    [SerializeField, Range(0f,1f)] private float chanceForExplosion = 0.25f;
    public float ChanceForExplosion => chanceForExplosion;
    private ObjectPool<PoolObject> explosionPool;
    private ObjectPool<PoolObject> projectilePool;
    public List<ResourceAmount> projectileCost = new List<ResourceAmount>();
    public bool seeksTarget = false;
    [SerializeField]
    public SFXManager.SFX launchSound;

    public GameObject GetExplosion()
    {
        if(explosionPrefab == null)
            return null;

        if (explosionPool == null)
            explosionPool = new ObjectPool<PoolObject>(explosionPrefab);

        return explosionPool.PullGameObject();
    }

    public GameObject GetProjectile()
    {
        if (projectilePool == null)
            projectilePool = new ObjectPool<PoolObject>(projectilePrefab);

        return projectilePool.PullGameObject();
    }

    public List<ResourceAmount> GetProjectileCost()
    {
        return projectileCost;
    }

}
