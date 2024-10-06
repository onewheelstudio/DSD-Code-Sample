using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class StatsUpgradeWindow : OdinEditorWindow
{
    [MenuItem("Tools/Stats Upgrade Creator")]
    private static void OpenWindow()
    {
        StatsUpgradeWindow window = GetWindow<StatsUpgradeWindow>();
        window.Show();
        window.path = PlayerPrefs.GetString("StatsPath", "");
    }
    private new void OnDestroy()
    {
        PlayerPrefs.SetString("StatsPath", path);
        base.OnDestroy();
    }

    [FolderPath, SerializeField, Required]
    private string path;

    [InlineEditor(Expanded = true)]
    public StatsUpgrade upgrade;

    [GUIColor(0.5f,1f,0.5f)]
    [ButtonGroup("")]
    private void SaveUpgrade()
    {
        if (string.IsNullOrEmpty(upgrade.UpgradeName))
            return;

        AssetDatabase.CreateAsset(upgrade, path + "/" + upgrade.UpgradeName + ".asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        NewUpgrade();
    }
    
    [GUIColor(0.5f,0.5f,1f)]
    [ButtonGroup("")]
    private void NewUpgrade()
    {
        upgrade = ScriptableObject.CreateInstance<StatsUpgrade>();
    }
}
