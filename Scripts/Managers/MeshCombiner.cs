using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using HexGame.Resources;
using System;

[Manageable]
public class MeshCombiner : MonoBehaviour
{
    public static event Action hexTileCombined;

    MeshCollider mc;
    MeshFilter mf;
    MeshRenderer mr;

    [SerializeField]
    private Material defaultMaterial;

    [SerializeField]
    private LayerMask groundLayer;
    [SerializeField]
    private LayerMask treeLayer;
    [SerializeField]
    private LayerMask mountainLayer;
    [SerializeField]
    private LayerMask decorationLayer;

    private List<GameObject> newObjectsToCombine;

    private void Awake()
    {
        this.transform.position = Vector3.zero;
        mf = this.gameObject.AddComponent<MeshFilter>();
        mf.mesh = new Mesh();
        mr = this.gameObject.AddComponent<MeshRenderer>();
        mc = this.gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = mf.mesh;
        mr.material = defaultMaterial;
    }

    private void OnEnable()
    {
        HexTile.NewHexTile += HexTileAdded;
        HexTileManager.fillComplete += Combine;
        LandmassCreator.generationComplete += Combine;
    }

    private void OnDisable()
    {
        HexTile.NewHexTile -= HexTileAdded;
        HexTileManager.fillComplete -= Combine;
        LandmassCreator.generationComplete -= Combine;
    }

    private void HexTileAdded(HexTile obj)
    {
        obj.transform.SetParent(this.transform);
    }

    void Combine()
    {

        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        List<MeshFilter> meshesToCombine = new List<MeshFilter>();
        foreach (var mf in meshFilters)
        {
            if (treeLayer == (treeLayer | 1 << mf.gameObject.layer))
                continue;
            meshesToCombine.Add(mf);
        }
        meshFilters = meshesToCombine.ToArray();

        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);
            CleanUpTileObject(meshFilters[i].gameObject);
        }

        mf.mesh = new Mesh();
        mf.mesh.MarkDynamic();
        mf.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; //allow up to 4 billion verts
        mf.mesh.CombineMeshes(combine);
        transform.gameObject.SetActive(true);

        mc.sharedMesh = mf.mesh;
    }

    private void CleanUpTileObject(GameObject gameObject)
    {
        //this is ugly
        foreach (Transform child in this.transform)
        {
            foreach (Transform grandChild in child)
            {
                if (treeLayer == (treeLayer | 1 << grandChild.gameObject.layer))
                    continue;

                if (grandChild.gameObject != gameObject)
                    grandChild.gameObject.SetActive(false);
            }
        }
    }
}
