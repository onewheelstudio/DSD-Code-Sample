using DG.Tweening;
using HexGame.Grid;
using HexGame.Units;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class FogUnit : MonoBehaviour, ISelfValidator
{
    [SerializeField] private bool showOnRevel = true;
    private bool isDown = false;
    public bool IsDown => isDown;
    private float startScale;
    private float miniMapStartScale;
    [SerializeField] private Transform meshObject;
    [SerializeField] private Transform miniMapIcon;

    [Header("Tween Settings")]
    [SerializeField] private float tweenTime = 0.4f;
    [SerializeField] private Ease ease = Ease.InOutCirc;
    [SerializeField] private float moveDistance = 4f;

    [Header("Options")]
    [SerializeField] private bool doMove = true;
    [SerializeField] private bool doScale = true;
    private Hex3 currentLocation;
    protected HexTileManager htm;
    List<FogRevealer> fogRevealers = new List<FogRevealer>();
    public event Action<bool> isHidden;
    private static ObjectPool<PoolObject> disappearPool;
    private static ObjectPool<PoolObject> appearPool;
    [SerializeField] private GameObject disappearParticles;
    [SerializeField] private GameObject appearParticles;
    [SerializeField] private bool useForwardOffset = false;

    private void Awake()
    {
        startScale = meshObject.localScale.x;
        if(miniMapIcon)
            miniMapStartScale = miniMapIcon.localScale.x;

        if(htm == null)
            htm = FindObjectOfType<HexTileManager>();

        if (disappearPool == null && disappearParticles)
            disappearPool = new ObjectPool<PoolObject>(disappearParticles);
        if (appearPool == null && appearParticles)
            appearPool = new ObjectPool<PoolObject>(appearParticles);
    }

    private void OnEnable()
    {
        if (showOnRevel)
            DoTileDisappear(0.5f, Ease.Flash);

        PlayerUnit.unitCreated += UpdateRevealStatus;
        PlayerUnit.unitRemoved += UpdateRevealStatus;
    }

    private void OnDisable()
    {
        PlayerUnit.unitCreated -= UpdateRevealStatus;
        PlayerUnit.unitRemoved -= UpdateRevealStatus;
        DOTween.Kill(this,true);
    }



    private void Update()
    {
        if (useForwardOffset && currentLocation == (this.transform.position + this.transform.forward).ToHex3())
            return;
        
        if (currentLocation == this.transform.position.ToHex3())
            return;

        UpdateRevealStatus();
    }

    private void UpdateRevealStatus(Unit unit)
    {
        if (this == null || !this.gameObject.activeInHierarchy)
            return;
        StartCoroutine(DelayUpdateRevealStatus());
    }

    private IEnumerator DelayUpdateRevealStatus()
    {
        yield return null; //ensure that fog revealer list is updated
        UpdateRevealStatus();
    }

    public void UpdateRevealStatus()
    {
        if(useForwardOffset)
            currentLocation = (this.transform.position + this.transform.forward).ToHex3();
        else
            currentLocation = this.transform.position.ToHex3();
        if (HexTileManager.NumberOfRevealersAtLocation(currentLocation) > 0)
        {
            DoTileAppear(tweenTime, ease);
        }
        else
        {
            DoTileDisappear(tweenTime, ease);
        }
    }

    public void DoTileAppear(float tweenTime, Ease ease)
    {
        if (!isDown)
            return;

        isDown = false;
        isHidden?.Invoke(isDown);
        float time = tweenTime + Random.Range(-0.025f, 0.025f);

        if(miniMapIcon)
            miniMapIcon.gameObject.SetActive(true);
            //miniMapIcon.DOBlendableScaleBy(Vector3.one * miniMapStartScale, time).SetEase(ease);


        if (doScale)
        {
            if(meshObject)
                meshObject.DOBlendableScaleBy(Vector3.one * startScale, time).SetEase(ease);
        }
        if(doMove)
        {
            if (meshObject)
                meshObject.DOBlendableMoveBy(Vector3.up * moveDistance, time).SetEase(ease);
        }

        if (appearPool != null)
        {
            GameObject newParticles = appearPool.PullGameObject(this.transform.position + this.transform.forward * 0.1f + Vector3.up * 0.25f);
            //newParticles.transform.SetParent(this.transform);
            newParticles.transform.localScale = Vector3.one * 0.6f;
        }
    }

    private void DoTileDisappear(float tweenTime, Ease ease)
    {
        if (isDown)
            return;

        isDown = true;
        isHidden?.Invoke(isDown);

        float time = tweenTime + Random.Range(-0.025f, 0.025f);

        if (miniMapIcon)
            miniMapIcon.gameObject.SetActive(false);
            //miniMapIcon.DOBlendableScaleBy(Vector3.one * -miniMapStartScale, time).SetEase(ease);

        if (doScale)
        { 
            if (meshObject)
                meshObject.DOBlendableScaleBy(Vector3.one * -startScale, tweenTime).SetEase(ease);
          
        }
        if(doMove)
        {
            if (meshObject)
                meshObject.DOBlendableMoveBy(Vector3.down * moveDistance, tweenTime).SetEase(ease);
        }

        if (disappearPool != null)
        {
            GameObject newParticles = disappearPool.PullGameObject(this.transform.position + this.transform.forward * 0.1f + Vector3.up * 0.25f);
            //newParticles.transform.SetParent(this.transform);
            newParticles.transform.localScale = Vector3.one * 0.6f;
        }
    }

    public void Validate(SelfValidationResult result)
    {
        if (meshObject == null)
            result.AddWarning("Nothing to move!!");
    }
}
