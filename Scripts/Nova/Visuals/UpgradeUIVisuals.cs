using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using HexGame.Resources;

public class UpgradeUIVisuals : ItemVisuals
{
    public UIBlock2D background;
    public UIBlock2D outline;
    public UIBlock2D innerFade;
    private Color color;
    private Color hoverColor;
    [SerializeField]
    private Color purchasedColor = Color.white;
    private UpgradeTile uiTile;
    [SerializeField]private UIBlock2D icon;
    [SerializeField]private UIBlock2D crossOut;

    [Header("Stats")]
    [SerializeField] private ListView statList;
    [SerializeField] private TextBlock cost;
    [SerializeField] private UIBlock2D costIcon;

    [Header("Info SOs")]
    [SerializeField] private UnitImages unitImages;
    [SerializeField] private StatsInfo statsInfo;
    [SerializeField] private ColorData colorData;

    public ClipMask clipMask;
    [SerializeField]
    private float lockedAlpha = 0.5f;
    [SerializeField] private Color unLockedColor;
    [SerializeField] private Color lockedColor;
    [SerializeField] private UIBlock2D unlockIcon;

    public static event Action<Upgrade, UpgradeTile> onUpgradeHover;
    public static event Action<Upgrade, UpgradeTile> onUpgradeUnHover;

    protected static PlayerResources playerResources;

    public void Initialize(UpgradeTile tile, Color color)
    {
        this.uiTile = tile;

        if (tile.upgrade is StatsUpgrade statUpgrade)
            InitializeStatsUpgrade(statUpgrade);
        else if (tile.upgrade is UnitUnlockUpgrade unitUnlock)
            InitializeUnitUnlockUpgrade(unitUnlock);
        else if (tile.upgrade is TileUnlockUpgrade tileUnlock)
            InitializeTileUnlockUpgrade(tileUnlock);
        else if (tile.upgrade is IncreaseLimitUpgrade increaseLimit)
            InitializeLimitIncreaseUpgrade(increaseLimit);
        else if (tile.upgrade is UpgradeStartingPoint startingPoint)
            InitializeStartingPoint(startingPoint);
        else if (tile.upgrade is ProductionUpgrade productionUpgrade)
            InitializeProductionUpgrade(productionUpgrade);
        else if(tile.upgrade is RecipeUpgrade recipeUpgrade)
            InitializeRecipeUpgrade(recipeUpgrade);
        else if(tile.upgrade is UnlockAutoTrader triggerUpgrade)
            InitializeTriggerUpgrade(triggerUpgrade);
        else
            Debug.LogError($"{tile.upgrade.UpgradeName}  : Upgrade not supported");

        if (tile.upgrade.cost > 20000)
            cost.Text = $"{tile.upgrade.cost / 1000}K";
        else
            cost.Text = $"{tile.upgrade.cost}";

        if(tile.IsDemoBlocked() || tile.IsEarlyAccessBlocked())
        {
            cost.gameObject.SetActive(false);
            costIcon.gameObject.SetActive(false);
        }

        SetColor(color);
        SetUIState(tile.status);
    }



    public void DoHover()
    {
        //outline.Color = AdjustColor(uiTile.status, hoverColor);
        AssignHoverColor(uiTile.status);
        //uiTile.transform.SetAsLastSibling();
        uiTile.transform.DOScale(1.05f, 0.25f);

        onUpgradeHover?.Invoke(uiTile.upgrade, uiTile);
    }

    public void DoUnHover()
    {
        //outline.Color = AdjustColor(uiTile.status,color);
        SetUIState(uiTile.status);
        uiTile.transform.DOScale(1f, 0.25f);

        onUpgradeUnHover?.Invoke(uiTile.upgrade, uiTile);
    }

    public void DoUnlock()
    {
        SetUIState(uiTile.status);

    }
    public void DoLock()
    {
        SetUIState(uiTile.status);
    }

    public void SetColor(Color color)
    {
        this.color = color;
        hoverColor = new Color(color.r - 0.1f, color.g - 0.1f, color.b - 0.1f);
        Color crossoutColor = color;
        crossoutColor.a = 0.15f;
        crossOut.Color = crossoutColor;
        outline.Color = color;
        innerFade.Color = color;
    }

    private void SetUIState(Upgrade.UpgradeStatus status)
    {
        switch (status)
        {
            case Upgrade.UpgradeStatus.purchased:
                cost.transform.parent.gameObject.SetActive(false);
                crossOut.gameObject.SetActive(false);
                clipMask.Tint = purchasedColor;
                break;
            case Upgrade.UpgradeStatus.unlocked:
                cost.transform.parent.gameObject.SetActive(true);
                crossOut.gameObject.SetActive(false);
                clipMask.Tint = purchasedColor;
                break;
            case Upgrade.UpgradeStatus.locked:
                //clipMask?.SetAlpha(lockedAlpha);
                cost.transform.parent.gameObject.SetActive(true);
                crossOut.gameObject.SetActive(true);
                clipMask.Tint = lockedColor;
                break;
        }
    }
    
    private void AssignHoverColor(Upgrade.UpgradeStatus status)
    {
        switch (status)
        {
            case Upgrade.UpgradeStatus.purchased:
            case Upgrade.UpgradeStatus.unlocked:
            case Upgrade.UpgradeStatus.locked:
                clipMask.Tint = purchasedColor;
                break;
        }
    }

    private void InitializeTriggerUpgrade(UnlockAutoTrader triggerUpgrade)
    {
        costIcon.Color = colorData.GetColor(ColorCode.techCredit);
        icon.SetImage(triggerUpgrade.Icon);
    }

    private void InitializeRecipeUpgrade(RecipeUpgrade recipeUpgrade)
    {
        costIcon.Color = colorData.GetColor(ColorCode.techCredit);

        if(playerResources == null)
            playerResources = GameObject.FindObjectOfType<PlayerResources>();

        ResourceTemplate template = playerResources.GetResourceTemplate(recipeUpgrade.resourceType);
        icon.SetImage(template.icon);
        icon.Color = template.resourceColor;
    }

    private void InitializeProductionUpgrade(ProductionUpgrade productionUpgrade)
    {
        costIcon.Color = colorData.GetColor(ColorCode.techCredit);

        List<ResourceAmount> resources = new List<ResourceAmount>(productionUpgrade.productionResults);
        resources.AddRange(productionUpgrade.productCost);

        statList.AddDataBinder<ResourceAmount, UnitInfoButtonVisuals>(SetResources);
        statList.SetDataSource(resources);

        icon.SetImage(unitImages.GetPlayerUnitImage(productionUpgrade.buildingType));

    }

    private void InitializeStartingPoint(UpgradeStartingPoint startingPoint)
    {
        costIcon.gameObject.SetActive(false);
        GameObject startingPointIcon = new GameObject("StartingPointIcon");
        startingPointIcon.transform.SetParent(background.transform);
        startingPointIcon.transform.localScale = Vector3.one;
        startingPointIcon.layer = LayerMask.NameToLayer("UI");
        UIBlock2D block = startingPointIcon.AddComponent<UIBlock2D>();
        block.AutoSize.XY = AutoSize.Expand;
        block.SetImage(startingPoint.startingPointTexture);
        block.TrySetLocalPosition(Vector3.zero);
        icon.gameObject.SetActive(false);
    }

    private void InitializeLimitIncreaseUpgrade(IncreaseLimitUpgrade upgrade)
    {
        costIcon.Color = colorData.GetColor(ColorCode.techCredit);

        icon.SetImage(unitImages.GetPlayerUnitImage(upgrade.UnitType));
    }

    private void InitializeUnitUnlockUpgrade(UnitUnlockUpgrade upgrade)
    {
        icon.SetImage(unitImages.GetPlayerUnitImage(upgrade.buildingToUnlock));

        costIcon.Color = colorData.GetColor(ColorCode.techCredit);
        unlockIcon.gameObject.SetActive(true);
    }
    
    private void InitializeTileUnlockUpgrade(TileUnlockUpgrade tileUpgrade)
    {
        costIcon.Color = colorData.GetColor(ColorCode.techCredit);

        unlockIcon.gameObject.SetActive(true);
        icon.SetImage(tileUpgrade.tileImage);
    }

    private void InitializeStatsUpgrade(StatsUpgrade statsUpgrade)
    {
        string _name = statsUpgrade.statsToUpgrade.name;
        _name = _name.Replace("Stats", "");
        costIcon.Color = colorData.GetColor(ColorCode.techCredit);
        icon.SetImage(unitImages.GetPlayerUnitImage(statsUpgrade.unitType));

        List<StatData> stats = new List<StatData>();
        foreach (var stat in statsUpgrade.upgradeToApply)
        {
            StatData statData = new StatData();
            statData.stat = stat.Key;
            statData.amount = stat.Value;
            stats.Add(statData);
        }
        statList.AddDataBinder<StatData, UnitInfoButtonVisuals>(SetUpgrades);
        statList.SetDataSource(stats);
    }


    private void SetResources(Data.OnBind<ResourceAmount> evt, UnitInfoButtonVisuals target, int index)
    {
        if (playerResources == null)
            playerResources = GameObject.FindObjectOfType<PlayerResources>();

        ResourceTemplate resource = playerResources.GetResourceTemplate(evt.UserData.type);

        target.icon.SetImage(resource.icon);

        if (evt.UserData.amount > 0)
            target.label.Text = $"+{evt.UserData.amount}";
        else
            target.label.Text = $"{evt.UserData.amount}";

        target.infoToolTip.SetToolTipInfo(resource.type.ToNiceString(), resource.icon, "");
        target.icon.Color = resource.resourceColor;
    }


    private void SetUpgrades(Data.OnBind<StatData> evt, UnitInfoButtonVisuals target, int index)
    {
        StatsInfo.StatInfo info = statsInfo.GetStatInfo(evt.UserData.stat);
        target.icon.SetImage(info.icon);

        if(evt.UserData.amount > 0)
            target.label.Text = $"+{evt.UserData.amount}";
        else
            target.label.Text = $"{evt.UserData.amount}";

        target.infoToolTip.SetToolTipInfo(info.stat.ToNiceString(), info.icon, info.description);

        if(evt.UserData.amount > 0)
            target.icon.Color = colorData.GetColor(ColorCode.green);
        else if(evt.UserData.amount < 0)
            target.icon.Color = colorData.GetColor(ColorCode.red);
        else
            target.icon.Color = Color.white;
    }

    [System.Serializable]
    public class StatVisuals
    {
        public TextBlock amount;
        public UIBlock2D icon;

        public void SetUp(Stat stat, float amount)
        {
            if (amount >= 0)
                this.amount.Text = $"+{amount}";
            else
                this.amount.Text = $"{amount}";

            icon.SetImage(GameObject.FindObjectOfType<HexTechTree>().GetStatIcon(stat));
        }

        public void TurnOff()
        {
            amount.gameObject.SetActive(false);
            icon.gameObject.SetActive(false);
        }

        public void TurnOn()
        {
            amount.gameObject.SetActive(true);
            icon.gameObject.SetActive(true);
        }
    }

    private struct LerpScale : IAnimation
    {
        public Vector3 start;
        public Vector3 end;
        public Transform transform;

        public LerpScale(Vector3 start, Vector3 end, Transform transform)
        {
            this.start = start;
            this.end = end;
            this.transform = transform;
        }

        public void Update(float percentDone)
        {
            transform.localScale = Vector3.Lerp(start,end, percentDone);
        }
    }

}
