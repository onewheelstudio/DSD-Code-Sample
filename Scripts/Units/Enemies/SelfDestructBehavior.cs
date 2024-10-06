using HexGame.Units;
using Sirenix.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SelfDestructBehavior : UnitBehavior
{
    private Unit target;
    private SetDestination setDestination;
    private UnitDetection unitDetection;
    [SerializeField]
    private LayerMask collisionMask;

    private void OnEnable()
    {
        if (setDestination == null)
            setDestination = this.GetComponent<SetDestination>();
        if (unitDetection == null)
            unitDetection = this.GetComponentInChildren<UnitDetection>();
    }

    public override void StartBehavior()
    {
        target = null;
        _isFunctional = true;
    }

    public override void StopBehavior()
    {
        _isFunctional = false;
    }

    private void OnDisable()
    {
        GoBoom();
    }

    private void Update()
    {
        if (target == null)
        {
            target = EnemyTargeting.GetHighestValueTarget(this.transform.position);
            setDestination.SetTarget(target.transform);
        }

        if (unitDetection.HasTargetInRange())
            this.gameObject.SetActive(false);
    }

    private void GoBoom()
    {
        Collider[] colliders = Physics.OverlapSphere(this.transform.position, GetStat(Stat.maxRange), collisionMask);

        if(colliders.Length > 0)
        {
            colliders.ForEach(c => c.GetComponent<Unit>()?.DoDamage(GetStat(Stat.damage)));
            Debug.Log($"Doing damage to {colliders[0].name}");
        }
        
        this.gameObject.SetActive(false);
    }
}
