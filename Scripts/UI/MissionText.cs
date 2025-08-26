using DG.Tweening;
using Nova;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MissionText : MonoBehaviour
{
    private int audioClipIndex = 0;
    [SerializeField] private ClipMask backgroundClipMask;
    [SerializeField] private ClipMask messageClipMask;
    [SerializeField] private TextBlock clickToContinue;
    [SerializeField] private bool showInEditor = false;

    [SerializeField] private List<AudioClip> audioClipList;
    private AudioSource audioSource;
    Tween clickToContinueTween;

    private void Start()
    {
        if(Application.isEditor && !showInEditor)
        {
            this.gameObject.SetActive(false);
            return;
        }

        audioSource = this.GetComponent<AudioSource>();

        this.transform.SetAsLastSibling();

        PlayIntro();

        clickToContinue.gameObject.SetActive(false);
        StartCoroutine(ShowClickToContinue());
    }

    private void OnDisable()
    {
        DOTween.Kill(clickToContinue);
        DOTween.Kill(this,true);
    }

    [Button]
    private void PlayIntro()
    {
        audioClipIndex = 0;
        backgroundClipMask.SetAlpha(0f);
        messageClipMask.SetAlpha(0f);
        StartCoroutine(DoIntro());
    }

    private IEnumerator DoIntro()
    {
        StartCoroutine(FadeIn(backgroundClipMask));
        StartCoroutine(FadeIn(messageClipMask));

        audioSource.clip = audioClipList[audioClipIndex];
        audioSource.Play();
        audioClipIndex++;
        float time = Time.realtimeSinceStartup + audioSource.clip.length + 0.5f;
        yield return new WaitUntil(() => Time.realtimeSinceStartup > time || Mouse.current.leftButton.wasPressedThisFrame);
        yield return null;

        audioSource.clip = audioClipList[audioClipIndex];
        audioSource.Play();
        audioClipIndex++;
        time = Time.realtimeSinceStartup + audioSource.clip.length + 0.5f;
        yield return new WaitUntil(() => Time.realtimeSinceStartup > time || Mouse.current.leftButton.wasPressedThisFrame);
        yield return null;

        audioSource.clip = audioClipList[audioClipIndex];
        audioSource.Play();
        audioClipIndex++;
        time = Time.realtimeSinceStartup + audioSource.clip.length + 0.5f;
        yield return new WaitUntil(() => Time.realtimeSinceStartup > time || Mouse.current.leftButton.wasPressedThisFrame);
        yield return null;

        yield return new WaitUntil(() => Mouse.current.leftButton.wasPressedThisFrame);
        StartCoroutine(FadeOut(backgroundClipMask));
        yield return StartCoroutine(FadeOut(messageClipMask));
        clickToContinueTween.Kill();
        this.gameObject.SetActive(false);
    }

    private IEnumerator FadeIn(ClipMask clipMask)
    {
        while(clipMask.Tint.a < 1f)
        {
            clipMask.SetAlpha(clipMask.Tint.a + Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator FadeOut(ClipMask clipMask)
    {
        while (clipMask.Tint.a > 0f)
        {
            clipMask.SetAlpha(clipMask.Tint.a - Time.deltaTime);
            yield return null;
        }
    }


    private IEnumerator ShowClickToContinue()
    {
        yield return new WaitForSeconds(5f);
        clickToContinue.gameObject.SetActive(true);
        clickToContinueTween = clickToContinue.DoFade(0f, 1f).SetLoops(-1, LoopType.Yoyo);
    }
}
