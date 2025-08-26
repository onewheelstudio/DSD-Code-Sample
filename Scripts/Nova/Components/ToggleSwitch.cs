using Nova;
using UnityEngine;
using UnityEngine.Events;
using System;
using NovaSamples.UIControls;

namespace OWS.Nova
{
    /// <summary>
    /// A UI control which reacts to user input and flips an underlying bool to track a <see cref="ToggledOn"/> state as it is Clicked.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class ToggleSwitch : UIControl<ToggleSwitchVisuals>
    {
        [Tooltip("Event invoked when the toggle state changes. Provides the ToggledOn state.")]
        public UnityEvent<bool> OnToggled = null;
        public Action<ToggleSwitch, bool> Toggled;

        [Tooltip("The toggle state of this toggle control")]
        [SerializeField]
        private bool toggledOn = false;

        /// <summary>
        /// The state of this toggle control
        /// </summary>
        public bool ToggledOn
        {
            get => toggledOn;
            set
            {
                if (value == toggledOn)
                {
                    return;
                }

                toggledOn = value;

                UpdateToggleIndicator();

                OnToggled?.Invoke(toggledOn);
                Toggled?.Invoke(this,toggledOn);

                SFXManager.PlaySFX(SFXType.click);
            }
        }

        public void SetValueWithOutCallback(bool isOn)
        {
            this.toggledOn = isOn;
            UpdateToggleIndicator();
        }

        private void OnEnable()
        {
            // Subscribe to desired events
            View.UIBlock.AddGestureHandler<Gesture.OnClick, ToggleSwitchVisuals>(HandleClicked);
            View.UIBlock.AddGestureHandler<Gesture.OnHover, ToggleSwitchVisuals>(ToggleSwitchVisuals.HandleHovered);
            View.UIBlock.AddGestureHandler<Gesture.OnUnhover, ToggleSwitchVisuals>(ToggleSwitchVisuals.HandleUnhovered);
            View.UIBlock.AddGestureHandler<Gesture.OnPress, ToggleSwitchVisuals>(ToggleSwitchVisuals.HandlePressed);
            View.UIBlock.AddGestureHandler<Gesture.OnRelease, ToggleSwitchVisuals>(ToggleSwitchVisuals.HandleReleased);
            View.UIBlock.AddGestureHandler<Gesture.OnCancel, ToggleSwitchVisuals>(ToggleSwitchVisuals.HandlePressCanceled);

            UpdateToggleIndicator();
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            View.UIBlock.RemoveGestureHandler<Gesture.OnClick, ToggleSwitchVisuals>(HandleClicked);
            View.UIBlock.RemoveGestureHandler<Gesture.OnHover, ToggleSwitchVisuals>(ToggleSwitchVisuals.HandleHovered);
            View.UIBlock.RemoveGestureHandler<Gesture.OnUnhover, ToggleSwitchVisuals>(ToggleSwitchVisuals.HandleUnhovered);
            View.UIBlock.RemoveGestureHandler<Gesture.OnPress, ToggleSwitchVisuals>(ToggleSwitchVisuals.HandlePressed);
            View.UIBlock.RemoveGestureHandler<Gesture.OnRelease, ToggleSwitchVisuals>(ToggleSwitchVisuals.HandleReleased);
            View.UIBlock.RemoveGestureHandler<Gesture.OnCancel, ToggleSwitchVisuals>(ToggleSwitchVisuals.HandlePressCanceled);
        }

        /// <summary>
        /// Flip the toggle state on click.
        /// </summary>
        /// <param name="evt">The click event data.</param>
        /// <param name="visuals">The toggle visuals associated with the click event.</param>
        private void HandleClicked(Gesture.OnClick evt, ToggleSwitchVisuals visuals) => ToggledOn = !ToggledOn;

        /// <summary>
        /// Update the visual toggle indicate to match the underlying <see cref="ToggledOn"/> state.
        /// </summary>
        private void UpdateToggleIndicator()
        {
            if (!(View.Visuals is ToggleSwitchVisuals visuals) || visuals.switchIndicator == null)
            {
                return;
            }

            visuals.switchIndicator.Alignment = ToggledOn ? Alignment.Right : Alignment.Left;
            visuals.switchIndicator.Shadow.Offset = ToggledOn ? new Vector2(-2f, 0f) : new Vector2(2f, 0f);
            visuals.Background.Color = ToggledOn ? visuals.onColor : visuals.offColor;
        }

        public void RemoveAllListeners()
        {
            OnToggled.RemoveAllListeners();
            Toggled = null;
        }
    }
}
