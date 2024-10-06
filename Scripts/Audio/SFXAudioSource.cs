using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OWS.ObjectPooling;
using System;

public class SFXAudioSource : MonoBehaviour, IPoolable<SFXAudioSource>
{
    public AudioSource AudioSource => audioSource;
    private AudioSource audioSource;

    private Action<SFXAudioSource> returnAction;

    private void OnDisable()
    {
        ReturnToPool();
    }

    public void Initialize(Action<SFXAudioSource> returnAction)
    {
        this.returnAction = returnAction;
        this.audioSource = this.GetComponent<AudioSource>();
    }

    public void ReturnToPool()
    {
        this.returnAction?.Invoke(this);
    }
}
