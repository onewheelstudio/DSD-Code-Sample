using HexGame.Resources;
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Upgrades/Tile Unlock Upgrade")]
public class TileUnlockUpgrade : Upgrade
{
    [SerializeField] private HexTileType tileType;
    public static event Action<HexTileType> OnTileUnlocked;
    public Texture2D tileImage;

    public override void DoUpgrade()
    {
        OnTileUnlocked?.Invoke(tileType);
        UnlockQuests();
    }

    public override string GenerateDescription()
    {
        return $"Unlocks the ability to build <b>{tileType.ToNiceString()}</b> tiles.";
    }

    public override string GenerateNiceName()
    {
        return $"{tileType.ToNiceString()} Tiles";
    }
}

