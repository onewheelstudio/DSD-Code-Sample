using Nova;
using UnityEngine;

[ExecuteAlways]
public class FullScreenUIBlockManualDistance : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The UIBlock whose size and position will adjust automatically to fill the screen")]
    private UIBlock uiBlock = null;
    [Tooltip("The distance from camera to position the UIBlock")]
    public float Distance = 1f;

    private Camera cam = null;

    private void OnEnable()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (uiBlock == null)
        {
            return;
        }

        // Match camera rotation so the two are axis-aligned
        uiBlock.transform.rotation = cam.transform.rotation;

        // Position the uiBlock at the desired distance from camera
        uiBlock.transform.position = cam.transform.position + cam.transform.forward * Distance;

        // Adjust the size of the uiBlock to fill the viewport
        float height = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * Distance * 2f;

        Vector3 unscaledSize = new Vector3(height * cam.aspect, height, 0f);

        // This assumes uniform scale
        Vector3 scaledSize = unscaledSize / uiBlock.transform.localScale.x;

        uiBlock.Size = scaledSize;
    }
}