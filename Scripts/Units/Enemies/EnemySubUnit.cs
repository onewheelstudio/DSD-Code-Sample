using DG.Tweening;
using GPUInstancer.CrowdAnimations;
using HexGame.Units;
using System;
using System.Collections;
using UnityEngine;

public class EnemySubUnit : MonoBehaviour
{

    [SerializeField] private EnemyUnit parentUnit;
    [SerializeField] private FollowParent followParent;
    [SerializeField] private Vector3 targetPoint;
    public Vector3 TargetPoint => targetPoint + this.transform.position;
    [SerializeField] private Vector3 launchPoint;
    public Vector3 LaunchPoint => launchPoint + this.transform.position;
    private SphereCollider sphereCollider;

    [Header("Animations")]
    private GPUICrowdPrefab GPUICrowd;
    [SerializeField]
    private AnimationClip moveAnimation;
    [SerializeField]
    private AnimationClip deathAnimation;
    [SerializeField] private float deathDelay = 1f;
    private bool isDead = false;
    public bool IsDead => isDead;

    public static event Action<EnemyUnitType> subUnitDied;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(TargetPoint, 0.05f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(LaunchPoint, 0.05f);
    }

    private void Awake()
    {
        if (targetPoint == null)
            targetPoint = Vector3.zero;
        this.GPUICrowd = this.gameObject.GetComponentInChildren<GPUICrowdPrefab>();
        this.sphereCollider = this.GetComponent<SphereCollider>();

        if(!StateOfTheGame.gameStarted)
            this.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        DOTween.Kill(this,true);
    }

    public void DoDamage(float damage)
    {
        parentUnit.DoDamage(damage, this);
        followParent?.SubUnitHit(this);
    }

    public void TurnOn()
    {
        if (this.gameObject.activeInHierarchy)
            return;

        StopAllCoroutines();
        this.sphereCollider.enabled = true;
        this.gameObject.SetActive(true);
        isDead = false;
        PlayMove();
    }

    public void TurnOff()
    {
        if (!this.gameObject.activeInHierarchy)
            return;

        this.sphereCollider.enabled = false;
        isDead = true;
        subUnitDied?.Invoke(parentUnit.type);
        this.transform.position -= Vector3.up * 10f;
        this.gameObject.SetActive(false);
        SFXManager.PlaySFX(SFXType.enemyDeath);
        //PlayDeath();
        //StartCoroutine(DelayTurnOff());
    }

    private void PlayMove()
    {
        if (GPUICrowd == null || moveAnimation == null)
            return;

        GPUICrowdAPI.StartAnimation(GPUICrowd, moveAnimation);
    }

    private void PlayDeath()
    {
        if (GPUICrowd == null || deathAnimation == null)
            return;
        GPUICrowdAPI.StartAnimation(GPUICrowd, deathAnimation);
    }

    IEnumerator DelayTurnOff()
    {
        yield return new WaitForSeconds(12f);
        yield return new WaitForSeconds(6f);
        this.gameObject.SetActive(false);
    }

}
