using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatReconBehavior : UnitBehavior
{
    [SerializeField]private List<ResourceAmount> cost = new List<ResourceAmount>();
    private UnitStorageBehavior usb;
    [SerializeField] private GameObject reconPrefab;
    private static ObjectPool<PoolObject> reconPool;

    private void Awake()
    {
        usb = GetComponent<UnitStorageBehavior>();
        if(reconPool == null && reconPrefab != null)
            reconPool = new ObjectPool<PoolObject>(reconPrefab);
    }

    public override void StartBehavior()
    {
        _isFunctional = true;
    }

    public override void StopBehavior()
    {
        _isFunctional = false;
    }

    private bool CanFire()
    {
        return usb.HasAllResources(cost);
    }

    [Button]
    private void Fire(Hex3 target)
    {
        if (!CanFire())
            return;

        if ((target - this.transform.position.ToHex3()).Max() > GetStat(Stat.maxRange))
        {
            MessagePanel.ShowMessage("Recon target out of range.", this.gameObject);
            return;
        }

        Vector3 position = target.ToVector3() + Vector3.up * 20f;
        GameObject recon = reconPool.PullGameObject(position, Quaternion.identity);
    }
}
