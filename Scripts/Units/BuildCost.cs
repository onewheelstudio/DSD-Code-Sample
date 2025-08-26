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

        public List<ResourceAmount> GetCosts()
        {
            if(UnitManager.UseLowCost)
            {
               List<ResourceAmount> lowCosts = new List<ResourceAmount>();
                foreach (var cost in costs)
                {
                    lowCosts.Add(new ResourceAmount(cost.type, 5));
                }
                return lowCosts;
            }

            return costs;
        }
    }
}

public interface HasEnumType<T> where T : System.Enum
{
    T GetType();
    void SetType(T type);
}
