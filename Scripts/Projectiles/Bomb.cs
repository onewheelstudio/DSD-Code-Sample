using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Units;
using OWS.ObjectPooling;

/// <summary>
/// Used for "projectiles" that are not self guiding
/// </summary>
public class Bomb : MonoBehaviour
{
    private ProjectileData projectileData;
    private LayerMask collidesWith;
    [SerializeField] private bool destroyTiles = false;
    [SerializeField] private GameObject falloutPrefab;
    private static ObjectPool<PoolObject> falloutPool;

    private void Awake()
    {
        if (falloutPool == null && falloutPrefab != null)
            falloutPool = new ObjectPool<PoolObject>(falloutPrefab);
    }


    private void OnEnable()
    {
        //get values from parent projectile component
        projectileData = GetProjectileInParent(this.transform.parent).projectileData;
        collidesWith = GetProjectileInParent(this.transform.parent).collidesWith;
    }

    private Projectile GetProjectileInParent(Transform parent)
    {
        if (parent.TryGetComponent(out Projectile projectile))
            return projectile;
        else if (parent.parent != null)
            return GetProjectileInParent(parent.parent);
        else
            return null;
    }

    private void Update()
    {
        if (this.transform.position.y > 5f)
            return;

        Ray ray = new Ray(this.transform.position, this.transform.forward);
        if (Physics.Raycast(ray, 1.1f, collidesWith))
            DoExplosion();
    }

    protected void DoExplosion()
    {
        if (projectileData.GetStat(Stat.aoeRange) > 0.01f)
        {
            Collider[] colliders = Physics.OverlapSphere(this.transform.position, projectileData.GetStat(Stat.aoeRange));
            DoDamage(colliders);
        }
        else
            Debug.LogWarning($"AOE range of {this.gameObject.name} is set to 0");

        GameObject explosion = projectileData.GetExplosion();
        explosion.transform.position = this.transform.position;
        explosion.transform.localScale = projectileData.explosionScale * Vector3.one;
    }
    protected void DoDamage(Collider[] collidersHit)
    {
        Bounds b = collidersHit[0].bounds;
        foreach (Collider collider in collidersHit)
        {
            b.Encapsulate(collider.bounds);
        }

        foreach (var objectHit in collidersHit)
        {
            if (objectHit.TryGetComponent<Unit>(out Unit unit))
                unit.DoDamage(projectileData.GetStat(Stat.damage));
            else if (destroyTiles 
                    && objectHit.gameObject.activeInHierarchy
                    && objectHit.transform.parent != null 
                    && objectHit.transform.parent.TryGetComponent(out HexGame.Resources.HexTile tile))
            {
                if(falloutPrefab != null && HexTileManager.GetNextInt(0,100) > 40)
                    falloutPool.Pull(tile.transform.position + Vector3.up * 0.5f);
                FindObjectOfType<HexTileManager>().NukeTile(tile);
            }
        }

        var gou = new Pathfinding.GraphUpdateObject(b);
        gou.updatePhysics = true;
        AstarPath.active.UpdateGraphs(gou);
    }
}
