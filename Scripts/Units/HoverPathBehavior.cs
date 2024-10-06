using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;

namespace HexGame.Units
{
    public class HoverPathBehavior : UnitBehavior, IHaveTarget
    {
        [SerializeField]
        [Range(1f, 10f)]
        private float height = 1.25f;
        public bool isMoving { get; private set; }

        private void OnDisable()
        {
            DOTween.Kill(this,true);
        }

        [Button]
        public void SetDestination(Vector3 position)
        {
            if (isMoving)
                return;

            StartCoroutine(DoHoverMove(position));
        }

        private IEnumerator DoHoverMove(Vector3 position)
        {
            isMoving = true;
            if ((position - this.transform.position).sqrMagnitude > 0.1f) //attempt to prevent moving up and down if already at destination
            {
                float moveTime = (this.transform.position - position).magnitude / GetStat(Stat.speed);
                float verticalTime = 2f * Mathf.Abs(height) / GetStat(Stat.speed);
                Vector3 hoverPosition = new Vector3(position.x, height, position.z);

                Sequence hoveMoveSequence = DOTween.Sequence();
                hoveMoveSequence.Append(this.transform.DOMoveY(height, verticalTime));
                hoveMoveSequence.Append(this.transform.DOMove(hoverPosition, moveTime));
                hoveMoveSequence.Append(this.transform.DOMoveY(position.y, verticalTime));

                yield return hoveMoveSequence.WaitForCompletion();
            }
            isMoving = false;
        }

        public void SetTarget(Unit target)
        {
            SetDestination(target.transform.position);
        }

        public override void StartBehavior()
        {
        }

        public override void StopBehavior()
        {
        }
    }
}