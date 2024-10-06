using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using HexGame.Units;

public class PlaceholderCreater : OdinEditorWindow
{
    [MenuItem("Tools/Model Placeholder Creator")]
    private static void OpenWindow()
    {
        GetWindow<PlaceholderCreater>().Show();
    }

    [SerializeField,FolderPath] private string saveLocation;
    [SerializeField] private List<GameObject> modelsToConvert;
    [SerializeField] private Material placeHolderMaterial;

    [Button]
    private void CreatePlaceHolder()
    {
        foreach (var modelToConvert in modelsToConvert)
        {
            string localPath = saveLocation + $"/{modelToConvert.name} Place Holder.prefab";
            localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
            var newModel = Instantiate(modelToConvert);

            MeshRenderer[] mrList = newModel.GetComponentsInChildren<MeshRenderer>();
            foreach (var meshRenderer in mrList)
                meshRenderer.material = placeHolderMaterial;

            UnitBehavior[] unitBehaviors = newModel.GetComponentsInChildren<UnitBehavior>();
            for (int i = 0; i < unitBehaviors.Length; i++)
                DestroyImmediate(unitBehaviors[i]);

            UnitDetection unitDection = newModel.GetComponentInChildren<UnitDetection>();
            if(unitDection != null)
                DestroyImmediate(unitDection.gameObject);

            Collider[] colliders = newModel.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
                DestroyImmediate(colliders[i]);

            newModel.AddComponent<UnitIdentifier>().unitType = newModel.GetComponent<PlayerUnit>().unitType;

            DestroyComponent(newModel.GetComponent<PlayerUnit>());
            DestroyComponent(newModel.GetComponent<GPUInstancer.GPUInstancerPrefab>());
            DestroyComponent(newModel.GetComponent<GPUInstancer.GPUInstancerPrefabRuntimeHandler>());

            PrefabUtility.SaveAsPrefabAsset(newModel, localPath, out bool success);

            DestroyImmediate(newModel);

            if (!success)
                Debug.Log($"No Prefab Created at {localPath}");
            else
                Debug.Log(localPath);
        }
    }

    private void DestroyComponent<T>(T component) where T : Component
    {
        if(component != null)
            DestroyImmediate(component);
    } 
}


