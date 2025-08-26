using Nova;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class ProgressIndicator : WindowPopup
{
    [SerializeField] private TextBlock progressMessage;
    [SerializeField] private UIBlock2D progressBar;
    private AnimationHandle animationHandle;
    private DayNightManager dnm;

    private void Start()
    {
        CloseWindow();
    }

    public void StartProgress(string message, Action stopAction)
    {
        OpenWindow();
        progressMessage.Text = message;
        StartAnimation(stopAction);
    }

    [Button]
    private void StartAnimation(Action stopAction)
    {
        dnm ??= FindFirstObjectByType<DayNightManager>();
        dnm?.SetPause(true, false);

        if (!animationHandle.IsComplete())
        {
            animationHandle.Complete();
        }

        ProgressAnimation animation = new ProgressAnimation()
        {
            block = progressBar,
            startSize = 0f,
            endSize = 1f
        };

        animationHandle = animation.Loop(2f, -1);
        stopAction += StopAnimation;
    }

    public void StopProgress()
    {
        StopAnimation();
    }

    private void StopAnimation()
    {
        if(animationHandle == null)
            return;

        if (!animationHandle.IsComplete())
        {
            animationHandle.Complete();
        }
        dnm ??= FindFirstObjectByType<DayNightManager>();
        dnm?.SetPause(false, false);
        CloseWindow();
    }

    public struct ProgressAnimation : IAnimation
    {
        public UIBlock2D block;
        public float startSize;
        public float endSize;

        public void Update(float progress)
        {
            float time = (0.5f - Mathf.Abs(progress - 0.5f)) * 2f;
            block.Size.X.Percent = Mathf.Lerp(startSize, endSize, time);
        }
    }
}
