using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nova
{
    public class PCInputManager : InputManager
    {
        public LayerMask UILayerMask;
        /// <summary>
        /// The controlID for mouse point events
        /// </summary>
        public const uint MousePointerControlID = 1;

        /// <summary>
        /// The controlID for mouse wheel events
        /// </summary>
        public const uint ScrollWheelControlID = 2;

        /// <summary>
        /// To store the button states of both the left and right mouse buttons.
        /// </summary>
        private static readonly InputData Data = new InputData();

        [Tooltip("Inverts the mouse wheel scroll direction.")]
        public bool InvertScrolling = true;


        /// <summary>
        /// The camera used to convert a mouse position into a world ray
        /// </summary>
        [SerializeField,Required] private Camera Cam;
        private static PCInputManager instance;
        public static Camera uiCamera
        {
            get
            {
                if (!instance)
                    instance = FindObjectOfType<PCInputManager>();
                
                return instance.Cam;
            }
        }


        public override bool TryGetRay(uint controlID, out Ray ray)
        {
            if (controlID != MousePointerControlID)
            {
                ray = default;
                return false;
            }

            ray = Cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            return true;
        }

        private void Update()
        {
            if (Mouse.current == null)
            {
                // Nothing to do, no mouse device detected
                return;
            }

            // Get the current world-space ray of the mouse
            Ray mouseRay = Cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            // Get the current scroll wheel delta
            Vector2 mouseScrollDelta = Mouse.current.scroll.ReadValue();

            // Check if there is any scrolling this frame
            if (mouseScrollDelta != Vector2.zero)
            {
                // Invert scrolling for a mouse-type experience,
                // otherwise will scroll track-pad style.
                if (InvertScrolling)
                {
                    mouseScrollDelta.y *= -1f;
                }

                // Create a new Interaction.Update from the mouse ray and scroll wheel control id
                Interaction.Update scrollInteraction = new Interaction.Update(mouseRay, ScrollWheelControlID);

                // Feed the scroll update and scroll delta into Nova's Interaction APIs
                Interaction.Scroll(scrollInteraction, mouseScrollDelta);
            }

            // Store the button states for left/right mouse buttons
            Data.PrimaryButtonDown = Mouse.current.leftButton.isPressed;
            Data.SecondaryButtonDown = Mouse.current.rightButton.isPressed;

            // Create a new Interaction.Update from the mouse ray and pointer control id
            Interaction.Update pointInteraction = new Interaction.Update(mouseRay, MousePointerControlID, userData: Data);

            // Feed the pointer update and pressed state to Nova's Interaction APIs
            Interaction.Point(pointInteraction, Data.AnyButtonPressed);
        }

        public static bool MouseOverVisibleUIObject()
        {
            // This list could be cached - it's only not for 
            // the sake of clarity in this example
            List<UIBlockHit> hitsToPopuplate = new List<UIBlockHit>();

            Ray ray = uiCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            // Get the list of all UIBlocks intersected by the given ray
            Interaction.RaycastAll(ray, hitsToPopuplate, Mathf.Infinity, LayerMask.GetMask("UI"));

            bool foundVisible = false;

            // Loop over intersected UIBlocks 
            for (int i = 0; i < hitsToPopuplate.Count; ++i)
            {
                if (!hitsToPopuplate[i].UIBlock.gameObject.activeInHierarchy)
                    continue;

                if (!hitsToPopuplate[i].UIBlock.Visible)
                    continue;

                if (AreParentsVisible(hitsToPopuplate[i].UIBlock.transform))
                    return true;
            }

            return foundVisible;
        }

        private static bool IsTransparent(Transform block)
        {
            return block.GetComponentsInParent<ClipMask>(true).Any(c => c.Tint.a == 0);
        }

        private static bool AreParentsVisible(Transform block)
        {
            ClipMask[] parentBlocks = block.GetComponentsInParent<ClipMask>();

            if(parentBlocks.Length == 0)
                return true;
            else 
                return parentBlocks[^1].Tint.a > 0.01f;
        }

    }


}

