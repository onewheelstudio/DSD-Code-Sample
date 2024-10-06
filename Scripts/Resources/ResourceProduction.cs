using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace HexGame.Resources
{
    [System.Serializable]
    [ManageableData]
    [CreateAssetMenu(menuName = "Hex/Resource Production")]
    public class ResourceProduction : ScriptableObject, IEqualityComparer<ResourceProduction>
    {
        [SerializeField]
        public string _niceName;
        public string niceName
        {
            get
            {
                if (string.IsNullOrEmpty(_niceName))
                    return this.name;
                else
                    return _niceName;
            }
        }

        [SerializeField] private List<ResourceAmount> production = new List<ResourceAmount>();
        [SerializeField] private List<ResourceAmount> cost = new List<ResourceAmount>();
        [NonSerialized, ShowInInspector, ReadOnly] private List<ResourceAmount> upgradedProduction = new List<ResourceAmount>();
        [NonSerialized, ShowInInspector, ReadOnly] private List<ResourceAmount> upgradedCost = new List<ResourceAmount>();
        public List<UnitCondition> useConditions = new List<UnitCondition>();

        public List<ProductivityCondition> productivityConditions = new List<ProductivityCondition>();
        [NonSerialized,ShowInInspector,ReadOnly] private List<ProductionUpgrade> upgrades = new List<ProductionUpgrade>();

        [Range(1,60)]
        [SerializeField] private float timeToProduce = 10;
        [NonSerialized,ShowInInspector, ReadOnly] private float upgradeTime = 0f;
        [BoxGroup,ShowInInspector]
        public float numPerInDay { get => FindObjectOfType<DayNightManager>().DayLength / Mathf.Max(1,GetTimeToProduce()); }

        [SerializeField] private bool unlockAtStart = false;
        [NonSerialized] private bool isUnlocked = false;
        public bool IsUnlocked => isUnlocked || unlockAtStart;

        public bool CanProduce(GameObject gameObject)
        {
            foreach (var condition in useConditions)
            {
                if (!condition.CanUse(gameObject))
                     return false;
            }

            return true;
        }

        public float ProductivityBoost(GameObject gameObject)
        {
            if (useConditions.Count > 0 && !CanProduce(gameObject))
                return Mathf.Infinity;

            float boost = 1f;
            foreach (var condition in productivityConditions)
            {
                boost *= condition.ProductivityMultiplier(gameObject);
            }

            return boost;
        }

        [Button]
        public void AddUpgrade(ProductionUpgrade upgrade)
        {
            upgrades.Add(upgrade);
            //initialize
            upgradedProduction = new List<ResourceAmount>(production);
            upgradedCost = new List<ResourceAmount>(cost);
            upgradeTime = timeToProduce;

            //recalculate with all upgrades
            foreach (var u in upgrades)
            {
                upgradeTime += u.timeToProduce;
                foreach (var result in u.productionResults)
                {
                    int index = upgradedProduction.FindIndex(x => x.type == result.type);
                    if (index >= 0)
                    {
                        ResourceAmount newAmount = upgradedProduction[index] + result;
                        if (newAmount.amount <= 0)
                            continue;
                        else
                            upgradedProduction[index] = newAmount;
                    }
                    else
                        upgradedProduction.Add(result);
                }

                foreach (var result in u.productCost)
                {
                    int index = upgradedCost.FindIndex(x => x.type == result.type);
                    if (index >= 0)
                    {
                        ResourceAmount newAmount = upgradedCost[index] + result;
                        if (newAmount.amount <= 0)
                            continue;
                        else
                            upgradedCost[index] = newAmount;
                    }
                    else
                        upgradedCost.Add(result);
                }
            }
        }

        public float GetTimeToProduce()
        {
            if (upgradeTime == 0f)
                upgradeTime = timeToProduce;

            return upgradeTime;
        }

        public List<ResourceAmount> GetCost()
        {
            if(upgradedCost == null || upgradedCost.Count == 0)
                upgradedCost = new List<ResourceAmount>(cost);

            return upgradedCost;
        }

        public List<ResourceAmount> GetProduction()
        {
            if (upgradedProduction.Count == 0)
                upgradedProduction = new List<ResourceAmount>(production);

            return upgradedProduction;
        }

        [Button]
        public void ClearUpgrades()
        {
            upgrades.Clear();
            upgradedCost = new List<ResourceAmount>(cost);
            upgradedProduction = new List<ResourceAmount>(production);
            upgradeTime = timeToProduce;
        }

        public bool Equals(ResourceProduction x, ResourceProduction y)
        {
            if(ReferenceEquals(x,y))
                return true;

            if (x is null || y is null)
                return false;

            return false;
        }

        public int GetHashCode(ResourceProduction obj)
        {
            return obj.GetHashCode();
        }

        public void Unlock()
        {
            this.isUnlocked = true;
        }
    }
}


