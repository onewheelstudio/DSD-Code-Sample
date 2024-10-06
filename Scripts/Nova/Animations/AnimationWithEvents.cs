using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova.Animations;
using System;

public struct AnimationWithEvents : Nova.IAnimationWithEvents
{
    public event Action<AnimationWithEvents> begin;
    public event Action<AnimationWithEvents> complete;
    public event Action<AnimationWithEvents> end;
    public event Action<AnimationWithEvents> onCanceled;
    public event Action<AnimationWithEvents> onPaused;
    public event Action<AnimationWithEvents> onResumed;

    public void Begin(int currentIteration)
    {
        throw new System.NotImplementedException();
    }

    public void Complete()
    {
        throw new System.NotImplementedException();
    }

    public void End()
    {
        throw new System.NotImplementedException();
    }

    public void OnCanceled()
    {
        throw new System.NotImplementedException();
    }

    public void OnPaused()
    {
        throw new System.NotImplementedException();
    }

    public void OnResumed()
    {
        throw new System.NotImplementedException();
    }

    public void Update(float percentDone)
    {
        throw new System.NotImplementedException();
    }
}
