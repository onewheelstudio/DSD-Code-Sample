using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using UnityEngine;
using UnityEngine.SceneManagement;

[BuildCallbackVersion(1)]
public class BuildProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    private static void GetUnitPrefabs()
    {
        UnitManager um = GameObject.FindFirstObjectByType<UnitManager>();
        if (um != null)
        { 
            um.GetAll();
            Debug.Log("Getting all prefabs for Unity Manager.");
        }
        else
            throw new System.Exception("UnitManager not found in the scene. Please ensure it is present before building.");
    }

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (scene.name.Contains("Game"))
            GetUnitPrefabs();
    }
}
