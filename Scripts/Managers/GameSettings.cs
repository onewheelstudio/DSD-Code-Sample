using Sirenix.OdinInspector;
using UnityEngine;
using HexGame.Units;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "GameSettings", menuName = "Hex/GameSettings")]
public class GameSettings : ScriptableObject
{
    [SerializeField, OnValueChanged("@ToggleBuildType()")] 
    private bool isDemo = false;
    public bool IsDemo { get => isDemo; }
    [SerializeField] private int maxTierForDemo = 2;
    public int MaxTierForDemo { get => maxTierForDemo; }
    public static event System.Action<bool> demoToggled;

    [SerializeField, OnValueChanged("@ToggleBuildType()")] 
    private bool isEarlyAccess = false;
    public bool IsEarlyAccess { get => isEarlyAccess; }
    [SerializeField] private int maxTierForEarlyAccess = 10;
    public int MaxTierForEarlyAccess { get => maxTierForEarlyAccess; }
    public static event System.Action<bool> earlyAccessToggled;

#if UNITY_EDITOR
    [Header("Scenes")]
    [SerializeField] private SceneAsset earlyAccessGameScene;
#endif

    [Header("Demo Settings")]
    [SerializeField] private List<PlayerUnitType> demoTypes = new List<PlayerUnitType>();
    public List<PlayerUnitType> DemoTypes { get => demoTypes; }

    public void ToggleBuildType()
    {
        demoToggled?.Invoke(isDemo);
        earlyAccessToggled?.Invoke(isEarlyAccess);

        SetEditorBuildSettingsScenes(isDemo);
    }

    public void SetEditorBuildSettingsScenes(bool isDemo)
    {
#if UNITY_EDITOR
        // Find valid Scene paths and make a list of EditorBuildSettingsScene
        EditorBuildSettingsScene[] editorBuildSettingsScenes = EditorBuildSettings.scenes;
        string scenePath = string.Empty;

        scenePath = AssetDatabase.GetAssetPath(earlyAccessGameScene);

        if (!string.IsNullOrEmpty(scenePath))
        {
            editorBuildSettingsScenes[1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = editorBuildSettingsScenes;
        }
#endif
    }
}
