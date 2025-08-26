using DG.DemiLib;
using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System.Collections;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PoolObject))]
public class Projectile : MonoBehaviour
{
    [SerializeField]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    protected ProjectileData _projectileData;
    public ProjectileData projectileData { get { return _projectileData; } }
    [SerializeField]
    protected LayerMask _collidesWith;
    public LayerMask collidesWith { get { return _collidesWith; } }

    [SerializeField]
    protected float selfDestructTime = 5f;
    private Unit target;
    private Transform targetTransform;
    private EnemySubUnit targetSubUnit;
    private float lastDistanceToTarget;
    private float damage;
    [SerializeField] private Hex3 startLocation;
    private float currentRange;

    [Header("Satellite Targeting")]
    [SerializeField] private bool useMaxHeight = true;
    [SerializeField] private GameObject groundImpact;
    private static ObjectPool<ImpactHole> groundImpactPool;
    [SerializeField] private GameObject waterImpact;
    private static ObjectPool<ImpactHole> waterImpactPool;

    private Camera mainCamera;

    private Vector3 targetLocation
    {
        get
        {
            if (targetSubUnit != null)
                return targetSubUnit.TargetPoint;
            else if(targetTransform != null)
                return targetTransform.position;
            else
                return target.transform.position;
        }
    }

    private Collider[] buffer = new Collider[30];


    protected void Awake()
    {
        if(groundImpact != null)
            groundImpactPool = new ObjectPool<ImpactHole>(groundImpact, 5);
        if (waterImpact != null)
            waterImpactPool = new ObjectPool<ImpactHole>(waterImpact, 5);

        if(mainCamera == null)
            mainCamera = Camera.main;
    }

    protected void OnEnable()
    {
        if (IsVisible(this.transform.position))
        {
            AudioSource audioSource = SFXManager.PlaceSFXAudioSource(this.transform.position);
            audioSource.priority = Random.Range(100, 200);
            _projectileData.launchSound.PlayClip(audioSource, true);
        }
    }



    protected void OnDisable()
    {
        StopAllCoroutines();
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (_collidesWith == (_collidesWith | 1 << other.gameObject.layer))
        {
            if(!InRange(other.transform))
                return;

            DoExplosion(other);
        }
    }

    //protected void OnCollisionEnter(Collision collision)
    //{
    //    if (_collidesWith == (_collidesWith | 1 << collision.gameObject.layer))
    //    {
    //        if (!InRange(collision.transform))
    //            return;

    //        DoExplosion(other);
    //    }
    //}

    private bool InRange(Transform target)
    {
        Hex3 hex3 = target.position.ToHex3();
        int distance = Hex3.DistanceBetween(startLocation, hex3);
        int maxRange = _projectileData.GetStatAsInt(Stat.maxRange);

        return distance <= currentRange;

        return Hex3.DistanceBetween(startLocation, target.position.ToHex3()) <= _projectileData.GetStat(Stat.maxRange);
    }

    protected void DoExplosion(Collider other)
    {
        if (_projectileData.GetStat(Stat.aoeRange) > 0.05f)
        {
            Vector3 position = this.transform.position;
            if(position.y < 0.1f)
                position.y = 0.1f;
            int size = Physics.OverlapSphereNonAlloc(position, _projectileData.GetStat(Stat.aoeRange), buffer, _collidesWith);

            if (size == 0) //no colliders hit so no AOE
                DoDamage(other);
            else
                DoDamage(buffer, size);
        }
        else if (other != null)
            DoDamage(other);

        bool isVisible = IsVisible(this.transform.position);

        //reduce the number of explosions for performance reasons
        if (Random.Range(0f, 1f) < _projectileData.ChanceForExplosion && isVisible)
        {
            GameObject explosion = _projectileData.GetExplosion();
            explosion.transform.position = this.transform.position + Vector3.up * _projectileData.explosionOffset;
            explosion.transform.localScale = _projectileData.explosionScale * Vector3.one;
        }

        if(TryGetImpactParticles(out GameObject particles) && isVisible)
        {
            //GameObject impact = Instantiate(impactObject, this.transform.position, Quaternion.Euler(90f,Random.Range(-180,180),0f));
            Vector3 position = particles.transform.position;
            position.y = 0.01f;

            particles.transform.position = position;
        }
        this.gameObject.SetActive(false);
    }

    private bool TryGetImpactParticles(out GameObject particles)
    {
        HexTile tile = HexTileManager.GetHexTileAtLocation(this.transform.position);
        if(tile == null ||(waterImpact == null && groundImpact == null))
        {
            particles = null;
            return false;
        }
        else if(tile.TileType == HexTileType.water && waterImpact != null)
        {
            particles = waterImpactPool.PullGameObject(this.transform.position, Quaternion.Euler(-90f, Random.Range(-180, 180), 0f));
            return true;
        }
        else if(groundImpact != null)
        {
            particles = groundImpactPool.PullGameObject(this.transform.position, Quaternion.Euler(90f, Random.Range(-180, 180), 0f));
            return true;
        }
        else
        {
            particles = null;
            return false;
        }
    }

    protected void DoDamage(Collider collider)
    {
        if (collider == null)
            return;
        
        if(this.gameObject.layer == 12 & collider.TryGetComponent(out PlayerUnit playerUnit)) //enemy projectile... look for player unit
        {
            playerUnit.DoDamage(damage);
            playerUnit.HitShield(this.transform);
        }
        else if (collider.TryGetComponent(out EnemySubUnit subUnit))
            subUnit.DoDamage(damage);
        else if(collider.transform.childCount > 0 && collider.transform.GetChild(0).TryGetComponent(out subUnit))
            subUnit.DoDamage(damage);
    }

    protected void DoDamage(Collider[] collidersHit, int size)
    {
        if (collidersHit == null)
            return;

        for (int i = 0; i < size; i++)
        {
            if (collidersHit[i] == null)
                continue;
            DoDamage(collidersHit[i]);
        }
    }

    private void FixedUpdate()
    {
        this.transform.position += this.transform.forward * _projectileData.GetStat(Stat.speed) * Time.fixedDeltaTime;

        if(_projectileData.seeksTarget)
            DoSeeking();

        if (this.transform.position.y < -0.1f)
            DoExplosion(null);
        else if(this.transform.position.y > 1.5f && useMaxHeight)
            this.gameObject.SetActive(false);
    }

    private void DoSeeking()
    {
        lastDistanceToTarget = (targetLocation - this.transform.position).magnitude;

        if (!target.gameObject.activeInHierarchy)
            return;

        if(lastDistanceToTarget > 0.25f)
            this.transform.LookAt(targetLocation);
    }

    protected IEnumerator SelfDestruct()
    {
        yield return new WaitForSeconds(selfDestructTime);
        if (_projectileData.seeksTarget && this.gameObject.activeSelf)
            DoExplosion(null);
        else
            this.gameObject.SetActive(false);
    }

    public void SetTarget(Unit target, Transform targetPoint)
    {
        this.target = target;
        this.targetTransform = targetPoint;
        //targetPoint should be the subunit transform
        this.targetSubUnit = targetPoint.GetComponent<EnemySubUnit>();
        
        lastDistanceToTarget = (targetLocation - this.transform.position).sqrMagnitude;
    }

    public void SetStats(float range, float damage)
    {
        currentRange = range;
        startLocation = this.transform.position.ToHex3();
        selfDestructTime = 2f * range / _projectileData.GetStat(Stat.speed);
        StartCoroutine(SelfDestruct());

        this.damage = damage;
    }

    public void SetDamage(float damage)
    {
        this.damage = damage;
    }

    public void SetStartPosition(Vector3 position)
    {
        startLocation = position.ToHex3();
        this.transform.position = position;
    }

    private bool IsVisible(Vector3 position)
    {
        Vector3 screenPoint = mainCamera.WorldToViewportPoint(position);

        if (screenPoint.z <= 0)
            return false;
        if (screenPoint.x > 1 || screenPoint.x < 0)
            return false;
        if (screenPoint.y > 1 || screenPoint.y < 0)
            return false;

        return true;
    }
}
