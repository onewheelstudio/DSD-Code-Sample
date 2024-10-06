using Nova;
using System;
using UnityEngine;

namespace Nova.Animations
{
    public struct ClipMaskAlphaAnimation : IAnimationWithEvents
    {
        public ClipMask clipMask;
        public float targetAlpha;

        public event Action<ClipMaskAlphaAnimation> begin;
        public event Action<ClipMaskAlphaAnimation> complete;
        public event Action<ClipMaskAlphaAnimation> end;
        public event Action<ClipMaskAlphaAnimation> onCanceled;
        public event Action<ClipMaskAlphaAnimation> onPaused;
        public event Action<ClipMaskAlphaAnimation> onResumed;

        public ClipMaskAlphaAnimation(ClipMask clipMask, float startAlpha)
        {
            this.clipMask = clipMask;
            this.targetAlpha = startAlpha;
            begin = null;
            complete = null;
            end = null;
            onCanceled = null;
            onPaused = null;
            onResumed = null;
        }

        public void Begin(int currentIteration)
        {
        }

        public void Complete()
        {
        }

        public void End()
        {
        }

        public void OnCanceled()
        {
        }

        public void OnPaused()
        {
        }

        public void OnResumed()
        {
        }

        public void Update(float percentDone)
        {
            clipMask.SetAlpha(Mathf.Lerp(clipMask.Tint.a, targetAlpha, percentDone));
        }
    }
}
