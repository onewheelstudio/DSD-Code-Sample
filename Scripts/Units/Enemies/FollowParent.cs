using HexGame.Units;
using Pathfinding;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowParent : UnitBehavior
{
    [SerializeField] private float verticalOffset = 0.2f;
    [SerializeField] private List<FollowerUnit> followerUnits = new List<FollowerUnit>();

    [SerializeField] private Transform parent;

    //[SerializeField] private Transform movingFollowTarget;
    [InfoBox("These values get set at runtime")]
    private float followDistance;
    private float followSpeed;
    [SerializeField] private float lerpSpeed = 0.1f;
    private Vector3 offset;
    private Vector3 velocity;
    private AIPath aiPath;
    [SerializeField] private float delay = 0.5f;
    private WaitForSeconds stunDelay = new WaitForSeconds(0.5f);

    private void Awake()
    {
        lerpSpeed *= UnityEngine.Random.Range(0.7f, 1f);
        followDistance = UnityEngine.Random.Range(0.2f, 0.3f);
        followSpeed = GetStat(Stat.speed);
        aiPath = this.GetComponent<AIPath>();

        foreach (var follower in followerUnits)
        {
            follower.followerUnit.SetParent(null);
            follower.followerUnit.GetComponent<FogUnit>().isHidden += SubUnitIsHidden;
        }
        stunDelay = new WaitForSeconds(delay);
    }

    private void OnValidate()
    {
        if(followerUnits.Count == 0)
            return;

        for (int i = 0; i < followerUnits.Count; i++)
        {
            if (followerUnits[i].followerUnit == null)
                continue;

            if(i == 0)
                followerUnits[i].followTarget = this.transform;
            else
                followerUnits[i].followTarget = followerUnits[i - 1].followerUnit;

            if (Application.isPlaying)
                continue;

            followerUnits[i].isEvenChild = i % 2 == 0;
            followerUnits[i].localOffset = followerUnits[i].followerUnit.localPosition + Vector3.up * verticalOffset;
            followerUnits[i].followOffset = new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 0, UnityEngine.Random.Range(-0.2f, 0.2f));
            followerUnits[i].lerpSpeed = this.lerpSpeed * UnityEngine.Random.Range(0.9f, 1.1f);
        }
    }

    public override void StartBehavior()
    {
        _isFunctional = true;

        foreach (var follower in followerUnits)
        {
            follower.followerUnit.position = this.transform.position + follower.localOffset + Vector3.up * verticalOffset;
            follower.isStunned = false;
        }
    }

    public override void StopBehavior()
    {
        _isFunctional = false;
        StopAllCoroutines();
    }

    private void Update()
    {
        if (!_isFunctional)
            return;

        foreach (var follower in followerUnits)
        {
            MoveToTarget(follower);
        }
    }

    private void MoveToTarget(FollowerUnit follower)
    {
        if(follower.isStunned)
        {
            follower.isStunned = false;
            return;
        }

        float distance = (follower.followerUnit.position - follower.followTarget.position).sqrMagnitude;

        //grouping
        if (aiPath.remainingDistance < aiPath.slowdownDistance)
        {
            follower.followerUnit.position = Vector3.SmoothDamp(follower.followerUnit.position, parent.position + follower.localOffset, ref velocity, follower.lerpSpeed, followSpeed);
            follower.followerUnit.rotation = Quaternion.Lerp(follower.followerUnit.rotation, parent.rotation, follower.lerpSpeed);
        }
        //following
        else// if (distance > followDistance)
        {
            offset = follower.isEvenChild ? follower.followTarget.right * followDistance : -follower.followTarget.right * followDistance;
            offset += follower.followOffset;

            float _lerpSpeed = Mathf.Lerp(0, follower.lerpSpeed, distance / followDistance);

            follower.followerUnit.position = Vector3.Lerp(follower.followerUnit.position, follower.followTarget.position + offset, _lerpSpeed);
            //follower.followerUnit.position = Vector3.SmoothDamp(follower.followerUnit.position, follower.followTarget.position + offset, ref velocity, follower.lerpSpeed, followSpeed);
            follower.followerUnit.rotation = Quaternion.Lerp(follower.followerUnit.rotation, follower.followTarget.rotation, follower.lerpSpeed);
        }

        Vector3 position = follower.followerUnit.position;
        position.y = verticalOffset;
        follower.followerUnit.position = position;
    }

    private void SubUnitIsHidden(bool isHidden)
    {
        if (isHidden)
        {
            followSpeed = GetStat(Stat.speed) * 2f;
            aiPath.maxSpeed = GetStat(Stat.speed) * 2f;
        }
        else
        {
            followSpeed = GetStat(Stat.speed);
            aiPath.maxSpeed = GetStat(Stat.speed);
        }
    }

    public List<EnemySubUnit> GetSubUnits()
    {
        List<EnemySubUnit > units = new List<EnemySubUnit>();
        foreach (var follower in followerUnits)
        {
            units.Add(follower.followerUnit.GetComponent<EnemySubUnit>());
        }

        return units;
    }

    internal void SubUnitHit(EnemySubUnit enemySubUnit)
    {
        foreach (var follower in followerUnits)
        {
            if (follower.followerUnit == enemySubUnit.transform)
            {
                follower.isStunned = true;
                //StartCoroutine(UnStunFollow(follower));
                break;
            }
        }
    }

    private IEnumerator UnStunFollow(FollowerUnit follower)
    {
        yield return stunDelay;
        follower.isStunned = false;
    }
}

[System.Serializable]
public class FollowerUnit
{
    public Transform followerUnit;
    public Transform followTarget;
    public bool isEvenChild;
    public Vector3 localOffset;
    public Vector3 followOffset;
    public float lerpSpeed;
    public bool isStunned = false;
}
