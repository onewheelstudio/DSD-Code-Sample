using Nova;
using Nova.Animations;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonIndicator : MonoBehaviour
{
    private UIBlock2D indicator;
    private TextBlock clickText;
    private AnimationHandle animationHandle;
    private Button button;

    private void Awake()
    {
        indicator = this.GetComponent<UIBlock2D>();
        clickText = this.GetComponentInChildren<TextBlock>();
        indicator.Visible = false;
        clickText.Visible = false;
    }

    private void OnEnable()
    {
        BuildMenu.IndicateButton += SetButtonToIndicator;
        BuildingTutorialComplete.buildingTutorialComplete += TurnOffBuildingIndication;
        StateOfTheGame.TutorialSkipped += TurnOffBuildingIndication;
    }

    private void OnDisable()
    {
        if(button != null)
            button.Clicked -= ButtonClicked;
        BuildMenu.IndicateButton -= SetButtonToIndicator;
        BuildingTutorialComplete.buildingTutorialComplete -= TurnOffBuildingIndication;
        StateOfTheGame.TutorialSkipped -= TurnOffBuildingIndication;

        if (animationHandle != null && !animationHandle.IsComplete())
        {
            animationHandle.Complete();
        }
    }

    private void TurnOffBuildingIndication()
    {
        BuildMenu.IndicateButton -= SetButtonToIndicator;
    }

    public static void IndicatorButton(UIBlock block)
    {
        ButtonIndicator buttonIndicator = FindObjectOfType<ButtonIndicator>();
        if (buttonIndicator != null)
        {
            buttonIndicator.SetButtonToIndicator(block);
        }
    }

    [Button]
    public void SetButtonToIndicator(UIBlock block)
    {
        indicator.Visible = true;
        clickText.Visible = true;
        button = block.GetComponent<Button>();
        button.RemoveClickListeners();
        button.Clicked += ButtonClicked;
        this.indicator.transform.SetParent(block.transform);
        this.indicator.Position.Y.Value = block.CalculatedSize.Y.Value * 1.05f;
        this.indicator.Position.X.Value = 0;
        this.indicator.CalculateLayout();
        
        if (!animationHandle.IsComplete())
        {
            animationHandle.Complete();
        }

        ButtonIndicatorAnimation animation = new ButtonIndicatorAnimation()
        {
            startPosition = block.CalculatedSize.Y.Value * 1.05f,
            startColor = indicator.Color,
            endColor = indicator.Color,
            endAlpha = 0f,
            uIBlock = indicator
        };

        animationHandle = animation.Loop(1f, -1);
    }

    private void ButtonClicked()
    {
        button.Clicked -= ButtonClicked;
        indicator.Visible = false;
        clickText.Visible = false;
        if (!animationHandle.IsComplete())
        {
            animationHandle.Complete();
        }
    }

    public struct ButtonIndicatorAnimation : IAnimation
    {
        public float startPosition;
        public Color startColor;
        public Color endColor;
        public float endAlpha;
        public UIBlock uIBlock;

        public void Update(float progress)
        {
            //uIBlock.Size.Value = Vector3.Lerp(startSize, endSize, Mathf.Sin(2*Mathf.PI * progress));
            float time = 0.5f - Mathf.Abs(progress - 0.5f);
            endColor.a = endAlpha;
            uIBlock.Color = Color.Lerp(startColor, endColor, time);
            uIBlock.Position.Y.Value = Mathf.Lerp(startPosition, startPosition  * 1.5f, time);
        }
    }
}
