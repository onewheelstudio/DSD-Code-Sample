using System.Collections.Generic;

namespace HexGame.Resources
{

    public interface IStoreResource
    {
        void ReserveForDelivery(ResourceAmount resource);
        void ReserveForPickup(ResourceAmount resource);

        bool DeliverResource(ResourceAmount resource);
        ResourceAmount PickupResource(ResourceAmount resource);

        void StoreResource(ResourceAmount resource);
        bool HasAllResources(List<ResourceAmount> resources);
        bool HasResource(ResourceAmount resource);

       bool CanDeliverResource(ResourceAmount deliver);

        /// <summary>
        /// Can the the resource be stored for pickip?
        /// </summary>
        /// <param name="pickUp"></param>
        /// <returns></returns>
        bool CanPickupResource(ResourceAmount pickUp);

        CargoManager.RequestPriority GetPriority();


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
