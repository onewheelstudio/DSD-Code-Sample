using Nova;
using UnityEngine;
using UnityEngine.Events;
using System;

namespace NovaSamples.UIControls
{
    /// <summary>
    /// A UI control which reacts to user input and flips an underlying bool to track a <see cref="ToggledOn"/> state as it is clicked.
    /// </summary>
    public class Toggle : UIControl<ToggleVisuals>
    {
        [Tooltip("Event invoked when the toggle state changes. Provides the ToggledOn state.")]
        public UnityEvent<bool> OnToggled = null;
        public Action<Toggle,bool> toggled;

        [Tooltip("The toggle state of this toggle control")]
        [SerializeField]
        private bool toggledOn = false;

        [SerializeField] private ToggleGroup toggleGroup;

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
                toggled?.Invoke(this,toggledOn);
            }
        }

        public void SetToggleWithOutCallback(bool isOn)
        {
            this.toggledOn = isOn;
            UpdateToggleIndicator();
        }

        private void OnEnable()
        {
            // Subscribe to desired events
            View.UIBlock.AddGestureHandler<Gesture.OnClick, ToggleVisuals>(HandleClicked);
            View.UIBlock.AddGestureHandler<Gesture.OnHover, ToggleVisuals>(HandleHover);
            View.UIBlock.AddGestureHandler<Gesture.OnUnhover, ToggleVisuals>(HandleUnHover);
            View.UIBlock.AddGestureHandler<Gesture.OnPress, ToggleVisuals>(ToggleVisuals.HandlePressed);
            View.UIBlock.AddGestureHandler<Gesture.OnRelease, ToggleVisuals>(ToggleVisuals.HandleReleased);
            View.UIBlock.AddGestureHandler<Gesture.OnCancel, ToggleVisuals>(ToggleVisuals.HandlePressCanceled);

            UpdateToggleIndicator();

            if (toggleGroup != null)
                toggleGroup.RegisterToggle(this);
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            View.UIBlock.RemoveGestureHandler<Gesture.OnClick, ToggleVisuals>(HandleClicked);
            View.UIBlock.RemoveGestureHandler<Gesture.OnHover, ToggleVisuals>(HandleHover);
            View.UIBlock.RemoveGestureHandler<Gesture.OnUnhover, ToggleVisuals>(HandleUnHover);
            View.UIBlock.RemoveGestureHandler<Gesture.OnPress, ToggleVisuals>(ToggleVisuals.HandlePressed);
            View.UIBlock.RemoveGestureHandler<Gesture.OnRelease, ToggleVisuals>(ToggleVisuals.HandleReleased);
            View.UIBlock.RemoveGestureHandler<Gesture.OnCancel, ToggleVisuals>(ToggleVisuals.HandlePressCanceled);

            if (toggleGroup != null)
                toggleGroup.UnRegisterToggle(this);
        }

        /// <summary>
        /// Flip the toggle state on click.
        /// </summary>
        /// <param name="evt">The click event data.</param>
        /// <param name="visuals">The toggle visuals associated with the click event.</param>
        private void HandleClicked(Gesture.OnClick evt, ToggleVisuals visuals) => ToggledOn = !ToggledOn;

        /// <summary>
        /// Update the visual toggle indicate to match the underlying <see cref="ToggledOn"/> state.
        /// </summary>
        private void UpdateToggleIndicator()
        {
            if (!(View.Visuals is ToggleVisuals visuals) || visuals.IsOnIndicator == null)
            {
                return;
            }

            visuals.IsOnIndicator.gameObject.SetActive(toggledOn);
        }

        public void RemoveAllListeners()
        {
            OnToggled.RemoveAllListeners();
            toggled = null;
        }

        private void HandleUnHover(Gesture.OnUnhover evt, ButtonVisuals visuals)
        {
            ToggleVisuals.UnHovered(evt, visuals);
        }

        private void HandleHover(Gesture.OnHover evt, ButtonVisuals visuals)
        {
            ToggleVisuals.Hovered(evt, visuals);
        }
    }
}
