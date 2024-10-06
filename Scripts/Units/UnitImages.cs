using HexGame.Units;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Units/Unit Images")]
[Searchable]
public class UnitImages : SerializedScriptableObject
{
    [SerializeField] private Sprite defaultImage;
    [SerializeField, Searchable] private Dictionary<PlayerUnitType, Sprite> playerUnitImages = new Dictionary<PlayerUnitType, Sprite>();

    public Sprite GetPlayerUnitImage(PlayerUnitType type)
    {
        if (playerUnitImages.TryGetValue(type, out Sprite image) && image != null)
        {
                return image;
        }
        else
        {
            return defaultImage;
        }
    }

    [Button]
    private void AddAllStats()
    {
        foreach (PlayerUnitType playerUnit in System.Enum.GetValues(typeof(PlayerUnitType)))
        {
            if (playerUnitImages.ContainsKey(playerUnit) == false)
            {
                playerUnitImages.Add(playerUnit, null);
            }
        }
    }
}
