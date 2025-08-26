using Nova;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Nova.Animations
{
    public struct ButtonHighlightAnimation : IAnimation
    {
        public Vector3 startSize;
        public Vector3 endSize;
        public Color startColor;
        public Color endColor;
        public float endAlpha;
        public UIBlock uIBlock;

        public void Update(float progress)
        {
            //uIBlock.Size.Value = Vector3.Lerp(startSize, endSize, Mathf.Sin(2*Mathf.PI * progress));
            float time = 0.5f - Mathf.Abs(progress - 0.5f);
            uIBlock.Size.Value = Vector3.Lerp(startSize, endSize, time);
            endColor.a = endAlpha;
            uIBlock.Color = Color.Lerp(startColor, endColor, time);
        }
    }
}
