using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class GenericSOWindow<T> : OdinEditorWindow where T : ScriptableObject
{
    [FolderPath, SerializeField, Required]
    protected string path;
    [SerializeField, Required]
    protected string _name;

    [InlineEditor(Expanded = true)]
    public T so;

    [GUIColor(0.5f,1f,0.5f)]
    [HorizontalGroup("Buttons")]
    [Button]
    protected void SaveUpgrade()
    {
        if (string.IsNullOrEmpty(_name))
            return;

        AssetDatabase.CreateAsset(so, path + "/" + _name + ".asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        NewUpgrade();
        _name = "";
    }
    
    [GUIColor(0.5f,0.5f,1f)]
    [HorizontalGroup("Buttons")]
    [Button]
    protected void NewUpgrade()
    {
        so = ScriptableObject.CreateInstance<T>();
    }

}

public class StatEditorWindow: GenericSOWindow<Stats>
{
    [MenuItem("Tools/Stats Creator")]
    protected static void OpenWindow()
    {
        StatEditorWindow window = GetWindow<StatEditorWindow>();
        window.Show();
        window.path = PlayerPrefs.GetString("Stat Path", "");
        window.NewUpgrade();
    }

    private new void OnDestroy()
    {
        PlayerPrefs.SetString("Stat Path", path);
        base.OnDestroy();
    }
}
