using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public abstract class EnumSOCreator<U,T> : OdinEditorWindow where U : System.Enum where T : ScriptableObject, HasEnumType<U>
{
    [MenuItem("Tools/Resource Creator")]
    private static void OpenWindow()
    {
        EnumSOCreator<U, T> window = GetWindow<EnumSOCreator<U, T>>();
        window.Show();
        window.path = PlayerPrefs.GetString($"{typeof(T).ToString()} SO Path", "");
        window.GetPrefix();
    }

    private new void OnDestroy()
    {
        PlayerPrefs.SetString(playerPrefsPath, path);
        PlayerPrefs.SetString($"{typeof(T).ToString()} SO Prefix", SO_Suffix);
        base.OnDestroy();
    }

    [SerializeField]
    [OnValueChanged("FindTemplate")]
    private U enumType;

    [FolderPath, SerializeField, Required]
    protected string path;
    protected static string playerPrefsPath;
    [SerializeField,Required]
    protected string SO_Suffix;

    [InlineEditor(Expanded = true)]
    public T so;

    [GUIColor(0.5f,1f,0.5f)]
    [ButtonGroup("")]
    private void Save()
    {
        if (string.IsNullOrEmpty(so.name))
            return;

        if(!AssetDatabase.Contains(so))
            AssetDatabase.CreateAsset(so, path + "/" + so.GetType().ToString().ToUpper() + " " + SO_Suffix + ".asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        CreateNew();
    }
    
    [GUIColor(0.5f,0.5f,1f)]
    [ButtonGroup("")]
    private void CreateNew()
    {
        so = ScriptableObject.CreateInstance<T>();
        so.name =  so.GetType().ToString().ToUpper() + " " + SO_Suffix;
    }

    [GUIColor(0.5f, 1f, 0.5f)]
    [ButtonGroup("")]
    private void CreateRemainingTypes()
    {
        List<T> templates = HelperFunctions.GetScriptableObjects<T>(path);

        foreach (U rt in System.Enum.GetValues(typeof(U)))
        {
            T resourceTemplate = templates.Where(x => System.Enum.Equals(x.GetType(), enumType)).FirstOrDefault();
            if(resourceTemplate == null)
            {
                CreateNew();
                so.SetType(rt);
                Save();
            }
        }
    }

    private void FindTemplate()
    {
        if (this.so != null)
            Save();

        List<T> templates = HelperFunctions.GetScriptableObjects<T>(path);
        T _so = null;

        if(templates != null && templates.Count > 0)
            _so = templates.Where(x => System.Enum.Equals(x.GetType(),enumType)).FirstOrDefault();

        if(_so == null)
        {
            CreateNew();
            this.so.SetType(enumType);
        }
        else
        {
            this.so = _so;
        }
    }

    protected void GetPrefix()
    {
        SO_Suffix = PlayerPrefs.GetString($"{typeof(T).ToString()} SO Prefix", "");
    }
    
    protected void GetPath()
    {
        path = PlayerPrefs.GetString(playerPrefsPath, "");
    }
}
