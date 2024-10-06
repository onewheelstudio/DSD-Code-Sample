using DG.DemiEditor;
using HexGame.Resources;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RecipeUpgradeWindow : OdinEditorWindow
{
    [MenuItem("Tools/Recipe Upgrade Creator")]
    private static void OpenWindow()
    {
        RecipeUpgradeWindow window = GetWindow<RecipeUpgradeWindow>();
        window.Show();
        window.path = PlayerPrefs.GetString("RecipeUpgradePath", "");
    }
    private new void OnDestroy()
    {
        PlayerPrefs.SetString("RecipeUpgradePath", path);
        base.OnDestroy();
    }

    [FolderPath, SerializeField, Required]
    private string path;

    [InlineEditor(Expanded = true)]
    public RecipeUpgrade upgrade;

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
        upgrade = ScriptableObject.CreateInstance<RecipeUpgrade>();
    }

    public List<ResourceProduction> recipeList = new();
    public List<RecipeUpgrade> upgrades = new();

    [GUIColor(0.5f, 0.5f, 1f)]
    [ButtonGroup("")]
    private void CreateRemaining()
    {
        recipeList = HelperFunctions.GetScriptableObjects<ResourceProduction>("Assets/Prefabs/Resource SOs/Resource Recipes");

        if (path.IsNullOrEmpty())
            return;

        upgrades = HelperFunctions.GetScriptableObjects<RecipeUpgrade>(path);
        for(int i = recipeList.Count - 1; i >= 0; i--)
        {
            if(upgrades.Exists(x => recipeList[i] == x.recipe))
                recipeList.RemoveAt(i);
        }

        foreach (var recipe in recipeList)
        {
            RecipeUpgrade newUpgrade = ScriptableObject.CreateInstance<RecipeUpgrade>();
            string recipeName = recipe.GetProduction()[0].type.ToNiceString() + " Unlock";
            newUpgrade.recipe = recipe;
            upgrades.Add(newUpgrade);
            AssetDatabase.CreateAsset(newUpgrade, path + "/" + recipeName + ".asset");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }






}
