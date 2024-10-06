using Nova;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using NovaSamples.UIControls;

namespace OWS.Nova
{
    /// <summary>
    /// An <see cref="ItemVisuals"/> for a simple toggle control. Inherits from <see cref="ButtonVisuals"/>.
    /// </summary>
    [System.Serializable]
    [MovedFrom(false, null, "Assembly-CSharp")]
    public class ToggleSwitchVisuals : ButtonVisuals
    {
        [Header("Toggle Fields")]
        [Tooltip("The UIBlock used to indicate the underlying \"Toggled On\" or \"Toggled Off\" state.")]
        public UIBlock2D switchIndicator;
        public TextBlock onText;
        public TextBlock offText;

        [Header("Colors")]
        public Color onColor = Color.green;
        public Color offColor = Color.red;
    }
}