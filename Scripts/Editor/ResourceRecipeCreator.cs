using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using HexGame.Resources;
using System.Collections.Generic;
using System.Linq;

public class ResourceRecipeCreator : OdinEditorWindow
{
    [MenuItem("Tools/Resource Recipe Creator")]
    private static void OpenWindow()
    {
        ResourceRecipeCreator window = GetWindow<ResourceRecipeCreator>();
        window.Show();
        window.path = PlayerPrefs.GetString("Resource Recipe Path", "");
    }
    private new void OnDestroy()
    {
        PlayerPrefs.SetString("Resource Recipe Path", path);
        base.OnDestroy();
    }

    [SerializeField]
    [OnValueChanged("FindTemplate")]
    private ResourceType resourceType;

    [FolderPath, SerializeField, Required]
    public string path;

    [InlineEditor(Expanded = true)]
    public ResourceProduction recipe;

    [GUIColor(0.5f,1f,0.5f)]
    [ButtonGroup("")]
    private void SaveResourceTemplate()
    {
        if (string.IsNullOrEmpty(recipe.niceName))
            return;

        if(!AssetDatabase.Contains(recipe))
            AssetDatabase.CreateAsset(recipe, path + "/" + recipe.niceName + ".asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        NewRecipe();
    }
    
    [GUIColor(0.5f,0.5f,1f)]
    [ButtonGroup("")]
    private void NewRecipe()
    {
        recipe = ScriptableObject.CreateInstance<ResourceProduction>();
    }


    private void FindTemplate()
    {
        if (this.recipe != null)
            SaveResourceTemplate();

        Debug.Log("Looking for template");
        List<ResourceProduction> templates = HelperFunctions.GetScriptableObjects<ResourceProduction>(path);
        ResourceProduction recipe = null;

        if(templates != null && templates.Count > 0)
            recipe = templates.Where(x => x.GetProduction()[0].type == resourceType).FirstOrDefault();

        if(recipe == null)
        {
            NewRecipe();
        }
        else
        {
            this.recipe = recipe;
        }
    }
}
