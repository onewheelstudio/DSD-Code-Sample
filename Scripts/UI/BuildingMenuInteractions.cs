using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using NovaSamples.UIControls;
using System;

public class BuildingMenuInteractions : UIControl<BuildingMenuVisuals>
{
    private BuildingSelectWindow lastMenuOpened;

    private void OnEnable()
    {
        BuildingSelectWindow.buildingSelectOpened += MenuOpened;

        // Subscribe to desired events
        View.UIBlock.AddGestureHandler<Gesture.OnHover, BuildingMenuVisuals>(HandleHover);
        View.UIBlock.AddGestureHandler<Gesture.OnUnhover, BuildingMenuVisuals>(HandleUnHover);
    }

    private void OnDisable()
    {
        BuildingSelectWindow.buildingSelectOpened -= MenuOpened;

        // Unsubscribe from events
        View.UIBlock.RemoveGestureHandler<Gesture.OnHover, BuildingMenuVisuals>(HandleHover);
        View.UIBlock.RemoveGestureHandler<Gesture.OnUnhover, BuildingMenuVisuals>(HandleUnHover);
    }

    private void MenuOpened(BuildingSelectWindow window)
    {
        lastMenuOpened = window;
    }

    private void HandleUnHover(Gesture.OnUnhover evt, BuildingMenuVisuals target)
    {

    }

    private void HandleHover(Gesture.OnHover evt, BuildingMenuVisuals target)
    {
        if (lastMenuOpened != null)
            lastMenuOpened.OpenWindow();
    }
}
