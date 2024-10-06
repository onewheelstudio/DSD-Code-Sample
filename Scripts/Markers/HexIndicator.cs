using DG.Tweening;
using OWS.ObjectPooling;
using System;
using UnityEngine;

public class HexIndicator : MonoBehaviour, IPoolable<HexIndicator>
{

    private Action<HexIndicator> returnToPool;
    public MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    private void OnEnable()
    {
        PopIn();
    }

    public void PopIn()
    {
        this.transform.localScale = Vector3.zero;
        this.transform.DOScale(1f, 0.25f).OnComplete(Pulse);
    }

    private void Pulse()
    {
        this.transform.DOScale(0.95f, 0.5f).SetLoops(-1, LoopType.Yoyo);
    }

    public void PopOut()
    {
        DOTween.Kill(this.transform);
        this.transform.DOScale(0f, 0.1f).OnComplete(ReturnToPool);
    }

    public void Initialize(Action<HexIndicator> returnAction)
    {
        this.returnToPool = returnAction;
    }

    public void ReturnToPool()
    {
        returnToPool?.Invoke(this);
    }
}
