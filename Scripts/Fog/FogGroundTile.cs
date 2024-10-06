using DG.Tweening;
using HexGame.Grid;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FogGroundTile : MonoBehaviour, ISelfValidator
{
    public bool showOnRevel = true;
    private bool isDown = false;
    public bool IsDown => isDown;
    private bool hasBeenRevealed = false;
    public bool HasBeenRevealed => hasBeenRevealed;
    private float startScale;
    private float blankTileStartScale;
    private float revealedTileStartScale;
    private float miniMapStartScale;
    private int agentCount => HexTileManager.NumberOfRevealersAtLocation(transform.position);
    [SerializeField] private Transform meshObject;
    [SerializeField] private Transform blankTile;
    [SerializeField] private Transform revealedBlank;
    [SerializeField] private Transform miniMapIcon;

    [Header("Tween Settings")]
    [SerializeField] private float tweenTime = 0.4f;
    [SerializeField] private Ease ease = Ease.InOutCirc;
    [SerializeField] private float moveDistance = 4f;

    [Header("Options")]
    [SerializeField] private bool doMove = true;
    [SerializeField] private bool doScale = true;
    public static Action<FogGroundTile> TileRevealed;
    public static Action<FogGroundTile> TileHidden;

    [Header("Reveal Juice")]
    [SerializeField] private bool useRevealJuice = false;
    [SerializeField, ShowIf("useRevealJuice")] private GameObject revealEffects;
    public Action JuicedTileRevealed;
    private SFXType revealSFX = SFXType.ResourceReveal;


    private void OnDrawGizmos()
    {
        if(agentCount > 0)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(this.transform.position + Vector3.up, 0.5f);
        }
    }

    private void Awake()
    {
        startScale = meshObject ? meshObject.localScale.x : blankTile.localScale.x;

        //initialize blank tile for tweens
        if (blankTile)
        {
            blankTileStartScale = blankTile.localScale.x;
            blankTile.localScale = Vector3.zero;
            blankTile.transform.position -= Vector3.up;
        }

        if (revealedBlank)
        {
            revealedTileStartScale = revealedBlank.localScale.x;
            revealedBlank.localScale = Vector3.zero;
            revealedBlank.transform.position -= Vector3.up;
        }

        if (miniMapIcon)
        {
            miniMapStartScale = miniMapIcon.localScale.x;
            miniMapIcon.localScale = Vector3.zero;
        }

        MoveToStartConfiguration();
    }

    private void MoveToStartConfiguration()
    {
        if (doScale)
        {
            if (meshObject)
                meshObject.transform.localScale = Vector3.zero;
            if (blankTile)
                blankTile.transform.localScale = Vector3.one * blankTileStartScale;
            if (revealedBlank)
                revealedBlank.transform.localScale = Vector3.zero;
        }
        if (doMove)
        {
            if (meshObject)
                meshObject.transform.localPosition += Vector3.down * moveDistance;
            if (blankTile)
                blankTile.transform.localPosition = Vector3.zero;
            if (revealedBlank)
                revealedBlank.transform.localPosition = Vector3.down * moveDistance;
        }

        isDown = true;
    }

    private void OnDisable()
    {
        DOTween.Kill(this, true);
    }

    [Button]
    public void DoTileAppear(float tweenTime, Ease ease)
    {
        if (!isDown)
            return;

        if (meshObject.transform.position.y > -0.01f)
            return;

        isDown = false;
        float time = tweenTime + UnityEngine.Random.Range(-0.025f, 0.025f);

        if(miniMapIcon && !hasBeenRevealed)
            miniMapIcon.DOBlendableScaleBy(Vector3.one * miniMapStartScale, time).SetEase(ease);

        if (doScale)
        {
            if(meshObject)
                meshObject.DOBlendableScaleBy(Vector3.one * startScale, time).SetEase(ease);
            if(blankTile && !hasBeenRevealed)
                blankTile.DOBlendableScaleBy(Vector3.one * -blankTileStartScale, time).SetEase(ease);
            if(revealedBlank && hasBeenRevealed)
                revealedBlank.DOBlendableScaleBy(Vector3.one * -revealedTileStartScale, time).SetEase(ease);
        }
        if(doMove)
        {
            if (meshObject)
                meshObject.DOBlendableMoveBy(Vector3.up * moveDistance, time).SetEase(ease);
            if(blankTile && !hasBeenRevealed)
                blankTile.DOBlendableMoveBy(Vector3.down * moveDistance, time).SetEase(ease);
            if(revealedBlank && hasBeenRevealed)
                revealedBlank.DOBlendableMoveBy(Vector3.down * moveDistance, time).SetEase(ease);
        }
            
        if (TileRevealed != null && this.gameObject.activeSelf)
            StartCoroutine(InvokeRevealed(time));

        if(!hasBeenRevealed && useRevealJuice)
        {
            SFXManager.PlaySFX(revealSFX);
            JuicedTileRevealed?.Invoke();
            if(revealEffects)
                Instantiate(revealEffects, this.transform.position + Vector3.up * 0.1f, Quaternion.Euler(-90,0,0));
        }

        hasBeenRevealed = true;
    }

    private IEnumerator InvokeRevealed(float delay)
    {
        yield return new WaitForSeconds(delay);
        TileRevealed?.Invoke(this);
    }

    [Button]
    public void DoTileDisappear(float tweenTime, Ease ease)
    {
        if (isDown)
            return;

        isDown = true;

        float time = tweenTime + UnityEngine.Random.Range(-0.025f, 0.025f);
        
        if (miniMapIcon && !hasBeenRevealed)
            miniMapIcon.DOBlendableScaleBy(Vector3.one * -miniMapStartScale, time).SetEase(ease);
        
        if (doScale)
        { 
            if (meshObject)
                meshObject.DOBlendableScaleBy(Vector3.one * -startScale, time).SetEase(ease);
            if(blankTile && !hasBeenRevealed)
                blankTile.DOBlendableScaleBy(Vector3.one * blankTileStartScale, time).SetEase(ease);
            if(revealedBlank && hasBeenRevealed)
                revealedBlank.DOBlendableScaleBy(Vector3.one * revealedTileStartScale, time).SetEase(ease);

        }
        if(doMove)
        {
            if (meshObject)
                meshObject.DOBlendableMoveBy(Vector3.down * moveDistance, time).SetEase(ease);
            if(blankTile && !hasBeenRevealed)
                blankTile.DOBlendableMoveBy(Vector3.up * moveDistance, time).SetEase(ease);
            if(revealedBlank && hasBeenRevealed)
                revealedBlank.DOBlendableMoveBy(Vector3.up * moveDistance, time).SetEase(ease);
        }

        if (TileHidden != null && this.gameObject.activeSelf)
            StartCoroutine(InvokeHidden(time));
    }

    private IEnumerator InvokeHidden(float delay)
    {
        yield return new WaitForSeconds(delay);
        TileHidden?.Invoke(this);
    }

    [Button]
    public void AddAgent(FogRevealer agent)
    {
        if (Hex3.DistanceBetween(this.transform.position, agent.transform.position) > agent.sightDistance)
            return;

        if (agentCount >= 1 && showOnRevel && isDown)
            DoTileAppear(tweenTime, ease);
        else if (agentCount > 0 && !showOnRevel)
            DoTileDisappear(tweenTime, ease);
    }

    [Button]
    public void RemoveAgent(FogRevealer agent)
    {   
        agent.fogRevealDisabled -= RemoveAgent;
        if(agentCount == 0 && showOnRevel)
            DoTileDisappear(tweenTime, ease);
        else if(agentCount == 0 && !showOnRevel)
            DoTileAppear(tweenTime, ease);
    }

    public void Validate(SelfValidationResult result)
    {
        if (meshObject == null && blankTile == null)
            result.AddWarning("Nothing to move!!");
    }

    public async void AddAgents(List<FogRevealer> fogRevealers)
    {
        await DelayAddAgents(fogRevealers);
    }

    private async Task DelayAddAgents(List<FogRevealer> fogRevealers)
    {
        await Task.Yield();
        foreach (var agent in fogRevealers)
        {
            AddAgent(agent);
        }
    }
}
