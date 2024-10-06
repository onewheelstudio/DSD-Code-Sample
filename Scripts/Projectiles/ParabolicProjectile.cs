using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Units;
using Sirenix.Utilities;

public class ParabolicProjectile : Projectile
{
    [SerializeField] private float travelDistance = 40f;
    [SerializeField] private Transform missileVisuals;
    [SerializeField] private float spinRate = 1f;
    private float maxHeight;
    private float time = 0f;
    private float distance;
    private Vector3 lastPosition;
    private float speed;
    private bool launch = false;
    [SerializeField] private ParticleSystem[] particles;

    //parabola coefficients
    private float a;
    private float b;

    private new void OnEnable()
    {
        base.OnEnable();
        time = 0f;
        ResetMissile();
    }



    private new void OnDisable()
    {
        base.OnDisable();
        launch = false;
    }

    protected new void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggering");

        if (_collidesWith == (_collidesWith | 1 << other.gameObject.layer))
        {
            DoExplosion(other);
        }
    }

    private void ResetMissile()
    {
        particles.ForEach(x => x.Stop());
        missileVisuals.transform.localPosition = Vector3.zero;
        missileVisuals.transform.LookAt(this.transform.position + Vector3.up);
    }

    public void SetTarget(Vector3 target)
    {
        this.transform.LookAt(target, Vector3.up);
        distance = (target - this.transform.position).magnitude;

        //using approximate constant travel distance
        //go higher if target is close and vice versa
        maxHeight = Mathf.Sqrt((travelDistance / 2f) * (travelDistance / 2f) - (distance / 2f) * (distance / 2f));
        a = -maxHeight * 4 / (distance * distance);
        b = maxHeight * 4 / distance;
        missileVisuals.transform.LookAt(missileVisuals.transform.position + Vector3.up);
        speed = 0.02f;
        launch = true;
        particles.ForEach(x => x.Play());
    }

    void FixedUpdate()
    {
        if (!launch)
            return;

        missileVisuals.transform.Rotate(missileVisuals.forward, spinRate * Time.fixedDeltaTime);

        lastPosition = missileVisuals.transform.position;

        if (speed > _projectileData.GetStat(Stat.speed))
            speed = _projectileData.GetStat(Stat.speed);
        else
            speed += _projectileData.GetStat(Stat.speed) * Time.fixedDeltaTime; //turns stat speed into acceleration

        time += Time.fixedDeltaTime * speed;
        missileVisuals.transform.localPosition = CalculateLocalPosition(time);
        missileVisuals.transform.LookAt(missileVisuals.transform.position + (missileVisuals.transform.position - lastPosition), Vector3.up);

        if (missileVisuals.transform.position.y < -0.45f)
            this.gameObject.SetActive(false);
    }

    private Vector3 CalculateLocalPosition(float time)
    {
        return new Vector3(0f, a * time * time + b * time, time);
    }
}
