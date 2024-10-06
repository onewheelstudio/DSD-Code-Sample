using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace HexGame.Units
{
    public class BombRunBehavior : UnitBehavior, IHaveTarget
    {
        [SerializeField]
        [Range(50, 150)]
        private float turnSpeed;
        [SerializeField]
        [Range(2, 20)]
        private float turnRange = 5f;
        [SerializeField]
        [Range(0f, 10f)]
        private float turnAccuracy = 2f;

        [SerializeField]
        private Transform target;
        private float distance;
        private Vector3 direction;
        private float angle;
        private bool isTurning = false;

        private void OnEnable()
        {
            turnRange *= Random.Range(0.75f, 1.25f);
            turnSpeed *= Random.Range(0.8f, 1.2f);
        }

        private void OnDisable()
        {
            DOTween.Kill(this,true);
        }

        // Update is called once per frame
        void Update()
        {
            //if (!isStarted || target == null)
            //    return;

            this.transform.position += this.transform.forward * GetStat(Stat.speed) * Time.deltaTime;

            direction = target.position - this.transform.position;
            direction.y = 0;
            distance = direction.magnitude;
            angle = Vector3.SignedAngle(this.transform.forward, direction, Vector3.up);

            if (!isTurning && Mathf.Abs(angle) > turnAccuracy && distance > turnRange)
                StartCoroutine(DoTurn());
        }

        private IEnumerator DoTurn()
        {
            float accuracy = Random.Range(1f, turnAccuracy);

            while (Mathf.Abs(angle) > accuracy && distance > 0.25f * turnRange)
            {
                isTurning = true;
                this.transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime);
                yield return null;
            }
            isTurning = false;
        }

        public void SetTarget(Unit target)
        {
            this.target = target.transform;
        }

        public override void StartBehavior()
        {
            _isFunctional = true;
        }

        public override void StopBehavior()
        {
            _isFunctional = false;
        }
    }
}