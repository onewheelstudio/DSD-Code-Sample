using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    protected static Camera _camera;
    protected static Transform cameraTransform;
    private Transform Transform;

    private void Awake()
    {
        this.Transform = this.transform;
    }

    public void Update()
    {
        if( _camera == null )
        {
            _camera = Camera.main;
            cameraTransform = _camera.transform;
        }

        if (!IsPositionVisible(_camera))
            return;

        Vector3 direction = Transform.position - cameraTransform.position;
        this.transform.LookAt(Transform.position + direction);
    }

    private bool IsPositionVisible(Camera cam)
    {
        Vector3 viewportPoint = cam.WorldToViewportPoint(Transform.position);

        // Check if the point is in front of the camera
        if (viewportPoint.z < 0)
            return false;

        // Check if the point is within the camera's viewport rectangle (0 to 1 in x and y)
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1;
    }
}
