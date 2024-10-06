using DG.Tweening;
using Nova;
using System;
using UnityEngine;

public class IntroFade : MonoBehaviour
{
    private UIBlock2D fadeBlock;

    // Start is called before the first frame update
    void Start()
    {
        fadeBlock = GetComponent<UIBlock2D>();
        FadeToTransparent();
    }


    private void OnDisable()
    {
        DOTween.Kill(this,true);
    }

    public void FadeToBlack(Action callback = null)
    {
        fadeBlock.DoFadeIn(1f, callback);
    }
    
    public void FadeToTransparent(Action callback = null)
    {
        fadeBlock.DoFadeOut(0f, callback);
    }
}
