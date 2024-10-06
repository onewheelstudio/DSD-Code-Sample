using HexGame.Resources;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Cargo Cube List")]
public class CargoCubeList : SerializedScriptableObject
{
    [SerializeField]
    private Dictionary<ResourceType, GameObject> cargoCubes = new Dictionary<ResourceType, GameObject>();
    private Dictionary<ResourceType, ObjectPool<PoolObject>> cargoPools = new Dictionary<ResourceType, ObjectPool<PoolObject>>();

    [SerializeField,Required] private GameObject defaultCube;

    public GameObject GetCargoCube(ResourceType resourceType)
    {
        if(cargoPools.TryGetValue(resourceType, out ObjectPool<PoolObject> pool))
            return pool.PullGameObject();
        else
        {
            //if (!cargoCubes.ContainsKey(resourceType) && cargoCubes.Keys.Count > 0)
            //    return GetCargoCube(cargoCubes.Keys.First());

            if(!cargoCubes.TryGetValue(resourceType, out GameObject prefab))
                prefab = GameObject.Instantiate(defaultCube);

            ObjectPool<PoolObject> newPool = new ObjectPool<PoolObject>(prefab);
            cargoPools.Add(resourceType, newPool);

            return newPool.PullGameObject();
        }
    }
}
