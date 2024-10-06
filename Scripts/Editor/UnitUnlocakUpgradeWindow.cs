using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class UnitUnlocakUpgradeWindow : OdinEditorWindow
{
    [MenuItem("Tools/Unlock Unit Upgrade Creator")]
    private static void OpenWindow()
    {
        UnitUnlocakUpgradeWindow window = GetWindow<UnitUnlocakUpgradeWindow>();
        window.Show();
        window.path = PlayerPrefs.GetString("UnlockUnitPath", "");
    }

    private new void OnDestroy()
    {
        PlayerPrefs.SetString("UnlockUnitPath", path);
        base.OnDestroy();
    }

    [FolderPath, SerializeField, Required]
    private string path;

    [InlineEditor(Expanded = true)]
    public UnitUnlockUpgrade upgrade;

    [GUIColor(0.5f,1f,0.5f)]
    [ButtonGroup("")]
    private void SaveUpgrade()
    {

        if (string.IsNullOrEmpty(upgrade.UpgradeName))
            return;
        string name = new string(upgrade.UpgradeName.Where(c => !char.IsPunctuation(c)).ToArray());
        AssetDatabase.CreateAsset(upgrade, path + "/" + name + ".asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        NewUpgrade();
    }
    
    [GUIColor(0.5f,0.5f,1f)]
    [ButtonGroup("")]
    private void NewUpgrade()
    {
        upgrade = ScriptableObject.CreateInstance<UnitUnlockUpgrade>();
    }

    [GUIColor(0.5f, 1f, 0.5f)]
    [ButtonGroup("")]
    private string GenerateName()
    {
        string name = "Unlock ";

        name += upgrade.buildingToUnlock.ToNiceString();

        return name;
    }

    private string GenerateDescription()
    {
        return $"Unlocks {upgrade.buildingToUnlock.ToNiceString()}";
    }
}
