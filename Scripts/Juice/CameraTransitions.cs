using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;
using DG.Tweening;
using VolumetricFogAndMist2;
using System.Collections;

public class CameraTransitions : MonoBehaviour
{
    private Camera Camera;
    [SerializeField] private Volume volume;
    [SerializeField] private VolumeProfile profile;
    private VolumeProfile clonedProfile;
    private Vignette vignette;
    private Vignette clonedVignette;

    [Header("Day Settings")]
    [SerializeField] private float dayFOV = 60f;
    [SerializeField] private float dayVignette = 1f;
    [Header("Night Settings")]
    [SerializeField] private float nightFOV = 40f;
    [SerializeField] private float nightVignette = 0.5f;

    [Header("Fog")]
    [SerializeField] private AnimationCurve fogCurve;
    [SerializeField] private VolumetricFogProfile fogProfile;

    [Header("Skybox")]
    [SerializeField] private Color dayColor;
    [SerializeField] private Color nightColor;

    [Header("Clouds")]
    [SerializeField] private Transform bottomCloud;
    [SerializeField] private Transform topCloud;
    [SerializeField] private Material bottomCloudMaterial;
    [SerializeField] private Material topCloudMaterial;
    [SerializeField, Range(0f, 5f)] private float fadeDistance = 3f;
    

    private void Awake()
    {
        //clone the profile and any components you want to change
        //I only need to change the vignette so I only clone that
        clonedProfile = Instantiate(profile);
        clonedProfile.TryGet(out vignette);
        clonedVignette = Instantiate(vignette);

        //assign the component to the cloned profile
        //seems like there should be a better way, but this works
        //you might need to experiment with the index value
        clonedProfile.components[0] = clonedVignette;
        //assign the profile to the volume  
        volume.profile = clonedProfile;

        Camera = GetComponent<Camera>();
        dayFOV = Camera.fieldOfView;
        //dayVignette = vignette.intensity.value;

        //set up initial light settingss
        Camera.backgroundColor = dayColor;
        Camera.fieldOfView = nightFOV;
        vignette.intensity.value = 0.35f;
    }

    private void OnEnable()
    {
        DayNightManager.transitionToDay += ToggleDay;
        DayNightManager.transitionToNight += ToggleNight;
        StateOfTheGame.GameStarted += ToggleDay;
    }


    private void OnDisable()
    {
        DayNightManager.transitionToDay -= ToggleDay;
        DayNightManager.transitionToNight -= ToggleNight;
        StateOfTheGame.GameStarted -= ToggleDay;
        DOTween.Kill(this,true);
    }

    private void Update()
    {
        if(fogProfile)
        {
            fogProfile.distance = fogCurve.Evaluate(this.transform.position.y / 25f) * 40f;
        }

        if (this.transform.position.y > bottomCloud.position.y)
        {
            float alpha = (this.transform.position.y - bottomCloud.position.y) / fadeDistance;
            alpha = Mathf.Clamp01(alpha);
            bottomCloudMaterial.SetFloat("_Alpha", Mathf.Lerp(0, 0.9f, alpha));
        }
        else
            bottomCloudMaterial.SetFloat("_Alpha", 0f);

        if (this.transform.position.y > topCloud.position.y)
        {
            float alpha = (this.transform.position.y - topCloud.position.y) / fadeDistance;
            alpha = Mathf.Clamp01(alpha);
            topCloudMaterial.SetFloat("_Alpha", Mathf.Lerp(0, 0.9f, alpha));
        }
        else
            topCloudMaterial.SetFloat("_Alpha", 0f);
    }

    private void ToggleDay()
    {
        ToggleDay(0, 2f);
    }

    [Button]
    private void ToggleNight(int dayNumber, float delay)
    {
        StartCoroutine(DelayedNighTransition(delay));
    }

    private IEnumerator DelayedNighTransition(float delay)
    {
        yield return new WaitForSeconds(delay);
        Camera.DOFieldOfView(nightFOV, delay * 2f);
        clonedVignette.DoIntensity(nightVignette, delay * 2f);
        Camera.DOColor(nightColor, delay * 2f);
    }

    [Button]
    private void ToggleDay(int dayNumber, float delay)
    {
        Camera.DOFieldOfView(dayFOV, delay * 2f);
        clonedVignette.DoIntensity(dayVignette, delay * 2f);
        Camera.DOColor(dayColor, delay * 2f);
    }
}
