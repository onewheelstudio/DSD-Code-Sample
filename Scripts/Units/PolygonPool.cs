using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OWS.ObjectPooling;
using Shapes;
using System;

public class PolygonPool : MonoBehaviour, IPoolable<Polygon>
{
    public event Action<Polygon> returnAction;
    private Polygon polygon;
    public void Initialize(Action<Polygon> returnAction)
    {
        if(polygon == null)
            polygon = this.GetComponent<Polygon>();
        this.returnAction = returnAction;
    }

    public void ReturnToPool()
    {
        returnAction?.Invoke(polygon);
    }
}
    
