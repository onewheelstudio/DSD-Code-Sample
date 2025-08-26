using DG.Tweening;
using HexGame.Resources;
using Nova;
using NovaSamples.UIControls;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TechTreeSearch : MonoBehaviour
{
    private HexTechTree techTree;
    private TextField searchField;
    private TextFieldSelector searchTextSelector;
    [SerializeField]
    private UpgradeTile[] tiles;
    public static event Action<UpgradeTile> resultFound;
    [SerializeField]
    private Transform techTreeContainer;
    [SerializeField]
    private ListView searchResults;
    [SerializeField]
    private int maxResultsToShow = 8;
    [SerializeField] 
    private UnitImages unitImages;
    private PlayerResources playerResources;
    [SerializeField] private ClipMask searchIndicator;
    private UIBlock2D searchIndicatorBlock;
    private CancellationTokenSource cancelSearch;

    private void Awake()
    {
        techTree = GetComponentInParent<HexTechTree>();
        searchField = GetComponentInChildren<TextField>();
        searchTextSelector = searchField.GetComponent<TextFieldSelector>();
        searchResults.AddDataBinder<UpgradeTile, SearchResultVisuals>(PopulateSearchResults);
        searchIndicatorBlock = searchIndicator.GetComponent<UIBlock2D>();
    }


    private void Start()
    {
        if (!SaveLoadManager.Loading)
            FindTiles();
        else
            SaveLoadManager.LoadComplete += FindTiles;
    }

    private void FindTiles()
    {
        SaveLoadManager.LoadComplete -= FindTiles;
        tiles = FindObjectsByType<UpgradeTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    private void OnEnable()
    {
        searchField.OnTextChanged += OnSearchTextChanged;
        HexTechTree.techTreeOpen += CleanUpSearch;
    }


    private void OnDisable()
    {
        searchField.OnTextChanged -= OnSearchTextChanged;
        HexTechTree.techTreeOpen -= CleanUpSearch;
        if (cancelSearch != null)
        {
            cancelSearch.Cancel();
            cancelSearch.Dispose();
        }
    }

    private void CleanUpSearch(bool obj)
    {
        searchField.Text = "";
        OnSearchTextChanged("");
        searchTextSelector.RemoveFocus();
    }

    private void OnSearchTextChanged(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            searchResults.SetDataSource(new List<UpgradeTile>());
            return;
        }

        if(cancelSearch != null)
            cancelSearch.Cancel();

        cancelSearch = new CancellationTokenSource();

        SearchUpgrades(text, cancelSearch.Token);
    }
    
    private async void SearchUpgrades(string searchText, CancellationToken token)
    {
        await Awaitable.BackgroundThreadAsync();
        List<UpgradeTile> results = new List<UpgradeTile>();
        for (int i = 0; tiles.Length > i; i++)
        {
            if (token.IsCancellationRequested)
                return;

            if (tiles[i].upgrade.UpgradeName.ToLower().Contains(searchText.ToLower()))
            {
                results.Add(tiles[i]);
            }
        }


        if (results.Count > maxResultsToShow)
            results = results.GetRange(0, maxResultsToShow);

        await Awaitable.MainThreadAsync();
        searchResults.SetDataSource(results);
    }


    private void PopulateSearchResults(Data.OnBind<UpgradeTile> evt, SearchResultVisuals target, int index)
    {
        Color levelColor = techTree.GetUpgradeColor(evt.UserData.upgrade.upgradeTier);
        string levelText = TMPHelper.Color($"(Lvl. {evt.UserData.upgrade.upgradeTier})", levelColor);
        target.Label.Text = $"{evt.UserData.upgrade.UpgradeName} {levelText}";
        target.button.RemoveAllListeners();
        target.button.Clicked += () => MoveToUpgrade(evt.UserData);
        target.icon.Color = Color.white;
        SetUpgradeIcon(evt.UserData, target.icon);
    }

    private void MoveToUpgrade(UpgradeTile upgradeTile)
    {
        resultFound?.Invoke(upgradeTile);
        techTreeContainer.DOLocalMove(-upgradeTile.transform.localPosition * techTreeContainer.localScale.x, 0.25f).SetUpdate(true);
        searchIndicator.transform.localPosition = upgradeTile.transform.localPosition;
        searchIndicatorBlock.Color = techTree.GetUpgradeColor(upgradeTile.upgrade.upgradeTier);

        var sequence = DOTween.Sequence();
        sequence.Append(searchIndicator.DoFade(0.9f, 0.5f));
        sequence.Append(searchIndicator.DoFade(0f, 1.5f));
        //sequence.SetLoops(2);
        sequence.SetUpdate(true);
    }

    public void SetUpgradeIcon(UpgradeTile tile, UIBlock2D iconBlock)
    {
        if (tile.upgrade is StatsUpgrade statUpgrade)
            SetStatsUpgradeIcon(statUpgrade, iconBlock);
        else if (tile.upgrade is UnitUnlockUpgrade unitUnlock)
            SetUnitUnlockUpgradeIcon(unitUnlock, iconBlock);
        else if (tile.upgrade is TileUnlockUpgrade tileUnlock)
            SetTileUnlockUpgradeIcon(tileUnlock, iconBlock);
        else if (tile.upgrade is IncreaseLimitUpgrade increaseLimit)
            SetLimitIncreaseUpgradeIcon(increaseLimit, iconBlock);
        else if (tile.upgrade is ProductionUpgrade productionUpgrade)
            SetProductionUpgradeIcon(productionUpgrade, iconBlock);
        else if (tile.upgrade is RecipeUpgrade recipeUpgrade)
            SetRecipeUpgradeIcon(recipeUpgrade, iconBlock);
        else if (tile.upgrade is UnlockAutoTrader triggerUpgrade)
            SetTriggerUpgradeIcon(triggerUpgrade, iconBlock);
    }

    private void SetStatsUpgradeIcon(StatsUpgrade statsUpgrade, UIBlock2D iconBlock)
    {
        iconBlock.SetImage(unitImages.GetPlayerUnitImage(statsUpgrade.unitType));
    }

    private void SetUnitUnlockUpgradeIcon(UnitUnlockUpgrade unitUpgrade, UIBlock2D iconBlock)
    {
        iconBlock.SetImage(unitImages.GetPlayerUnitImage(unitUpgrade.buildingToUnlock));
    }

    private void SetTileUnlockUpgradeIcon(TileUnlockUpgrade tileUpgrade, UIBlock2D iconBlock)
    {
        iconBlock.SetImage(tileUpgrade.tileImage);
    }

    private void SetLimitIncreaseUpgradeIcon(IncreaseLimitUpgrade limitUpgrade, UIBlock2D iconBlock)
    {
        iconBlock.SetImage(unitImages.GetPlayerUnitImage(limitUpgrade.UnitType));
    }

    private void SetProductionUpgradeIcon(ProductionUpgrade productionUpgrade, UIBlock2D iconBlock)
    {
        iconBlock.SetImage(unitImages.GetPlayerUnitImage(productionUpgrade.buildingType));
    }

    private void SetRecipeUpgradeIcon(RecipeUpgrade recipeUpgrade, UIBlock2D iconBlock)
    {
        if (playerResources == null)
            playerResources = GameObject.FindFirstObjectByType<PlayerResources>();

        ResourceTemplate template = playerResources.GetResourceTemplate(recipeUpgrade.resourceType);
        iconBlock.SetImage(template.icon);
        iconBlock.Color = template.resourceColor;
    }

    private void SetTriggerUpgradeIcon(UnlockAutoTrader triggerUpgrade, UIBlock2D iconBlock)
    {
        iconBlock.SetImage(triggerUpgrade.Icon);
    }
}
