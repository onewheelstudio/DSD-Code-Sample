using HexGame.Grid;
using HexGame.Units;
using OWS.ObjectPooling;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootLightingBehavior : UnitBehavior
{

    private UnitDetection unitDetection;
    [SerializeField]
    private Transform launchPoint;
    private bool canFire = true;
    [SerializeField] private float reloadDelay = 0.5f;
    [SerializeField] private GameObject lightingStrike;
    private static ObjectPool<Lighting> lightingStrikePool;
    private WaitForSeconds _reloadDelay;
    [SerializeField] private bool useChainedEffect = false;

    private void Awake()
    {
        if (unitDetection == null)
            unitDetection = this.GetComponentInChildren<UnitDetection>();

        if (lightingStrikePool == null)
            lightingStrikePool = new ObjectPool<Lighting>(lightingStrike);

        _reloadDelay = new WaitForSeconds(reloadDelay);
    }

    public override void StartBehavior()
    {
        isFunctional = true;
    }

    public override void StopBehavior()
    {
        isFunctional = false;
    }

    private void Update()
    {
        if (!_isFunctional)
            return;

        if (canFire && unitDetection.GetTargetList().Count > 0)
            StartCoroutine(Shoot());
    }

    protected IEnumerator Shoot()
    {
        if (!canFire)
            yield break;

        canFire = false;

        List<Unit> targets = unitDetection.GetTargetList();
        if (useChainedEffect)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                if (i == 0)
                {
                    yield return lightingStrikePool.Pull().AnimateLighting(launchPoint.position, targets[i].transform.position + Vector3.up * 0.2f);
                    //do damage
                }
                else
                {
                    yield return lightingStrikePool.Pull().AnimateLighting(targets[i - 1].transform.position + Vector3.up * 0.2f, targets[i].transform.position + Vector3.up * 0.2f);
                    //do damage
                }
                DoDamage(targets[i]);
            }
        }
        else
        {
            foreach (Unit target in targets)
            {
                if (HelperFunctions.HexRangeFloat(target.transform.position, this.transform.position) > GetStat(Stat.maxRange))
                    continue;

                yield return lightingStrikePool.Pull().AnimateLighting(launchPoint.position, target.transform.position + Vector3.up * 0.2f);
                DoDamage(target);
            }
        }

        yield return _reloadDelay;
        canFire = true;
    }

    private void DoDamage(Unit target)
    {
        //do damage
        float damage = this.GetStat(Stat.damage);
        target.DoDamage(damage);
    }
}

