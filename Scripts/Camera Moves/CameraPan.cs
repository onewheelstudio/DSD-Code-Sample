using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

public class CameraPan : MonoBehaviour
{
    [SerializeField] private float panTime = 0.5f;
    [SerializeField] private Vector3 move;
    private Vector3 startLocation;
    private Quaternion startRotation;
    [SerializeField] private Transform lookAtTarget;
    private CameraMovement cameraMovement;

    private void Start()
    {
        cameraMovement = GetComponent<CameraMovement>();
    }

    [Button]
    private void PanCamera()
    {
        startLocation = transform.position;
        startRotation = transform.rotation;
        StartCoroutine(DoPan());
    }

    private IEnumerator DoPan()
    {
        cameraMovement.enabled = false;
        float elapsedTime = 0;
        Vector3 endLocation = startLocation + this.transform.right * move.x + this.transform.up * move.y + this.transform.forward * move.z;
        while (elapsedTime < panTime)
        {
            transform.position = Vector3.Lerp(startLocation, endLocation, elapsedTime / panTime);
            if(lookAtTarget != null)
                transform.LookAt(lookAtTarget);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);

        ReturnToStart();
        cameraMovement.enabled = true;
    }

    [Button]
    private void ReturnToStart()
    {
        this.transform.position = startLocation;
        this.transform.rotation = startRotation;
    }
}
