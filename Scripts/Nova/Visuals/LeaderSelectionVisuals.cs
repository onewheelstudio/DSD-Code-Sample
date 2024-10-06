using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using System;
using DG.Tweening;

public class LeaderSelectionVisuals : ItemVisuals
{
    public UIBlock2D parentBlock;
    public Color selectedColor = new Color(1f, 0.7f, 0.27f);

    public void ToggleSelection(bool selected)
    {
        if (selected)
        {
            parentBlock.Border.Enabled = true;
            parentBlock.Border.Color = selectedColor;
        }
        else
        {
            parentBlock.Border.Enabled = true;
            parentBlock.Border.Color = Color.black;
        }
    }

    public static void OnHover(Gesture.OnHover gestures, LeaderSelectionVisuals visuals)
    {
        visuals.parentBlock.transform.DOScale(1.02f, 0.2f);
    }

    internal static void OnUnHover(Gesture.OnUnhover evt, LeaderSelectionVisuals visuals)
    {
        visuals.parentBlock.transform.DOScale(1f, 0.15f);
    }

    //public Tween DoScale(float endValue, float duration)
    //{
    //    return DOTween.To(() => this.alpha, x => this.SetAlpha(x), endValue, duration);
    //}
}
