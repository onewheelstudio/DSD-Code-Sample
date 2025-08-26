using HexGame.Units;
using OWS.ObjectPooling;
using System;
using UnityEngine;

public class RepairIcon : MonoBehaviour, IPoolable<RepairIcon>
{
    [SerializeField] private GameObject iconParent;
    private PlayerUnit urgentUnit;
    private Action<RepairIcon> returnAction;

    private void OnDisable()
    {
        ReturnToPool();
    }

    public void Initialize(Action<RepairIcon> returnAction)
    {
        this.returnAction = returnAction;
    }

    public void ReturnToPool()
    {
        returnAction?.Invoke(this);
    }
}
