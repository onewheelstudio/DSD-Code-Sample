using Nova;
using UnityEngine;
using DG.Tweening;

public class UpgradeGlow : MonoBehaviour
{
    [SerializeField] private Color normalColor;
    private UIBlock2D block;
    private HexTechTree techTree;

    private void OnEnable()
    {
        techTree = GameObject.FindObjectOfType<HexTechTree>();
        block = GetComponent<UIBlock2D>();
        normalColor = block.Color;
        UpgradeUIVisuals.onUpgradeHover += DoHover;
        UpgradeUIVisuals.onUpgradeUnHover += DoUnHover;
    }

    private void DoUnHover(Upgrade upgrade, UpgradeTile tile)
    {
        block.Color = normalColor;
    }

    private void DoHover(Upgrade upgrade, UpgradeTile tile)
    {
        if (techTree == null)
            return;
        Color color = techTree.GetUpgradeColor(upgrade.upgradeTier);
        color.a = normalColor.a;
        block.Color = color;
    }

    private void OnDisable()
    {
        UpgradeUIVisuals.onUpgradeHover -= DoHover;
        UpgradeUIVisuals.onUpgradeUnHover -= DoUnHover;
        DOTween.Kill(this,true);
    }
}
