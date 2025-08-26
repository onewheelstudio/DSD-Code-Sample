using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexGame.Units
{
    public class HoverMoveBehavior : UnitBehavior, IHaveTarget
    {
        private float tempHeight;
        private float speed;
        public bool isMoving { get; private set; }

        [SerializeField]
        private Transform transformToAlign;
        [SerializeField]
        private float alignSpeed = 100f;
        [SerializeField] private bool lookAtDestination = true;
        public event Action reachedDestination;
        [SerializeField] private List<TrailRenderer> trails;
        [SerializeField] private List<GameObject> objectsToToggle;
        [SerializeField] private ParticleSystem[] particlesToToggle;
        public event Action<float> movementTime;
        private Sequence hoverMoveSequence;

        private void OnEnable()
        {
            if(this.unit == null)
            {
                //do stuff...?
            }
        }

        private void OnDisable()
        {
            DOTween.Kill(this,true);
            hoverMoveSequence.Kill();
        }

        [Button]
        public void SetDestination(Vector3 position)
        {
            if (isMoving)
                return;

            if(this.transform.position.y > position.y)
                tempHeight = this.transform.position.y + UnityEngine.Random.Range(0.15f, 0.3f);
            else
                tempHeight = position.y + UnityEngine.Random.Range(0.15f, 0.3f);

            tempHeight = Mathf.Max(tempHeight, 1f);
            
            speed = GetStat(Stat.speed);

            DoHoverMove(position, tempHeight);
        }

        private async Awaitable DoHoverMove(Vector3 position, float moveHeight)
        {
            isMoving = true;
            ToggleObjects(true);
            if ((position - this.transform.position).sqrMagnitude > 0.1f) //attempt to prevent moving up and down if already at destination
            {
                float moveTime = (this.transform.position - position).magnitude / speed;
                float verticalTime = 2f * Mathf.Abs(moveHeight) / speed;
                float totalTime = 0f; //used for the callbacks
                Vector3 hoverPosition = new Vector3(position.x, moveHeight, position.z);

                hoverMoveSequence = DOTween.Sequence();
                if (transformToAlign != null)
                {
                    hoverMoveSequence.Append(transformToAlign.DOLocalRotate(new Vector3(0f, 90f, -90f), 0.25f));
                    totalTime += 0.25f;
                }
                hoverMoveSequence.Append(this.transform.DOMoveY(moveHeight, verticalTime));
                totalTime += verticalTime;

                if(lookAtDestination)
                {
                    hoverMoveSequence.Append(this.transform.DOLookAt(position, 0.75f, AxisConstraint.Y, Vector3.up));
                    totalTime += 0.75f;
                }
                if (transformToAlign != null)
                {
                    hoverMoveSequence.Append(transformToAlign.DOLocalRotate(new Vector3(-90f, 90f, -90f), 0.25f));
                    totalTime += 0.25f;
                }

                //now that we've moved up toggle trails and move to next location
                hoverMoveSequence.InsertCallback(totalTime, () => ToggleTrails(true));
                hoverMoveSequence.Append(this.transform.DOMove(hoverPosition, moveTime));
                totalTime += moveTime;
                hoverMoveSequence.InsertCallback(totalTime, () => ToggleTrails(false));

                //do the final move down
                if (transformToAlign != null)
                {
                    hoverMoveSequence.Append(transformToAlign.DOLocalRotate(new Vector3(0f, 90f, -90f), 0.25f));
                    totalTime += 0.25f;
                }
                hoverMoveSequence.Append(this.transform.DOMoveY(position.y, verticalTime));
                totalTime += verticalTime;

                movementTime?.Invoke(totalTime);
                await Awaitable.WaitForSecondsAsync(totalTime);
            }

            if (destroyCancellationToken.IsCancellationRequested)
                return;

            isMoving = false;
            ToggleObjects(false);
            reachedDestination?.Invoke();
        }


        public void SetTarget(Unit target)
        {
            SetDestination(target.transform.position);
        }

        public override void StartBehavior()
        {
            isFunctional = true;
        }

        public override void StopBehavior()
        {
            isFunctional = false;
        }

        private void ToggleTrails(bool isOn)
        {
            if(trails == null || trails.Count == 0)
                return;

            foreach (var trail in trails)
            {
                trail.emitting = isOn;
            }
        }

        private void ToggleObjects(bool isOn)
        {
            if(objectsToToggle != null)
            {
                for(int i = 0; i < objectsToToggle.Count; i++)
                {
                    if (objectsToToggle[i] == null)
                    {
                        Debug.Log($"Null Object on {this.gameObject}", this.gameObject);
                        continue;
                    }

                    objectsToToggle[i].SetActive(isOn);
                }
            }

            if (particlesToToggle != null)
            {
                for (int i = 0; i < particlesToToggle.Length; i++)
                {
                    if (isOn)
                        particlesToToggle[i].Play();
                    else
                        particlesToToggle[i].Stop();
                }
            }
        }
    }
}