using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Upgrades/UpgradeStartingPoint")]
public class UpgradeStartingPoint : Upgrade
{
    public Texture2D startingPointTexture;

    public override void DoUpgrade()
    {
        UnlockQuests();
    }
}
