using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    protected static Camera _camera;

    public void Update()
    {
        if( _camera == null )
            _camera = Camera.main;

        Vector3 direction = this.transform.position - _camera.transform.position;
        this.transform.LookAt(this.transform.position + direction);
    }
}
