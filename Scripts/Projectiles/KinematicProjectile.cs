using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OWS.ObjectPooling;
using System;

public class KinematicProjectile : Projectile, IPoolable<KinematicProjectile>
{
    Action<KinematicProjectile> returnAction;
    private Vector3 velocity;
    private new void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(SelfDestruct());
    }

    private new void OnDisable()
    {
        base.OnDisable();
        ReturnToPool();
    }

    public void Initialize(Action<KinematicProjectile> returnAction)
    {
        this.returnAction = returnAction;
    }

    public void ReturnToPool()
    {
        returnAction?.Invoke(this); 
    }

    public void Shoot(Vector3 startLocation, Vector3 velocity)
    {
        this.velocity = velocity.normalized * _projectileData.GetStat(Stat.speed);
        this.transform.position = startLocation;
    }

    private void FixedUpdate()
    {
        this.transform.position += velocity * Time.deltaTime;
        velocity.y += Physics.gravity.y * Time.deltaTime;
    }

}
