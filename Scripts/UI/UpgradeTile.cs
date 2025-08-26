using HexGame.Grid;
using Nova;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeTile : MonoBehaviour, IHavePopupInfo
{
    [SerializeField]
    [InlineEditor(InlineEditorModes.GUIOnly, Expanded = true)]
    [RequiredIn(PrefabKind.PrefabInstance)]
    public Upgrade upgrade;
    public Hex3 hexPosition;

    [SerializeField]
    private UIBlock upgradeBlock;
    private UpgradeUIVisuals upgradeVisuals;

    private HexTechTree techTree;
    private Upgrade.UpgradeStatus _status = Upgrade.UpgradeStatus.locked;
    public Upgrade.UpgradeStatus status
    {
        get
        {
            return _status;
        }
        set
        {
            _status = value;
        }
    }

    public static event Action<UpgradeTile, Upgrade.UpgradeStatus> upgradeStatusChange;
    public static event Action<UpgradeTile> upgradePurchased;
    public static event Action<UpgradeTile> purchaseFailed;

    protected static GameSettingsManager gameSettingsManager;

    private void Awake()
    {
        if(gameSettingsManager == null)
            gameSettingsManager = FindFirstObjectByType<GameSettingsManager>();
    }

    public void Initialize(Upgrade upgrade, HexTechTree techTree, Color color)
    {
        this.techTree = techTree;
        this.upgradeBlock.AddGestureHandler<Gesture.OnClick, UpgradeUIVisuals>(ClickUpgrade);
        this.upgradeBlock.AddGestureHandler<Gesture.OnHover, UpgradeUIVisuals>(HoverUpgrade);
        this.upgradeBlock.AddGestureHandler<Gesture.OnUnhover, UpgradeUIVisuals>(UnHoverUpgrade);
        
        SetUpgrade(upgrade, color);
    }

    private void UnHoverUpgrade(Gesture.OnUnhover evt, UpgradeUIVisuals target)
    {
        target.DoUnHover();
    }

    private void HoverUpgrade(Gesture.OnHover evt, UpgradeUIVisuals target)
    {
        target.DoHover();
    }

    private void ClickUpgrade(Gesture.OnClick evt, UpgradeUIVisuals target)
    {
        if (upgrade == null)
            return; 
        
        if(CanPurchase())
            Purchase();
        else
        {
            SFXManager.PlaySFX(SFXType.error);
            purchaseFailed?.Invoke(this); 
        }
    }


    [Button]
    private void AdjustPosition()
    {
        this.transform.localPosition = HexTechTree.scale * SwapYZ(Hex3.Hex3ToVector3(Hex3.Vector3ToHex3(SwapYZ(this.transform.localPosition / HexTechTree.scale))));
        hexPosition = Hex3.Vector3ToHex3(SwapYZ(this.transform.localPosition / HexTechTree.scale));
    }

    public Vector3 SwapYZ(Vector3 input)
    {
        return new Vector3(input.x, input.z, input.y);
    }

    private void SetUpgrade(Upgrade upgrade, Color color)

    {
        if (upgrade == null)
            return;

        this.upgrade = upgrade;

        this.upgradeBlock = this.GetComponent<UIBlock>();
        this.upgradeVisuals = this.GetComponent<ItemView>().Visuals as UpgradeUIVisuals;
        
        this.upgradeVisuals.Initialize(this, color);
        this.gameObject.name = upgrade.name;
    }

    public bool CanPurchase()
    {
        return HexTechTree.TechCredits >= upgrade.cost
               && status == Upgrade.UpgradeStatus.unlocked
               && ReputationManager.Reputation >= upgrade.RequiredReputation()
               && !GameOverMenu.isGameOver
               && !IsDemoBlocked();
    }

    /// <summary>
    /// Inteneded to help with daily directives
    /// </summary>
    /// <returns></returns>
    public bool CanBeUnlocked()
    {
        if (IsDemoBlocked())
            return false;
        else if (IsEarlyAccessBlocked())
            return false;
        else
            return status != Upgrade.UpgradeStatus.purchased;
    }

    public bool IsDemoBlocked()
    {
        return gameSettingsManager.IsDemo && upgrade.upgradeTier > gameSettingsManager.MaxTierForDemo;
    }

    public bool IsEarlyAccessBlocked()
    {
        return gameSettingsManager.IsEarlyAccess && upgrade.upgradeTier > gameSettingsManager.MaxTierForEarlyAccess;
    }

    public bool Purchase()
    {
        if(IsDemoBlocked())
        {
            SFXManager.PlaySFX(SFXType.error);
            return false;
        }

        if(IsEarlyAccessBlocked())
        {
            SFXManager.PlaySFX(SFXType.error);
            return false;
        }
        
        HexTechTree.ChangeTechCredits(-upgrade.cost);
        status = Upgrade.UpgradeStatus.purchased;
        upgradeStatusChange(this, status);
        upgradeVisuals.DoUnlock();
        upgrade.DoUpgrade();
        upgradePurchased?.Invoke(this);
        SFXManager.PlaySFX(SFXType.newDirective);

        return true;
    }

    public void ForcePurchase()
    {
        status = Upgrade.UpgradeStatus.purchased;
        upgradeStatusChange(this, status);
        upgradeVisuals.DoUnlock();
        upgrade.DoUpgrade();
    }

    public bool CanUnlock()
    {
        return techTree.CanUnlock(this.hexPosition) || upgrade.unlockedAtStart;
    }

    public bool IsPurchased()
    {
        return upgrade.unlockedAtStart || status == Upgrade.UpgradeStatus.purchased;
    }

    public bool TryUnlock()
    {
        if (CanUnlock() && status == Upgrade.UpgradeStatus.locked)
        {
            status = Upgrade.UpgradeStatus.unlocked;
            upgradeStatusChange(this, status);
        }

        if (status != Upgrade.UpgradeStatus.locked)
            upgradeVisuals.DoUnlock();
        else
            upgradeVisuals.DoLock();

        return status != Upgrade.UpgradeStatus.locked;
    }

    public List<PopUpInfo> GetPopupInfo()
    {
        string info = $"<b><uppercase>{upgrade.UpgradeName}</uppercase></b><size=50%>\n \n";
        info += "<size=100%>Cost:<i>";
        info += upgrade.cost.ToString();
        info += "</i>";

        return new List<PopUpInfo>(){ new PopUpInfo(info, 1, PopUpInfo.PopUpInfoType.name)};
    }

    public void SetStatus(Upgrade.UpgradeStatus status)
    {
        this.status = status;
        TryUnlock();
    }
}
