using DG.Tweening;
using System.Collections;
using UnityEngine;

namespace HexGame.Units
{
    public class Turret : MonoBehaviour
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
        [Range(0.01f, 0.5f)]
        [SerializeField]
        private float launchDelay = 0.1f;

        [SerializeField]
        private GameObject[] barrels;
        private Transform[] barrelLaunchPoints;
        private ParticleSystem[] muzzleFlashes;
        private UnitStorageBehavior storageBehavior;
        private Coroutine randomRotate;
        private Unit unit;
        private float range => unit.GetStat(Stat.maxRange); 
        private float damage => unit.GetStat(Stat.damage);

        private void Start()
        {
            muzzleFlashes = GetComponentsInChildren<ParticleSystem>();
            storageBehavior = this.GetComponentInParent<UnitStorageBehavior>();
            unit = this.GetComponentInParent<Unit>();
            GetBarrelLaunchPoints();
            RandomRotate();
        }

        private void OnEnable()
        {
            DoRandomRotate();
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

            if (canFire && DayNightManager.isNight)
                FireEachBarrel(target);
        }


        private async void FireEachBarrel(Unit target)
        {
            if (!canFire || target == null)
                return;

            if(storageBehavior.efficiency <= 0.01f)
            {
                //MessagePanel.ShowMessage("No workers.", this.gameObject);
                return;
            }    

            canFire = false;
            for (int i = 0; i < barrels.Length; i++)
            {
                if (target == null)
                    break;

                if (!storageBehavior.TryUseAllResources(projectileData.projectileCost))
                    continue;

                Projectile projectile = projectileData.GetProjectile().GetComponent<Projectile>();
                Transform targetPoint = target.GetTarget();
                if(projectileData.seeksTarget)
                    projectile.SetTarget(target,targetPoint);

                Vector3 targetLocation = targetPoint.position;
                if (targetPoint.TryGetComponent(out EnemySubUnit subUnit))
                    targetLocation = subUnit.LaunchPoint;

                projectile.SetStats(range, damage);
                projectile.SetStartPosition(barrelLaunchPoints[i].position);
                projectile.transform.LookAt(targetLocation, Vector3.up); //aims the projectile

                float startX = barrels[i].transform.localPosition.x;

                var sequence = DOTween.Sequence();
                sequence.Append(barrels[i].transform.DOLocalMoveX(startX - 0.00055f, 0.075f).SetEase(Ease.InOutElastic));
                sequence.Append(barrels[i].transform.DOLocalMoveX(startX, 0.2f, true).SetEase(Ease.Linear));
             
                await sequence.AsyncWaitForCompletion();
            }

            DoMuzzleFlash();
            ReloadTimer();
        }

        private async void ReloadTimer()
        {
            if (canFire)
                return;
            else
            {
                await Awaitable.WaitForSecondsAsync(unit.GetStat(Stat.reloadTime), this.destroyCancellationToken);
                await Awaitable.NextFrameAsync(this.destroyCancellationToken);//wait one extra frame avoid errors is reload time is zero.
                canFire = true;
            }
        }

        private void DoMuzzleFlash()
        {
            foreach (var ps in muzzleFlashes)
                ps.Play();
        }

        private void GetBarrelLaunchPoints()
        {
            barrelLaunchPoints = new Transform[barrels.Length];
            for (int i = 0; i < barrels.Length; i++)
                barrelLaunchPoints[i] = barrels[i].transform.GetChild(0);
        }

        private async void DoRandomRotate()
        {
            while(true && !this.destroyCancellationToken.IsCancellationRequested)
            {
                await Awaitable.WaitForSecondsAsync(HexTileManager.GetNextInt(1, 5), this.destroyCancellationToken);
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