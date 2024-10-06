using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Units;

namespace HexGame.Resources
{
    [CreateAssetMenu(fileName = "Build Costs", menuName = "Hex/Build Costs")]
    public class BuildCost : ScriptableObject, HasEnumType<PlayerUnitType>
    {
        public PlayerUnitType unitType;
        public List<ResourceAmount> costs = new List<ResourceAmount>();

        public void SetType(PlayerUnitType type)
        {
            this.unitType = type;
        }

        PlayerUnitType HasEnumType<PlayerUnitType>.GetType()
        {
            return unitType;
        }
    }
}

public interface HasEnumType<T> where T : System.Enum
{
    T GetType();
    void SetType(T type);
}
