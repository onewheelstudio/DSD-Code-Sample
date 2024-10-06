using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class UISlideInOut : MonoBehaviour
{
    private Vector3 outPosition;
    private Vector3 inPosition;
    private Vector3 startScale;
    [SerializeField] private float tweenTime = 0.25f;
    private float variation;
    private Vector3 tweenDirection;


    private void Awake()
    {
    }

    private void OnDisable()
    {
        DOTween.Kill(this,true);

    }

    public void Initialize(Vector3 inPosition)
    {
        outPosition = this.transform.localPosition;
        startScale = this.transform.localScale;
        this.inPosition = inPosition;
    }

    public void SlideOut()
    {
        variation = UnityEngine.Random.Range(-0.075f, 0.075f);
        this.transform.DOLocalMove(outPosition, tweenTime + variation).SetEase(Ease.InExpo);
        this.transform.DOScale(startScale, tweenTime + variation).SetEase(Ease.InExpo);
    }

    public void SlideIn()
    {
        variation = UnityEngine.Random.Range(-0.075f, 0.075f);
        this.transform.DOLocalMove(inPosition, tweenTime + variation).SetEase(Ease.InExpo);
        this.transform.DOScale(Vector3.zero, tweenTime + variation).SetEase(Ease.InExpo);
    }
}
