using Nova;
using UnityEngine;
using Sirenix.OdinInspector;

[ExecuteAlways]
public class FullScreenUIBlockAutoDistance : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The UIBlock whose size and position will adjust automatically to fill the screen")]
    [Required]
    private UIBlock uiBlock = null;

    [SerializeField, Required]
    private Camera cam;

    void Update()
    {
        if (uiBlock == null || cam == null)
        {
            return;
        }

        // Match camera rotation so the two are axis-aligned
        uiBlock.transform.rotation = cam.transform.rotation;

        // Set the size of the UIBlock root
        Vector2 unscaledSize = new Vector2(cam.pixelWidth, cam.pixelHeight);
        uiBlock.Size.XY.Value = unscaledSize;

        // This assumes a uniform local scale on root
        Vector2 scaledSize = unscaledSize * uiBlock.transform.localScale.x;

        // Positioning below assumes center alignment, so just ensure that's the configuration here
        uiBlock.Alignment = Alignment.Center;

        // Position the root at the distance such that it fills the camera viewport
        float distanceFromCamera = scaledSize.y / (2f * Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f));
        uiBlock.Position.Value = cam.transform.position + cam.transform.forward * distanceFromCamera;
    }
}