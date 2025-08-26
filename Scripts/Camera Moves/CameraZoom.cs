using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    private Camera _camera;
    [SerializeField] private float zoomDistance = 5f;
    [SerializeField] private float zoomTime = 0.5f;
    private Vector3 startLocation;
    private Quaternion startRotation;
    [SerializeField] private Transform lookAtTarget;
    private CameraMovement cameraMovement;

    private void Start()
    {
        _camera = GetComponentInChildren<Camera>();
        cameraMovement = GetComponent<CameraMovement>();
    }

    [Button]
    private void ZoomCamera()
    {
        startLocation = _camera.transform.localPosition;
        startRotation = _camera.transform.localRotation;
        StartCoroutine(DoZoom());
    }

    private IEnumerator DoZoom()
    {
        cameraMovement.enabled = false;
        float elapsedTime = 0;
        while (elapsedTime < zoomTime)
        {
            _camera.transform.localPosition = Vector3.Lerp(startLocation, startLocation + _camera.transform.forward * zoomDistance, elapsedTime / zoomTime);
            if (lookAtTarget != null)
                _camera.transform.LookAt(lookAtTarget);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
        ReturnToStart();
        cameraMovement.enabled = true;
    }

    private void ReturnToStart()
    {
        _camera.transform.localPosition = startLocation;
        _camera.transform.localRotation = startRotation;
    }
}
