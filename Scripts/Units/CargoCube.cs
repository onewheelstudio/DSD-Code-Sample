using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoCube : MonoBehaviour
{
    public HexGame.Resources.ResourceType cargoType;
    private Transform _transform;
    public Transform Transform
    {
        get
        {
            if(_transform == null)
                _transform = this.transform;
            return _transform;
        }
    }

}
