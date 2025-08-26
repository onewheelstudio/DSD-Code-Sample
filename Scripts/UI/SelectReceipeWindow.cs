using HexGame.Resources;
using Nova;
using NovaSamples.UIControls;
using System.Collections.Generic;
using UnityEngine;

public class SelectReceipeWindow : WindowPopup
{
    [SerializeField]
    private Button closeButton;
    [SerializeField]
    private ListView receipeList;
    private RecipeInfo receipeInfo;

    private void Awake()
    {
        receipeList.AddDataBinder<ResourceProduction, ReceipeButtonVisuals>(LoadReceipes);
    }

    private new void OnEnable()
    {
        base.OnEnable();
        closeButton.Clicked += CloseWindow;
        CloseWindow();
    }

    private new void OnDisable()
    {
        closeButton.Clicked -= CloseWindow;
        base.OnDisable();
    }

    private void LoadReceipes(Data.OnBind<ResourceProduction> evt, ReceipeButtonVisuals target, int index)
    {
        target.Initialize();
        target.Label.Text = evt.UserData.niceName;
        target.receipeInfo = this.receipeInfo;
        target.requirements.SetDataSource(evt.UserData.GetCost());
        target.products.SetDataSource(evt.UserData.GetProduction());
        target.receipeButton.RemoveAllListeners();
        target.receipeButton.Clicked += () => this.receipeInfo.recipeOwner.SetReceipe(index);
        target.receipeButton.Clicked += CloseWindow;
    }

    /// <summary>
    /// Returns true if recipes are changed or updated. Returns false if recipes have not changed.
    /// </summary>
    /// <param name="receipeInfo"></param>
    /// <returns></returns>
    public bool SetRecipes(RecipeInfo receipeInfo)
    {
        this.receipeInfo = receipeInfo;
        if(RecipesNeedUpate(receipeList.GetDataSource<ResourceProduction>(), receipeInfo))
        {
            receipeList.SetDataSource(GetUnlockedRecipes(receipeInfo));
            return true;
        }

        return false;
    }

    private List<ResourceProduction> GetUnlockedRecipes(RecipeInfo recipeInfo)
    {
        List<ResourceProduction> recipes = new();
        for (int i = 0; i < recipeInfo.recipes.Count; i++)
        {
            if (recipeInfo.recipes[i].IsUnlocked)
                recipes.Add(recipeInfo.recipes[i]);
        }

        return recipes;
    }

    private bool RecipesNeedUpate(IList<ResourceProduction> recipes, RecipeInfo recipeInfo)
    {
        if(recipes == null || recipeInfo == null || recipeInfo.recipes == null || recipes.Count != recipeInfo.recipes.Count)
            return true;

        for (int i = 0; i < recipes.Count; i++)
        {
            if (recipes[i].niceName != recipeInfo.recipes[i].niceName)
                return true;
        }

        return false;
    }

    public override void CloseWindow()
    {
        receipeList.SetDataSource<ResourceProduction>(null);
        base.CloseWindow();
    }

}
