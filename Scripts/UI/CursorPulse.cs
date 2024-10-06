using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CursorPulse : MonoBehaviour
{
    [SerializeField]
    [Range(0.95f, 1.05f)]
    private float scale;
    private Vector3 startScale;
    [SerializeField]
    [Range(0.1f, 5f)]
    private float scaleTime;
    Sequence sequence;

    private void Awake()
    {
        startScale = transform.localScale;
    }

    private void OnEnable()
    {
        DoPulse();
    }

    private void OnDisable()
    {
        sequence.Kill();
        DOTween.Kill(this,true);
    }

    private void DoPulse()
    {
        sequence = DOTween.Sequence();
        sequence.Append(this.transform.DOScale(startScale * scale, scaleTime).SetEase(Ease.Linear));
        sequence.Append(this.transform.DOScale(startScale, scaleTime).SetEase(Ease.Linear));

        sequence.SetLoops(-1);
    }
}
