using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using Sirenix.OdinInspector;

namespace HexGame.Units
{
    public class BomberMoveBehavior : UnitBehavior, IHaveTarget
    {
        [SerializeField]
        [Range(1f, 10f)]
        private float height = 3f;
        public bool isMoving { get; private set; }

        [SerializeField]
        private Transform transformToAlign;
        [SerializeField]
        private float alignSpeed = 100f;
        private Vector3 landingPosition;

        [SerializeField]
        private GameObject bombPrefab;
        [SerializeField]
        private int bombsToDrop = 5;
        [SerializeField]
        private float bombInterval = 0.25f;

        private void OnDisable()
        {
            DOTween.Kill(this,true);
        }

        [Button]
        public void SetDestination(Vector3 position)
        {
            if (isMoving)
                return;

            landingPosition = this.transform.position;
            position = position + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0f, UnityEngine.Random.Range(-0.5f, 0.5f));
            StartCoroutine(DoBombRun(position));
        }

        private IEnumerator DoBombRun(Vector3 position)
        {
            isMoving = true;
            if ((position - this.transform.position).sqrMagnitude > 0.1f) //attempt to prevent moving up and down if already at destination
            {
                float moveTime = (this.transform.position - position).magnitude / GetStat(Stat.speed);
                float verticalTime = 2f * Mathf.Abs(height) / GetStat(Stat.speed);
                Vector3 hoverPosition = new Vector3(position.x, height, position.z);

                Sequence hoveMoveSequence = DOTween.Sequence();
                if (transformToAlign != null)
                    hoveMoveSequence.Append(transformToAlign.DOLocalRotate(new Vector3(0f, 90f, 0), 0.25f));
                hoveMoveSequence.Append(this.transform.DOMoveY(height, verticalTime));
                hoveMoveSequence.Append(this.transform.DOLookAt(position, 0.75f, AxisConstraint.Y, Vector3.up));
                if (transformToAlign != null)
                    hoveMoveSequence.Append(transformToAlign.DOLocalRotate(new Vector3(0f, 0f, 0), 0.25f));
                hoveMoveSequence.Append(this.transform.DOMove(hoverPosition, moveTime));
                if (transformToAlign != null)
                    hoveMoveSequence.Append(transformToAlign.DOLocalRotate(new Vector3(0f, 90f, 0), 0.25f));

                yield return hoveMoveSequence.WaitForCompletion();

                //wait
                yield return new WaitForSeconds(0.5f);
                //drop the bomb
                yield return DropBombs();
                //wait
                yield return new WaitForSeconds(0.5f);


                //move back to base
                Sequence moveBackToBase = DOTween.Sequence();
                hoverPosition = new Vector3(landingPosition.x, height, landingPosition.z);

                moveBackToBase.Append(this.transform.DOMove(hoverPosition, moveTime));
                moveBackToBase.Append(this.transform.DOMoveY(landingPosition.y, verticalTime));
                yield return moveBackToBase.WaitForCompletion();
            }
            isMoving = false;
        }

        private IEnumerator DropBombs()
        {
            for (int i = 0; i < bombsToDrop; i++)
            {
                Instantiate(bombPrefab, this.transform.position - Vector3.up * 0.25f, Quaternion.identity);
                yield return new WaitForSeconds(bombInterval);
            }
        }


        public void SetTarget(Unit target)
        {
            SetDestination(target.transform.position);
        }

        public override void StartBehavior()
        {
            isFunctional = true;
            height += UnityEngine.Random.Range(-0.25f, 0.25f);
        }

        public override void StopBehavior()
        {
            isFunctional = false;
        }
    }
}