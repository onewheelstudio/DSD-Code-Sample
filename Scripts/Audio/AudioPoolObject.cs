using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OWS.ObjectPooling;
using System;

[RequireComponent(typeof(AudioSource))]
public class AudioPoolObject : MonoBehaviour, IPoolable<AudioPoolObject>
{
    private Action<AudioPoolObject> returnToPool;
    private AudioSource audioSource;

    public void PlayAudio(AudioClip clip, float volume, float pitch = 1f)
    {
        if (clip == null)
        {
            this.gameObject.SetActive(false);
            return;
        }

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.Play();
        StartCoroutine(WaitUntilFinished());
    }

    private IEnumerator WaitUntilFinished()
    {
        yield return new WaitUntil(() => !audioSource.isPlaying);
        this.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        ReturnToPool();
    }

    public void Initialize(Action<AudioPoolObject> returnAction)
    {
        //cache reference to return action
        this.returnToPool = returnAction;
        this.audioSource = this.GetComponent<AudioSource>();
    }

    public void ReturnToPool()
    {
        //invoke and return this object to pool
        returnToPool?.Invoke(this);
    }
}
