using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Resources;
using Sirenix.OdinInspector;
using System.Linq;
using System;

namespace HexGame.Units
{
    [RequireComponent(typeof(GlobalStorageBehavior))]
    public class ResourceStart : UnitBehavior
    {

        [SerializeField] 
        private List<PlayerUnitType> starterBuildings = new List<PlayerUnitType>();
        private UnitManager unitManager;

        [SerializeField]
        private List<HexGame.Resources.ResourceAmount> resources = new List<HexGame.Resources.ResourceAmount>();
        public static event System.Action<int> workersAdded;

        public static event Action initialResourcesAdded;
        private bool isInitialized = false;

        public override void StartBehavior()
        {
            isFunctional = true;

            if(isInitialized) //lets only do this once
                return;

            unitManager ??= FindObjectOfType<UnitManager>();

            foreach (var unit in starterBuildings)
            {
                foreach (var resource in unitManager.GetUnitCost(unit))
                {
                    if (resource.type == HexGame.Resources.ResourceType.Workers)
                        workersAdded?.Invoke(resource.amount);
                    else
                        this.GetComponent<GlobalStorageBehavior>().StoreResource(resource);
                }
            }

            foreach (var resource in resources)
            {
                if (resource.type == HexGame.Resources.ResourceType.Workers)
                    workersAdded?.Invoke(resource.amount);
                else
                    this.GetComponent<GlobalStorageBehavior>().StoreResource(resource);
            }

            initialResourcesAdded?.Invoke();
            isInitialized = true;
        }

        public override void StopBehavior()
        {
            isFunctional = false;
        }
    }
}
