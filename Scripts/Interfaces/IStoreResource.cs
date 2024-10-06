using System.Collections.Generic;

namespace HexGame.Resources
{

    public interface IStoreResource
    {
        void ReserveForDelivery(ResourceAmount resource);
        void ReserveForPickup(ResourceAmount resource);

        bool DeliverResource(ResourceAmount resource);
        ResourceAmount PickupResource(ResourceAmount resource);

        bool CanStoreResource(ResourceAmount resource);
        bool CanPickUpResource(ResourceAmount resource);
        void StoreResource(ResourceAmount resource);
        bool HasAllResources(List<ResourceAmount> resources);
        bool HasResource(ResourceAmount resource);

        CargoManager.RequestPriority GetPriority();
        void RequestWorkers();


        public float efficiency
        {
            get
            {
                return 1;
            }
        }

        public WorkerStatus workerStatus => WorkerStatus.none;

    }

    public enum WorkerStatus
    {
        none,
        some,
        all
    }

}
