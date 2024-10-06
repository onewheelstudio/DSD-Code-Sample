using HexGame.Resources;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static HexGame.Resources.ResourceProductionBehavior;

namespace HexGame.Units
{
    public class GlobalStorageBehavior : UnitStorageBehavior, IStoreResource
    {
        public static event System.Action<GlobalStorageBehavior> gloablStorageAdded;
        public static event System.Action<GlobalStorageBehavior> gloablStorageRemoved;
        public static event System.Action<ResourceAmount> resourceAdded;
        public static event System.Action<ResourceAmount> resourceRemoved;

        private int numberOfWorkers = 0;
        private static int workersMoving = 0;
        public static int WorkersMoving => workersMoving;
        private float requiredNumberOfWorkers => this.GetStat(Stat.workers);

        private new void OnDisable()
        {
            base.OnDisable();
            numberOfWorkers = 0;
        }

        public override void StartBehavior()
        {
            isFunctional = requiredNumberOfWorkers == numberOfWorkers;
            workersMoving = 0;
            DisplayWarning();
            RequestWorkers();
            AddStartingConnection();
            gloablStorageAdded?.Invoke(this);
        }

        public override void StopBehavior()
        {
            isFunctional = false;
            gloablStorageRemoved?.Invoke(this);
        }

        private void DisplayWarning()
        {
            List<ProductionIssue> issueList = new List<ProductionIssue>();
            if (requiredNumberOfWorkers > numberOfWorkers)
                issueList.Add(ProductionIssue.missingWorkers);
            else if (numberOfWorkers == 0 && requiredNumberOfWorkers > 0)
                issueList.Add(ProductionIssue.noWorkers);

            if (issueList.Count == 0 && hasWarningIcon)
            {
                warningIconInstance.ToggleIconsOff();
                return;
            }

            if (!hasWarningIcon)
            {
                warningIconInstance = UnitManager.warningIcons.PullGameObject(this.transform.position, Quaternion.identity).GetComponent<WarningIcons>();
                warningIconInstance.transform.SetParent(this.transform);
            }

            warningIconInstance.SetWarnings(issueList);
        }

        public new void RequestWorkers()
        {
            base.RequestWorkers();
        }

        public override bool CanStoreResource(ResourceAmount deliver)
        {
            if (!allowAllTypes && !allowedTypes.Contains(deliver.type))
                return false;

            if (PlayerResources.GetStorageLimit(deliver.type) - GetAmountStored(deliver.type) >= deliver.amount)
                return true;
            else
                return false;
        }

        public override bool CanPickUpResource(ResourceAmount pickup)
        {
            if(pickup.type == ResourceType.Workers)
                return WorkerManager.TakeWorkers(pickup.amount) == pickup.amount;

            return PlayerResources.GetAmountStored(pickup.type) >= pickup.amount;
        }

        public override void StoreResource(ResourceAmount resource)
        {
            int max = PlayerResources.GetStorageLimit(resource.type);
            int current = PlayerResources.GetAmountStored(resource.type);

            if (current + resource.amount >= max)
                resource.amount = max - current;

            AddResourceToList(resource, PlayerResources.resourceStored);
            resourceAdded?.Invoke(resource);
        }

        public override bool DeliverResource(ResourceAmount resource)
        {
            if (resource.type == ResourceType.Workers)
            {
                numberOfWorkers += resource.amount;
                if (requiredNumberOfWorkers == numberOfWorkers)
                    isFunctional = true;
                workersMoving -= resource.amount;
                WorkerManager.DeliverWorkers(resource.amount);
                DisplayWarning();
            }
            else
            {
                RemoveResourceFromList(resource, PlayerResources.resourceInTransit);
                AddResourceToList(resource, PlayerResources.resourceStored);
            }
            resourceAdded?.Invoke(resource);
            return true;
        }

        public override void ReserveForDelivery(ResourceAmount deliver)
        {
            if (deliver.type == ResourceType.Workers)
            {
                workersMoving += deliver.amount;
                WorkerManager.DeliverWorkers(deliver.amount);
                return;
            }

            AddResourceToList(deliver, PlayerResources.resourceInTransit);
            RemoveResourceFromList(deliver, PlayerResources.resourceRequested);
        }

        public override ResourceAmount PickupResource(ResourceAmount resource)
        {
            if(resource.type == ResourceType.Workers)
            {
                return resource;
            }

            if (GetAmountStored(resource.type) >= resource.amount)
            {
                RemoveResourceFromList(resource, PlayerResources.resourceInTransit);
                resourceRemoved?.Invoke(resource);
                return resource;
            }
            else
            {
                ResourceAmount r = new ResourceAmount(resource.type, GetAmountStored(resource.type));
                RemoveResourceFromList(r, PlayerResources.resourceInTransit);
                resourceRemoved?.Invoke(r);
                return r;
            }
        }
        public override void ReserveForPickup(ResourceAmount pickUp)
        {
            if(pickUp.type == ResourceType.Workers)
            {
                //workersMoving += pickUp.amount;
                return;
            }

            if (GetAmountStored(pickUp.type) >= pickUp.amount)
            {
                RemoveResourceFromList(pickUp, PlayerResources.resourceStored);
                AddResourceToList(pickUp, PlayerResources.resourceInTransit);
            }
            else
            {
                ResourceAmount newPickup = new ResourceAmount(pickUp.type, GetAmountStored(pickUp.type));
                RemoveResourceFromList(newPickup, PlayerResources.resourceStored);
                AddResourceToList(newPickup, PlayerResources.resourceInTransit);
            }
        }

        public override bool HasAllResources(List<ResourceAmount> resourceList)
        {
            foreach (var resource in resourceList)
            {
                if (!HasResource(resource))
                    return false;
            }

            return true;
        }
        public override bool HasResource(ResourceAmount resource)
        {
            foreach (var r in PlayerResources.resourceStored)
            {
                if (r.type == resource.type && r.amount >= resource.amount)
                    return true;
            }

            return false;
        }

        public override int GetAmountStored(ResourceType type)
        {
            return PlayerResources.GetAmountStored(type);
        }

        protected override void AddStartingConnection()
        {
            BuildingSpotBehavior[] buildingSpots = FindObjectsOfType<BuildingSpotBehavior>()
                .Where(x => HelperFunctions.HexRangeFloat(x.transform.position, this.transform.position) <= CargoManager.transportRange)
                .ToArray();

            foreach (var bs in buildingSpots)
                this.AddDeliverConnection(bs.GetComponent<UnitStorageBehavior>());
        }

        public override int GetWorkersNeed()
        {
            if (!this.gameObject.activeInHierarchy)
                return 0;

            int localCount = GetIntStat(Stat.workers) - numberOfWorkers;
            Debug.Log($"Needs {localCount} workers", this.gameObject);
            return localCount;
        }

        public override int GetWorkerTotal()
        {
            return numberOfWorkers;
        }
    }

}