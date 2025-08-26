using HexGame.Resources;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexGame.Units
{
    [RequireComponent(typeof(TransportStorageBehavior))]
    public class ResourceStart : UnitBehavior
    {

        [SerializeField] 
        private List<PlayerUnitType> starterBuildings = new List<PlayerUnitType>();
        private UnitManager unitManager;
        private PlayerResources playerResources;

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

            unitManager ??= FindFirstObjectByType<UnitManager>();
            playerResources ??= FindFirstObjectByType<PlayerResources>();

            if (SaveLoadManager.Loading)
                return;

            TransportStorageBehavior storage = this.GetComponent<TransportStorageBehavior>();

            foreach (var unit in starterBuildings)
            {
                foreach (var resource in unitManager.GetUnitCost(unit))
                {
                    if (resource.type == HexGame.Resources.ResourceType.Workers)
                        workersAdded?.Invoke(resource.amount);
                    else
                    {
                        storage.AddAllowedResource(resource.type);
                        storage.AddResourceForPickup(resource);
                        playerResources.AddResource(resource);
                    }
                }
            }

            foreach (var resource in resources)
            {
                if (resource.type == HexGame.Resources.ResourceType.Workers)
                {
                    workersAdded?.Invoke(resource.amount);
                    continue;
                }

                if(resource.type != Resources.ResourceType.Workers)
                    storage.AddAllowedResource(resource.type);
                storage.AddResourceForPickup(resource);
                playerResources.AddResource(resource);
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
