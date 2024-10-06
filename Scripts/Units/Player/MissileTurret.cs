using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using HexGame.Resources;
using Sirenix.Utilities;
using System.Linq;

namespace HexGame.Units
{
    public class MissileTurret : MonoBehaviour
    {

        [SerializeField]
        private Unit _target;
        public Unit target
        {
            get
            {
                return _target;
            }
        }
        [SerializeField]
        private bool canFire = true;
        [SerializeField]
        private float reloadTime = 0.25f;
        [SerializeField]
        private ProjectileData projectileData;
        Tween lookAtTween;
        [SerializeField]
        [Range(0.01f, 0.5f)]
        private float rotateSpeed = 0.25f;
        [Range(0.01f, 5f)]
        [SerializeField]
        private float launchDelay = 1f;

        [SerializeField] private Transform turretArm;

        [SerializeField]
        private Transform[] launchPoints;
        [SerializeField] private GameObject[] missiles;
        private UnitStorageBehavior storageBehavior;
        private Coroutine randomRotate;
        private Unit unit;
        private float range => unit.GetStat(Stat.maxRange);
        private float damage => unit.GetStat(Stat.damage);

        private void Start()
        {
            storageBehavior = this.GetComponentInParent<UnitStorageBehavior>();
            unit = this.GetComponentInParent<Unit>();
            missiles.ForEach(m => m.SetActive(false));
            RandomRotate();
        }

        private void OnEnable()
        {
            StartCoroutine(DoRandomRotate());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            DOTween.Kill(this,true);
        }

        public void SetTarget(Unit target)
        {
            if (target != null)
            {
                lookAtTween.Kill();
                this._target = target;
                Vector3 lookTarget =_target.transform.position + (_target.transform.position - this.transform.position).normalized;
                lookTarget.y = this.transform.position.y;

                lookAtTween = this.transform.DOLookAt(lookTarget,
                                                        rotateSpeed,
                                                        AxisConstraint.None,
                                                        Vector3.up);
            }
        }

        public bool NeedsTarget()
        {
            return target == null;
        }

        private void Update()
        {
            if (this.target != null && !this.target.gameObject.activeSelf)
                this._target = null;

            if (canFire)
            {
                foreach (var missile in missiles)
                {
                    if (!missile.activeSelf && storageBehavior.TryUseAllResources(projectileData.projectileCost))
                        missile.SetActive(true);
                }
                StartCoroutine(FireEachBarrel(target, range));
            }
        }


        IEnumerator FireEachBarrel(Unit target, float range)
        {
            if (!canFire || target == null)
                yield break;

            if(storageBehavior.efficiency <= 0.01f)
            {
                //MessagePanel.ShowMessage("No workers.", this.gameObject);
                yield break;
            }    

            canFire = false;
            for (int i = 0; i < missiles.Length; i++)
            {
                if (target == null)
                    break;

                if (!missiles[i].activeSelf)
                    continue;

                missiles[i].SetActive(false);
                Projectile projectile = projectileData.GetProjectile().GetComponent<Projectile>();
                projectile.SetStats(range, damage);
                projectile.gameObject.SetActive(true);
                projectile.SetStartPosition(launchPoints[i].position);
                projectile.transform.rotation = launchPoints[i].rotation;

                Transform targetPoint = target.GetTarget();
                projectile.GetComponent<Projectile>().SetTarget(target,targetPoint);

                yield return new WaitForSeconds(launchDelay);
            }

            StartCoroutine(ReloadTimer());
        }

        IEnumerator ReloadTimer()
        {
            if (canFire)
                yield break;
            else
            {
                yield return new WaitForSeconds(projectileData.GetStat(Stat.reloadTime));
                yield return null; //wait one extra frame avoid errors is reload time is zero.
                canFire = true;
            }
        }

        IEnumerator DoRandomRotate()
        {
            while(true)
            {
                yield return new WaitForSeconds(HexTileManager.GetNextInt(1, 5));
                if(this.target == null)
                    RandomRotate();
            }
        }

        private void RandomRotate()
        {
            float amount = HexTileManager.GetNextInt(-60, 60);
            lookAtTween = this.transform.DORotate(new Vector3(0f, amount, 0f) + this.transform.rotation.eulerAngles, 0.5f);
        }
    }

}