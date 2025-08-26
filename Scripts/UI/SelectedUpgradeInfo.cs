using DG.Tweening;
using HexGame.Resources;
using Nova;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NovaSamples.UIControls;

public class SelectedUpgradeInfo : MonoBehaviour
{
    private UIBlock2D parentBlock;
    [SerializeField] private TextBlock upgradeHeader;
    [SerializeField] private TextBlock cost;
    [SerializeField] private TextBlock required;
    [SerializeField] private GameObject requirementParent;
    [SerializeField] private TextBlock description;
    [SerializeField] private ListView upgradeList;
    [SerializeField] private UIBlock2D buildingImage;

    [Header("Tool Tip Placement")]
    [SerializeField]
    [Range(-200, 200)]
    private float xOffset = 0;
    private float XOffset => xOffset * techTree.zoomScale;
    [SerializeField]
    [Range(-200, 200)]
    private float yOffset = 0;
    private float YOffset => yOffset * techTree.zoomScale;
    public static UnitToolTip toolTipObject;
    private Vector2 canvasResolution;
    private float canvasScale;
    [SerializeField] private bool moveToMouse = false;
    private HexTechTree techTree;
    private UpgradeTile selectedTile;
    private ClipMask clipMask;
    private Tween clipMaskTween;

    [Header("Info SOs")]
    [SerializeField] private UnitImages unitImages;
    [SerializeField] private StatsInfo statsInfo;
    private PlayerResources playerResources;

    [Header("Recipe Bits")]
    [SerializeField] private Transform productionPanel;
    [SerializeField] private ListView productList;
    [SerializeField] private ListView costList;


    private void Awake()
    {
        parentBlock = this.GetComponent<UIBlock2D>();
        clipMask = this.GetComponent<ClipMask>();
        techTree = GameObject.FindObjectOfType<HexTechTree>();
        canvasResolution = GameObject.FindObjectOfType<Nova.ScreenSpace>().ReferenceResolution;
        canvasScale = Screen.width / (float)canvasResolution.x;
    }

    private void OnEnable()
    {
        UpgradeUIVisuals.onUpgradeHover += UpdateInfo;
        UpgradeUIVisuals.onUpgradeUnHover += Clear;

        upgradeList.AddDataBinder<StatData, UnitInfoButtonVisuals>(SetUpgrades);
        upgradeList.AddDataBinder<ResourceAmount, UnitInfoButtonVisuals>(SetResources);
        productList.AddDataBinder<ResourceAmount, UnitInfoButtonVisuals>(SetIcons);
        costList.AddDataBinder<ResourceAmount, UnitInfoButtonVisuals>(SetIcons);
    }

    private void OnDisable()
    {
        UpgradeUIVisuals.onUpgradeHover -= UpdateInfo;
        UpgradeUIVisuals.onUpgradeUnHover -= Clear;
        upgradeList.RemoveDataBinder<StatData, UnitInfoButtonVisuals>(SetUpgrades);
        upgradeList.RemoveDataBinder<ResourceAmount, UnitInfoButtonVisuals>(SetResources);
        productList.RemoveDataBinder<ResourceAmount, UnitInfoButtonVisuals>(SetIcons);
        costList.RemoveDataBinder<ResourceAmount, UnitInfoButtonVisuals>(SetIcons);

        clipMaskTween.Kill();
    }

    public void UpdateInfo(Upgrade upgrade, UpgradeTile tile)
    {
        if (upgrade is UpgradeStartingPoint)
            return;

        selectedTile = tile;
        clipMaskTween = clipMask.DoFade(1f, 0.1f);
        if(moveToMouse)
            StartCoroutine(OneFrameDelayPositioning());

        upgradeHeader.Text = upgrade.UpgradeName;
        cost.Text = upgrade.cost.ToString();

        required.Text = upgrade.RequiredReputation().ToString();
        description.Text = upgrade.GenerateDescription();
        productionPanel.gameObject.SetActive(false);
        upgradeList.gameObject.SetActive(false);
        buildingImage.Color = Color.white;

        if (upgrade is StatsUpgrade statsUpgrade)
        {
            List<StatData> stats = new List<StatData>();
            foreach (var stat in statsUpgrade.upgradeToApply)
            {
                StatData statData = new StatData();
                statData.stat = stat.Key;
                statData.amount = stat.Value;
                stats.Add(statData);
            }
            buildingImage.SetImage(unitImages.GetPlayerUnitImage(statsUpgrade.unitType));
            upgradeList.gameObject.SetActive(true);
            upgradeList.SetDataSource(stats);
        }
        else if (upgrade is UnitUnlockUpgrade unitUnlock)
        {
            buildingImage.SetImage(unitImages.GetPlayerUnitImage(unitUnlock.buildingToUnlock));
        }
        else if(upgrade is TileUnlockUpgrade tileUpgrade)
        {
            buildingImage.SetImage(tileUpgrade.tileImage);
        }
        else if(upgrade is ProductionUpgrade productionUpgrade)
        {
            buildingImage.SetImage(unitImages.GetPlayerUnitImage(productionUpgrade.buildingType));
            upgradeList.gameObject.SetActive(true);

            List<ResourceAmount> resources = new List<ResourceAmount>(productionUpgrade.productionResults);
            resources.AddRange(productionUpgrade.productCost);
            upgradeList.SetDataSource(resources);
        }
        else if(upgrade is RecipeUpgrade recipeUpgrade)
        {
            if (playerResources == null)
                playerResources = GameObject.FindObjectOfType<PlayerResources>();

            ResourceTemplate template = playerResources.GetResourceTemplate(recipeUpgrade.resourceType);
            buildingImage.SetImage(template.icon);
            buildingImage.Color = template.resourceColor;

            productionPanel.gameObject.SetActive(true);
            productList.SetDataSource(recipeUpgrade.recipe.GetProduction());
            costList.SetDataSource(recipeUpgrade.recipe.GetCost());
        }
        else if(upgrade is UnlockAutoTrader triggerUpgrade)
        {
            buildingImage.SetImage(triggerUpgrade.Icon);
        }
        else if(upgrade is IncreaseLimitUpgrade increaseLimitUpgrade)
        {
            buildingImage.SetImage(unitImages.GetPlayerUnitImage(increaseLimitUpgrade.UnitType));
        }

        if (tile.IsDemoBlocked() || tile.IsEarlyAccessBlocked())
            requirementParent.SetActive(false);
        else
            requirementParent.SetActive(true);

        if(tile.IsDemoBlocked())
        {
            description.Text += "\n\nNot available in the demo.".TMP_Color(ColorManager.GetColor(ColorCode.red));
        }
        else if(tile.IsEarlyAccessBlocked())
        {
            description.Text += "\n\nA WIP. Coming Soon.".TMP_Color(ColorManager.GetColor(ColorCode.red));
        }

        parentBlock.Border.Color = techTree.GetUpgradeColor(upgrade.upgradeTier);
        parentBlock.Color = techTree.GetUpgradeColor(upgrade.upgradeTier);
    }

    public void Clear(Upgrade upgrade, UpgradeTile tile)
    {
        clipMaskTween = clipMask.DoFade(0f, 0.1f);
    }

    private IEnumerator OneFrameDelayPositioning()
    {
        yield return null;

        //var position = Mouse.current.position.ReadValue();
        var position = PCInputManager.uiCamera.WorldToScreenPoint(selectedTile.transform.position);
        var offset = GetOffset(position);

        parentBlock.Position.Y = (position.y + offset.y) / canvasScale;
        parentBlock.Position.X = (position.x + offset.x) / canvasScale;
    }


    private Vector2 GetOffset(Vector2 mousePosition)
    {
        Vector2 offset = Vector2.zero;
        Vector3 size = parentBlock.Size.Value;

        if (mousePosition.y + size.y * 2 + YOffset > Screen.height * 0.95f)
            offset += new Vector2(0, -size.y - YOffset);
        else
            offset += new Vector2(0, YOffset);

        //not dividing the size by 2 because of the canvas scale
        if (mousePosition.x + size.x * 2 > Screen.width * 0.95f)
            offset += new Vector2(-size.x - XOffset, 0);
        else
            offset += new Vector2(XOffset, 0);

        return offset * canvasScale;
    }

    private void SetResources(Data.OnBind<ResourceAmount> evt, UnitInfoButtonVisuals target, int index)
    {
        ResourceTemplate resource = GameObject.FindObjectOfType<PlayerResources>().GetResourceTemplate(evt.UserData.type);

        target.icon.SetImage(resource.icon);

        if (evt.UserData.amount > 0)
            target.label.Text = $"+{evt.UserData.amount}";
        else
            target.label.Text = $"{evt.UserData.amount}";

        target.infoToolTip.SetToolTipInfo(resource.type.ToNiceString(), resource.icon, "");
        target.icon.Color = resource.resourceColor;
    }
    
    /// <summary>
    /// Does not add a "+" in front of the amount
    /// </summary>
    /// <param name="evt"></param>
    /// <param name="target"></param>
    /// <param name="index"></param>
    private void SetIcons(Data.OnBind<ResourceAmount> evt, UnitInfoButtonVisuals target, int index)
    {
        ResourceTemplate resource = GameObject.FindObjectOfType<PlayerResources>().GetResourceTemplate(evt.UserData.type);

        target.icon.SetImage(resource.icon);
        target.label.Text = $"{evt.UserData.amount}";

        target.infoToolTip.SetToolTipInfo(resource.type.ToNiceString(), resource.icon, "");
        target.icon.Color = resource.resourceColor;
    }

    private void SetUpgrades(Data.OnBind<StatData> evt, UnitInfoButtonVisuals target, int index)
    {
        StatsInfo.StatInfo info = statsInfo.GetStatInfo(evt.UserData.stat);
        target.icon.SetImage(info.icon);
        
        if(evt.UserData.amount > 0)
        {
            target.label.Text = "+" + evt.UserData.amount.ToString();
            target.icon.Color = ColorManager.GetColor(ColorCode.green);

        }
        else
        {
            target.label.Text = evt.UserData.amount.ToString();
            target.icon.Color = ColorManager.GetColor(ColorCode.red);
        }
        target.infoToolTip.SetToolTipInfo(info.stat.ToNiceString(), info.icon, info.description);
    }
}

public struct StatData
{
    public Stat stat;
    public float amount;
}
