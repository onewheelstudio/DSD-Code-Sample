using DG.Tweening;
using Nova;
using UnityEngine;

public class MinimapPulse : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private float pulseSize = 1.25f;
    private Vector3 startScale;

    private void Awake()
    {
        startScale = this.transform.localScale;
    }

    // Start is called before the first frame update
    private void OnEnable()
    {
        this.transform.DOScale(startScale * pulseSize, pulseSpeed).SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDisable()
    {
        DOTween.Kill(this.transform);
    }
}
