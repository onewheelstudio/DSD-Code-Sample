using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotateAround : MonoBehaviour
{
    [SerializeField] private float rotateTime = 0.5f;
    [SerializeField] private float rotateSpeed = 360;
    [SerializeField] private Vector3 axis = Vector3.up;
    private Vector3 startLocation;
    private Quaternion startRotation;
    [SerializeField] private Transform lookAtTarget;
    private CameraMovement cameraMovement;

    private void Start()
    {
        cameraMovement = GetComponent<CameraMovement>();
    }

    [Button]
    private void RotateCamera()
    {
        startLocation = transform.position;
        startRotation = transform.rotation;
        StartCoroutine(DoRotate());
    }

    private IEnumerator DoRotate()
    {
        cameraMovement.enabled = false;
        float elapsedTime = 0;
        while (elapsedTime < rotateTime)
        {
            transform.RotateAround(lookAtTarget.position, axis, rotateSpeed * Time.deltaTime / rotateTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);
        ReturnToStart();
        cameraMovement.enabled = true;
    }

    private void ReturnToStart()
    {
        this.transform.position = startLocation;
        this.transform.rotation = startRotation;
    }
}
