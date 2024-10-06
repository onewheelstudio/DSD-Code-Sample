using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using HexGame.Units;

namespace HexGame.Resources
{
    [CreateAssetMenu(fileName = "Build Cost List", menuName = "Hex/Build Cost List")]
    public class BuildCostData : SerializedScriptableObject
    {
        public Dictionary<PlayerUnitType, BuildCost> buildCostData = new Dictionary<PlayerUnitType, BuildCost>();
        private const string buildCostPath = "Assets/Prefabs/Units/Player/Build Costs/";
        public List<ResourceAmount> GetBuildCosts(PlayerUnitType unitType)
        {
            if (buildCostData.TryGetValue(unitType, out BuildCost buildCost))
                return buildCost.costs;
            else
                return new List<ResourceAmount>();
        }

        private void OnValidate()
        {
            GetBuildCosts();
        }

        [Button]
        private void GetBuildCosts()
        {
            buildCostData.Clear();
            foreach (var bc in HelperFunctions.GetScriptableObjects(buildCostPath))
            {
                if (bc is BuildCost)
                {
                    BuildCost buildCost = (BuildCost)bc;
                    buildCostData.Add(buildCost.unitType, buildCost);
                }
            }
        }
    }
}
