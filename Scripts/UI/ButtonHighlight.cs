using DG.Tweening;
using Nova;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using UnityEngine;

public class ButtonHighlight : MonoBehaviour
{
    [SerializeField] private Vector2 maxSize;
    private Vector2 startSize;
    [SerializeField] private Color highlightColor = Color.white;
    private Color startColor;
    [SerializeField] private float highlightTime = 0.35f;
    [SerializeField] private float maxBorderWidth = 2f;
    private float startBorderWidth;
    private Button button;
    
    private Tween sizeTween;
    private Tween colorTween;
    private Tween widthTween;
    private UIBlock2D block;

    private void Awake()
    {
        block = this.GetComponent<UIBlock2D>();
        startSize = block.Size.XY.Raw;
        startColor = block.Border.Color;
        startBorderWidth = block.Border.Width.Raw;
        button = this.GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.clicked += StopHighLight;
    }

    private void OnDisable()
    {
        button.clicked -= StopHighLight;
        DOTween.Kill(this,true);
    }

    [Button]
    public void DoHighLight()
    {
        return;

        sizeTween = block.DoScale(maxSize, highlightTime).SetLoops(-1, LoopType.Yoyo);
        colorTween = block.DoBorderColor(highlightColor, highlightTime).SetLoops(-1, LoopType.Yoyo);
        widthTween = block.DoBorderWidth(maxBorderWidth, highlightTime).SetLoops(-1, LoopType.Yoyo);
    }

    [Button]
    public void StopHighLight()
    {
        return;

        sizeTween?.Kill();
        colorTween?.Kill();
        widthTween?.Kill();

        block.Border.Color = startColor;
        block.Border.Width = startBorderWidth;
        block.Size.XY.Raw = startSize;
    }
}
