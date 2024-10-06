using DG.Tweening;
using System.Collections;
using UnityEngine;

public class FogBomb : MonoBehaviour
{
    [SerializeField] private float dropTime = 2f;
    [SerializeField] private float reconTime = 10f;
    [SerializeField] private float shrinkTime = 2f;
    [SerializeField] private float sightRadius = 8f;
    private WaitForSeconds reconWait;
    private SphereCollider sphereCollider;

    // Start is called before the first frame update
    void Awake()
    {
        reconWait = new WaitForSeconds(reconTime);
        sphereCollider = GetComponentInChildren<SphereCollider>();
    }

    private void OnEnable()
    {
        StartCoroutine(DoFogStuff());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        DOTween.Kill(this.gameObject);
        DOTween.Kill(sphereCollider);
    }

    private IEnumerator DoFogStuff()
    {
        sphereCollider.radius = sightRadius;
        yield return DoDrop();
        yield return FogTimer();
        this.gameObject.SetActive(false);
    }

    private IEnumerator FogTimer()
    {
        yield return reconWait;
        Tween shrink = sphereCollider.DoRadius(0, shrinkTime).SetEase(Ease.Linear);
        yield return shrink.WaitForCompletion();
    }

    private IEnumerator DoDrop()
    {
        Tween drop = this.transform.DOMoveY(0, dropTime).SetEase(Ease.Linear);
        yield return drop.WaitForCompletion();
    }
}
