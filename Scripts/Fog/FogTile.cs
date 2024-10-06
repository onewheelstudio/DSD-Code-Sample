using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class FogTile : MonoBehaviour, ISelfValidator
{
    public bool isDown = false;
    private float startScale;
    [SerializeField] private bool allowReAppear = false;
    private int agentCount => HexTileManager.NumberOfRevealersAtLocation(transform.position);
    [SerializeField] private Transform fogTile;

    [Header("Tween Settings")]
    [SerializeField] private float tweenTime = 0.4f;
    [SerializeField] private Ease ease = Ease.InOutCirc;

    [Header("Options")]
    [SerializeField] private bool doMove = true;
    [SerializeField] private bool doScale = true;    

    private void Awake()
    {
        startScale =  fogTile.localScale.x;
    }

    private void Start()
    {
        if (agentCount > 0)
            DoTileDisappear(0.5f, Ease.Flash);
    }

    private void OnDestroy()
    {
        DOTween.Kill(this,true);
    }

    [Button]
    public void DoTileAppear(float tweenTime, Ease ease)
    {
        if (!isDown)
            return;

        isDown = false;

        if (doScale)
            fogTile.DOBlendableScaleBy(Vector3.one * startScale, tweenTime).SetEase(ease);
        if(doMove)
            fogTile.DOBlendableMoveBy(Vector3.up, tweenTime).SetEase(ease);
    }

    [Button]
    public void DoTileDisappear(float tweenTime, Ease ease)
    {
        if (isDown)
            return;

        isDown = true;
        
        Tween tween = null;

        if (doScale)
            tween = fogTile.DOBlendableScaleBy(Vector3.one * -startScale, tweenTime).SetEase(ease);
        if(doMove)
            tween = fogTile.DOBlendableMoveBy(Vector3.down, tweenTime).SetEase(ease);

        if (!allowReAppear)
            tween.OnComplete(() => Destroy(this.gameObject));
    }

    public void AddAgent(FogRevealer agent)
    {
        if (agentCount >= 0)
            DoTileDisappear(tweenTime, ease);
    }

    public void RemoveAgent(FogRevealer agent)
    {
        if(agentCount == 0 && allowReAppear)
            DoTileAppear(tweenTime, ease);
    }

    public void Validate(SelfValidationResult result)
    {
        if (fogTile == null)
            result.AddWarning("Nothing to move!!");
    }
}
