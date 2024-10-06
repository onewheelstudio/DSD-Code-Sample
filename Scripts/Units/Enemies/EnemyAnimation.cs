using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer.CrowdAnimations;
using Sirenix.OdinInspector;

public class EnemyAnimation : MonoBehaviour
{
    [SerializeField]
    private GPUICrowdPrefab GPUICrowd;
    [SerializeField]
    private AnimationClip moveAnimation;
    [SerializeField]
    private AnimationClip deathAnimation;


    [Button]
    private void PlayMove()
    {
        GPUICrowdAPI.StartAnimation(GPUICrowd, moveAnimation);
    }

    [Button]
    private void PlayDeath()
    {
        GPUICrowdAPI.StartAnimation(GPUICrowd, deathAnimation);
    }
}
