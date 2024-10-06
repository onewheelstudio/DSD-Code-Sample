using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFX : MonoBehaviour
{
    public List<AudioClip> clips;
    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0f, 0.2f)]
    public float volumeVariation = 0.05f;
    [Range(0f, 2f)]
    public float pitch = 1f;
    [Range(0f, 0.2f)]
    public float pitchVariation = 0.05f;
    public void PlaySFX()
    {
        if(clips.Count == 0)
        {
            Debug.LogError($"No audio clips assigned to {this.gameObject.name}.", this.gameObject);
            return;
        }

        AudioClip clip = GetRandomClip(this);
        if (clip == null)
            return;

        float volume = this.volume + Random.Range(-volumeVariation, volumeVariation);
        float pitch = this.pitch + Random.Range(-pitchVariation, pitchVariation);
        AudioManager.Play(clip, volume, pitch, this.transform.position);
    }

    private AudioClip GetRandomClip(SFX sfx)
    {
        if (sfx.clips.Count > 0)
            return sfx.clips[Random.Range(0, sfx.clips.Count - 1)];
        else
            return null;
    }
}
