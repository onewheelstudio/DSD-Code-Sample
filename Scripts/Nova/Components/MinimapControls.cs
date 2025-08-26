using Nova;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

namespace NovaSamples.UIControls
{
    /// <summary>
    /// A UI control which reacts to user input and fires click events
    /// </summary>
    public class MinimapControls : UIControl<MiniMapVisuals>
    {
        [Tooltip("Event fired when the button is Clicked.")]
        public System.Action Clicked;
        [SerializeField] private Transform canvas;
        [SerializeField] private Camera overlayCamera;
        public static event System.Action<Vector3> minimapClicked;

        private void OnEnable()
        {
            // Subscribe to desired events
            View.UIBlock.AddGestureHandler<Gesture.OnClick, MiniMapVisuals>(HandleClicked);
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            View.UIBlock.RemoveGestureHandler<Gesture.OnClick, MiniMapVisuals>(HandleClicked);
        }

        /// <summary>
        /// Fire the Unity event on Click.
        /// </summary>
        /// <param name="evt">The click event data.</param>
        /// <param name="visuals">The buttons visuals which received the click.</param>
        private void HandleClicked(Gesture.OnClick evt, MiniMapVisuals visuals)
        {
            Ray ray = evt.Interaction.Ray;
            float distance = canvas.position.z - overlayCamera.transform.position.z;
            Plane plane = new Plane(-this.transform.forward, distance);
            if (plane.Raycast(ray, out float dist))
            {
                Vector3 offset = ray.GetPoint(dist) - this.transform.position;
                if (float.IsNaN(evt.Receiver.Size.X.Value)) //one of the values is nan if using %
                  offset = (offset * 2f / canvas.transform.localScale.x) / evt.Receiver.Size.Y.Value;
                else
                  offset = (offset * 2f / canvas.transform.localScale.x) / evt.Receiver.Size.X.Value;

                if(offset.sqrMagnitude < 1f)
                    minimapClicked?.Invoke(offset);
            }
        }
    }
}
