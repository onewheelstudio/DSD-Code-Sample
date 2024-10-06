using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Units;
using Sirenix.OdinInspector;
using DG.Tweening;
using System;
using Random = UnityEngine.Random;
using OWS.ObjectPooling;

public class Marine : MonoBehaviour
{
    private Transform parent;
    private HoverMoveBehavior hmb;
    private Animator animator;
    private Vector3 offset;
    [SerializeField] private ParticleSystem jetpack;
    [SerializeField] private ProjectileData projectileData;
    [SerializeField] private Transform launchLocation;
    private bool canFire = true;
    private WaitForSeconds startDelay;
    public bool isMoving { get; private set; }
    public event Action reachedDestination;
    protected static ObjectPool<PoolObject> dustPool;
    [SerializeField] private GameObject dust;

    private void Awake()
    {
        this.parent = this.transform.parent;
        hmb = this.GetComponent<HoverMoveBehavior>();
        animator = this.GetComponent<Animator>();
        startDelay = new WaitForSeconds(Random.Range(0.1f,0.25f));
        isMoving = false;
        if (dustPool == null && dust != null)
            dustPool = new ObjectPool<PoolObject>(dust);

    }

    private void OnEnable()
    {
        hmb.reachedDestination += FinishMovement;
        DayNightManager.toggleDay += SetIdle;
        DayNightManager.toggleNight += SetReady;
    }

    private void SetReady(int obj)
    {
        animator.SetTrigger("Ready");

    }

    private void SetIdle(int obj)
    {
        animator.SetTrigger("Idle");
    }

    private void OnDisable()
    {
        hmb.reachedDestination -= FinishMovement;
        DOTween.Kill(this,true);
    }

    public void SetDestination(Vector3 position)
    {
        offset = this.transform.position - (this.transform.position.ToHex3()).ToVector3();
        this.transform.SetParent(null);
        StartCoroutine(DelayMove(position + offset));
        isMoving = true;
    }

    private IEnumerator DelayMove(Vector3 position)
    {
        float delay = Random.Range(0f, 0.5f);
        yield return new WaitForSeconds(delay);
        jetpack.Play();
        animator.SetTrigger("TakeOff");
        yield return new WaitForSeconds(0.2f); //6 frames of the jump animation
        dustPool.Pull(this.transform.position);
        hmb.SetDestination(position, true);

        yield return new WaitUntil(() => this.transform.position.y > 0.8f);
        yield return new WaitUntil(() => this.transform.position.y < 0.45f);
        animator.SetTrigger("Land");
        yield return new WaitForSeconds(0.87f); //26 frames of the jump animation
        dustPool.Pull(this.transform.position);
        jetpack.Stop();
    }

    private void FinishMovement()
    {
        reachedDestination?.Invoke();
        this.transform.SetParent(parent);
        isMoving = false;
    }

    public void SetJetPack(bool isOn)
    {
        if (isOn)
            jetpack.Play();
        else
            jetpack.Stop();
    }

    private IEnumerator Shoot(EnemyUnit target, float reloadTime, float range, float damage, Func<bool> targetIsValid)
    {
        if (!targetIsValid.Invoke())
            yield break;

        canFire = false;
        Tween lookAt = this.transform.DOLookAt(target.transform.position, 0.25f, AxisConstraint.None, Vector3.up);
        yield return lookAt.IsComplete();

        if (launchLocation == null || projectileData == null)
        {
            canFire = true;
            yield break;
        }

        float delay = Random.Range(0f, 0.25f); //create small randomness in the firing time
        yield return new WaitForSeconds(delay);

        Projectile projectile = projectileData.GetProjectile().GetComponent<Projectile>();

        Transform targetPoint = target.GetTarget();
        if (projectileData.seeksTarget)
            projectile.GetComponent<Projectile>().SetTarget(target,targetPoint);

        Vector3 targetLocation = targetPoint.position;
        if(targetPoint.TryGetComponent(out EnemySubUnit subUnit))
            targetLocation = subUnit.LaunchPoint;

        projectile.GetComponent<Projectile>().SetStats(range, damage);
        projectile.SetStartPosition(launchLocation.position);
        projectile.transform.LookAt(targetLocation, Vector3.up); //aims the projectile
        animator.SetTrigger("Shoot");

        yield return new WaitForSeconds(reloadTime);
        canFire = true;
    }

    public void SetTarget(EnemyUnit target, float reloadTime, float range, float damage, Func<bool> targetIsValid)
    {
        if (canFire && target != null && target.gameObject.activeInHierarchy)
            StartCoroutine(Shoot(target, reloadTime, range, damage, targetIsValid));
    }

    internal void DoFlyDown(float startHeight)
    {
        isMoving = true;
        float endHeight = this.transform.position.y;
        float time = (startHeight - endHeight) / 2.5f;
        this.transform.position = new Vector3(this.transform.position.x, startHeight, this.transform.position.z);
        this.transform.DOMoveY(endHeight, time).SetEase(Ease.OutExpo).OnComplete(FinishFlyDown);
    }

    private void FinishFlyDown()
    {
        SetJetPack(false);
        this.isMoving = false;
    }
}
