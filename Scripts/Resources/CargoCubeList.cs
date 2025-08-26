using HexGame.Resources;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Cargo Cube List")]
public class CargoCubeList : SerializedScriptableObject
{
    [SerializeField]
    private Dictionary<ResourceType, GameObject> cargoCubes = new Dictionary<ResourceType, GameObject>();
    [NonSerialized] private Dictionary<ResourceType, ObjectPool<CargoCube>> cargoPools = new Dictionary<ResourceType, ObjectPool<CargoCube>>();

    [SerializeField,Required] private GameObject defaultCube;

    public CargoCube GetCargoCube(ResourceType resourceType)
    {
        if(cargoPools.TryGetValue(resourceType, out ObjectPool<CargoCube> pool))
            return pool.Pull();
        else
        {
            if(!cargoCubes.TryGetValue(resourceType, out GameObject prefab))
                prefab = GameObject.Instantiate(defaultCube);

            ObjectPool<CargoCube> newPool = new ObjectPool<CargoCube>(prefab);
            newPool.ToggleObjects = false;
            cargoPools.Add(resourceType, newPool);

            return newPool.Pull();
        }
    }

    public void ClearCargoPools()
    {
        cargoPools.Clear();
    }
}
