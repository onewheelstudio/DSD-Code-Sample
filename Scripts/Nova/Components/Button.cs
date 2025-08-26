using Nova;
using UnityEngine;
using UnityEngine.Events;

namespace NovaSamples.UIControls
{
    /// <summary>
    /// A UI control which reacts to user input and fires click events
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class Button : UIControl<ButtonVisuals>
    {
        [Tooltip("Event fired when the button is Clicked.")]
        public UnityEvent OnClicked = null;
        public event System.Action Clicked;
        public event System.Action DoubleClicked;
        private float lastClickTime = 0;
        private float doubleClickTime = 0.5f;

        public UnityEvent OnRelease = null;
        public event System.Action onRelease;

        public UnityEvent OnHover = null;
        public event System.Action hover;
        
        public UnityEvent OnUnHover = null;
        public event System.Action unhover;

        private Interactable interactable;
        private ClipMask clipMask;

        private void OnEnable()
        {
            // Subscribe to desired events
            View.UIBlock.AddGestureHandler<Gesture.OnClick, ButtonVisuals>(HandleClicked);
            View.UIBlock.AddGestureHandler<Gesture.OnHover, ButtonVisuals>(HandleHover);
            View.UIBlock.AddGestureHandler<Gesture.OnUnhover, ButtonVisuals>(HandleUnHover);
            View.UIBlock.AddGestureHandler<Gesture.OnPress, ButtonVisuals>(ButtonVisuals.HandlePressed);
            View.UIBlock.AddGestureHandler<Gesture.OnRelease, ButtonVisuals>(ButtonVisuals.HandleReleased);
            View.UIBlock.AddGestureHandler<Gesture.OnCancel, ButtonVisuals>(ButtonVisuals.HandlePressCanceled);
        }

        private void OnDisable()
        {
            //reset in case of reuse of the button (tips)
            this.transform.localScale = Vector3.one;
            // Unsubscribe from events
            View.UIBlock.RemoveGestureHandler<Gesture.OnClick, ButtonVisuals>(HandleClicked);
            View.UIBlock.RemoveGestureHandler<Gesture.OnHover, ButtonVisuals>(HandleHover);
            View.UIBlock.RemoveGestureHandler<Gesture.OnUnhover, ButtonVisuals>(HandleUnHover);
            View.UIBlock.RemoveGestureHandler<Gesture.OnPress, ButtonVisuals>(ButtonVisuals.HandlePressed);
            View.UIBlock.RemoveGestureHandler<Gesture.OnRelease, ButtonVisuals>(ButtonVisuals.HandleReleased);
            View.UIBlock.RemoveGestureHandler<Gesture.OnCancel, ButtonVisuals>(ButtonVisuals.HandlePressCanceled);
        }

        /// <summary>
        /// Fire the Unity event on Click.
        /// </summary>
        /// <param name="evt">The click event data.</param>
        /// <param name="visuals">The buttons visuals which received the click.</param>
        private void HandleClicked(Gesture.OnClick evt, ButtonVisuals visuals)
        {
            OnClicked?.Invoke();
            Clicked?.Invoke();
            SFXManager.PlaySFX(SFXType.click);
            if (Time.time - lastClickTime < doubleClickTime)
            {
                DoubleClicked?.Invoke();
            }
            lastClickTime = Time.time;
        }
        
        private void HandleUnHover(Gesture.OnUnhover evt, ButtonVisuals visuals)
        {
            OnUnHover?.Invoke();
            unhover?.Invoke();
            ButtonVisuals.UnHovered(evt, visuals);
        }

        private void HandleHover(Gesture.OnHover evt, ButtonVisuals visuals)
        {
            OnHover?.Invoke();
            hover?.Invoke();
            ButtonVisuals.Hovered(evt, visuals);
        }

        public void RemoveAllListeners()
        {
            //did this work??
            Clicked = null;
            DoubleClicked = null;
            onRelease = null;
            hover = null;
            unhover = null;
        }

        public void RemoveClickListeners()
        {
            Clicked = null;
        }

        public void RemoveDoubleClickListners()
        {
            DoubleClicked = null;
        }

        public void Hide()
        {
            ButtonVisuals visuals = View.Visuals as ButtonVisuals;
            visuals.Background.Visible = false;

            interactable ??= this.gameObject.GetComponent<Interactable>();
            interactable.enabled = false;
        }

        public void UnHide()
        {
            ButtonVisuals visuals = View.Visuals as ButtonVisuals;
            visuals.Background.Visible = true;

            interactable ??= this.gameObject.GetComponent<Interactable>();
            interactable.enabled = true;
        }

        public void SetInteractable(bool interactable)
        {
            this.interactable ??= this.gameObject.GetComponent<Interactable>();
            this.interactable.enabled = interactable;

            if (this.gameObject.TryGetComponent(out ClipMask clipmask))
                this.clipMask = clipmask;
            else
                this.clipMask = this.gameObject.AddComponent<ClipMask>();

            Color tint = clipMask.Tint;
            tint.a = interactable ? 1 : 0.25f;
            clipMask.Tint = tint;
        }
    }
}
