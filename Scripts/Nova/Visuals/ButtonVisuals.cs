using Nova;
using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using DG.Tweening;

namespace NovaSamples.UIControls
{
    /// <summary>
    /// An <see cref="ItemVisuals"/> for a simple button control.
    /// </summary>
    [System.Serializable]
    [MovedFrom(false, null, "Assembly-CSharp")]
    public class ButtonVisuals : UIControlVisuals
    {
        [Space]
        [Tooltip("The button's background UIBlock.")]
        public UIBlock2D Background = null;
        [Tooltip("The TextBlock to display the button's label.")]
        public TextBlock Label = null;
        [Range(0.9f,1.5f)]public float hoverScale = 1f;
        public InfoToolTip toolTip;

        protected override UIBlock TransitionTargetFallback => Background;

        internal static void Hovered(Gesture.OnHover evt, ButtonVisuals visuals)
        {
            visuals.toolTip?.OpenToolTip();
            if(visuals.hoverScale > 1f)
                evt.Receiver.transform.DOScale(visuals.hoverScale, 0.1f).SetUpdate(true);
            ButtonVisuals.HandleHovered(evt, visuals);
        }

        internal static void UnHovered(Gesture.OnUnhover evt, ButtonVisuals visuals)
        {
            visuals.toolTip?.CloseTip();
            if(visuals.hoverScale > 1f)
                evt.Receiver.transform.DOScale(1f, 0.1f).SetUpdate(true);
            ButtonVisuals.HandleUnhovered(evt, visuals);
        }
    }
}
