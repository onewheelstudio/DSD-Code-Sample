using Nova;
using Sirenix.OdinInspector;
using UnityEngine;

public class HighlightButton : MonoBehaviour
{
    [Button]
    private void DoAnimation()
    {
        HighlightButtonAnimation animation = new HighlightButtonAnimation(GetComponent<UIBlock2D>(), Color.red, 1.2f);
        animation.Loop(1f);
    }
}

public struct HighlightButtonAnimation : IAnimation
{
    public UIBlock2D block;
    public Color highlightColor;
    private float startScale;
    private float finalScale;
    private Color startColor;

    public HighlightButtonAnimation(UIBlock2D block, Color highLightColor, float scale)
    {
        this.block = block;
        highlightColor = highLightColor;
        startScale = block.transform.localScale.x;
        this.finalScale = scale;
        startColor = block.Color;
    }

    public void Update(float percentDone)
    {
        if(percentDone < 0.5f)
        {
            block.transform.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.one * finalScale, percentDone * 2f);
            block.Color = Color.Lerp(startColor, highlightColor, percentDone * 2f);
        }
        else
        {
            block.transform.localScale = Vector3.Lerp(Vector3.one * finalScale, Vector3.one * startScale, 2f * (percentDone - 0.5f));
            block.Color = Color.Lerp(highlightColor, startColor, 2f * (percentDone - 0.5f));
        }
    }
}
