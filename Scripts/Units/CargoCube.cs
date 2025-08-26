using OWS.ObjectPooling;
using System;
using UnityEngine;

public class CargoCube : MonoBehaviour, IPoolable<CargoCube>
{
    public HexGame.Resources.ResourceType cargoType;
    private Transform _transform;
    private Action<CargoCube> returnToPool;
    public int positionIndex = -1;

    public Transform Transform
    {
        get
        {
            if(_transform == null)
                _transform = this.transform;
            return _transform;
        }
    }

    private void OnDisable()
    {
        ReturnToPool();
    }

    public void Initialize(Action<CargoCube> returnAction)
    {
        this.returnToPool = returnAction;

    }

    public void ReturnToPool()
    {
        //invoke and return this object to pool
        returnToPool?.Invoke(this);
    }
}
