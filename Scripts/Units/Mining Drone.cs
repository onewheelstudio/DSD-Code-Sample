using DG.Tweening;
using HexGame.Grid;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class Drone : MonoBehaviour
{
    [SerializeField] private Transform impactPoint;
    [SerializeField] private float hoverHeight;
    [SerializeField] private float speed;
    private WaitForSeconds ActionDelay;
    [SerializeField] private GameObject beamObject;
    [SerializeField] private LayerMask layerMask;
    private bool isDoing = false;
    public bool IsMining => isDoing;
    private Vector3 startPosition;

    private void Awake()
    {
        startPosition = this.transform.localPosition;
        hoverHeight += Random.Range(-0.1f, 0.1f);
        ActionDelay = new WaitForSeconds(Random.Range(4f, 6f));
        speed += Random.Range(-0.1f, 0.1f);
    }

    [Button]
    public void MoveToLocation(Hex3 location)
    {
        StartCoroutine(DoDroneAction(location));
    }

    public IEnumerator DoDroneAction(Vector3 position)
    {
        isDoing = true;
        float angle = Random.Range(0, Mathf.PI * 2f);
        position += new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.60f;

        if ((position - this.transform.position).sqrMagnitude > 0.1f) //attempt to prevent moving up and down if already at destination
        {
            float moveTime = (this.transform.position - position).magnitude / speed;
            float verticalTime = 2f * Mathf.Abs(hoverHeight) / speed;

            Sequence hoveMoveSequence = DOTween.Sequence();
            hoveMoveSequence.Append(this.transform.DOMoveY(hoverHeight, verticalTime));
            hoveMoveSequence.Append(this.transform.DOLookAt(position, 0.75f, AxisConstraint.Y, Vector3.up));
            hoveMoveSequence.Append(this.transform.DOMove(position + Vector3.up * hoverHeight, moveTime));
            yield return hoveMoveSequence.WaitForCompletion();

            ToggleBeam(true);
            yield return ActionDelay;
            ToggleBeam(false);
        }

        isDoing = false;
    }

    public IEnumerator DoDroneAction(Vector3 position, WaitForSeconds actionDelay)
    {
        isDoing = true;
        float angle = Random.Range(0, Mathf.PI * 2f);
        position += new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.60f;

        if ((position - this.transform.position).sqrMagnitude > 0.1f) //attempt to prevent moving up and down if already at destination
        {
            float moveTime = (this.transform.position - position).magnitude / speed;
            float verticalTime = 2f * Mathf.Abs(hoverHeight) / speed;

            Sequence hoveMoveSequence = DOTween.Sequence();
            hoveMoveSequence.Append(this.transform.DOMoveY(hoverHeight, verticalTime));
            hoveMoveSequence.Append(this.transform.DOLookAt(position, 0.75f, AxisConstraint.Y, Vector3.up));
            hoveMoveSequence.Append(this.transform.DOMove(position + Vector3.up * hoverHeight, moveTime));
            yield return hoveMoveSequence.WaitForCompletion();

            ToggleBeam(true);
            yield return actionDelay;
            ToggleBeam(false);
        }

        isDoing = false;
    }

    [Button]
    public void DoReturnToStart()
    {
        StopAllCoroutines();
        StartCoroutine(ReturnToStart());
    }

    private IEnumerator ReturnToStart()
    {
        isDoing = true;
        ToggleBeam(false);
        float moveTime = (this.transform.position - startPosition).magnitude / speed;
        float verticalTime = 2f * Mathf.Abs(hoverHeight) / speed;

        Sequence hoveMoveSequence = DOTween.Sequence();
        hoveMoveSequence.Append(this.transform.DOLookAt(this.transform.parent.position + startPosition + Vector3.up * hoverHeight, 0.75f, AxisConstraint.Y, Vector3.up));
        hoveMoveSequence.Append(this.transform.DOLocalMove(startPosition + Vector3.up * hoverHeight, moveTime));
        hoveMoveSequence.Append(this.transform.DOLocalMove(startPosition, verticalTime));
        yield return hoveMoveSequence.WaitForCompletion();
        
        isDoing = false;
    }

    private void ToggleBeam(bool isOn)
    {
        if(isOn)
        {
            if(Physics.Raycast(this.transform.position, Vector3.down, out RaycastHit hit, 5f, layerMask))
            {
                if((hit.point - this.transform.position).sqrMagnitude > 1f) //we didn't hit the ground
                {
                    Vector3 hitPoint = this.transform.position;
                    hitPoint.y = 0.25f;
                    impactPoint.transform.position = hitPoint + new Vector3(Random.Range(-0.1f, 0.1f), 0f, Random.Range(-0.1f, 0.1f));
                }
                else
                    impactPoint.transform.position = hit.point + Vector3.up * 0.1f + new Vector3(Random.Range(-0.1f, 0.1f), 0f, Random.Range(-0.1f, 0.1f));
            }
        }

        beamObject.SetActive(isOn);
    }

}
