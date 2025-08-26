using DG.Tweening;
using Nova;
using System;
using UnityEngine;

public class IntroFade : MonoBehaviour
{
    private ClipMask clipMask;
    [SerializeField] private bool fadeOnStart = true;

    // Start is called before the first frame update
    void Start()
    {
        clipMask = GetComponent<ClipMask>();
        if (fadeOnStart)
            FadeToTransparent();
    }


    private void OnDisable()
    {
        DOTween.Kill(this,true);
    }

    public void FadeToBlack(Action callback = null)
    {
        clipMask.DoFade(1f, 1f, callback);
    }
    
    public void FadeToTransparent(Action callback = null)
    {
        clipMask.DoFade(0f, 1f, callback);
    }
}
