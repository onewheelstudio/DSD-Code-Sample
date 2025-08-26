using DG.Tweening;
using HexGame.Resources;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace HexGame.Units
{
    public class CargoShuttleBehavior : UnitBehavior
    {
        public bool isWorking { get; private set; }
        public bool hasLoad { get; private set; }
        private bool lookingForWork = false;
        private HoverMoveBehavior hoverMove;
        private Vector3 startLocation;
        private WaitForSeconds wait = new WaitForSeconds(0.5f);
        private Coroutine returningHome;
        public CargoManager.Request request;
        [SerializeField]
        private Transform cargoParent;
        private CargoCube activeCube;
        private AudioSource audioSource;
        private UnitStorageBehavior usb;
        public UnitStorageBehavior Usb => usb;
        private static CargoManager cm;
        private static PlayerResources playerResources;

        private void Awake()
        {
            if (cm == null)
                cm = FindFirstObjectByType<CargoManager>();
            if(playerResources == null)
                playerResources = FindFirstObjectByType<PlayerResources>();
        }


        private void OnEnable()
        {
            usb ??= this.GetComponentInParent<UnitStorageBehavior>();
            usb.shuttles.Add(this);

            if (hoverMove == null)
                hoverMove = this.GetComponent<HoverMoveBehavior>();
        }

        private void OnDisable()
        {
            usb.shuttles.Remove(this);
            DOTween.Kill(this,true);
        }

        public override void StartBehavior()
        {
            isFunctional = true;
            _isFunctional = true;
            startLocation = this.transform.position;
        }

        public override void StopBehavior()
        {
            isFunctional = false;
            _isFunctional = false;
        }

        public bool IsAvailable()
        {
            if (lookingForWork)
                return true;

            return isFunctional && !isWorking && !hasLoad;
        }

        public void AddRequest(CargoManager.Request request, CargoManager.Request match)
        {
            this.request = request;

            if (IsAvailable() && request.requestType == CargoManager.RequestType.pickup)
            {
                DoRequestAwaitable(request, match);
            }
            else if(IsAvailable() && request.requestType == CargoManager.RequestType.deliver)
            {
                DoRequestAwaitable(match, request);
            }
        }

        private async void DoRequestAwaitable(CargoManager.Request pickup, CargoManager.Request delivery)
        {
            isWorking = true;

            if ((pickup.storage.Position - this.transform.position).sqrMagnitude > 1f)
                hoverMove.SetDestination(pickup.storage.landingPadPosition);
            pickup.storage.ReserveForPickup(pickup.resourceCapacity);
            delivery.storage.ReserveForDelivery(pickup.resourceCapacity);

            while(hoverMove.isMoving)
            {
                await Awaitable.NextFrameAsync();
            }

            if (!IsRequestValid(pickup, delivery))
            {
                CompleteRequest();
                return;
            }

            pickup.storage.PickupResource(pickup.resourceCapacity);
            hasLoad = true;
            SetCargo(pickup.resourceCapacity.type, hasLoad);

            hoverMove.SetDestination(delivery.storage.landingPadPosition);
            while (hoverMove.isMoving)
            {
                await Awaitable.NextFrameAsync();
            }

            if (!IsRequestValid(pickup, delivery))
            {
                CompleteRequest();
                return;
            }

            if (delivery.storage.DeliverResource(pickup.resourceCapacity))
            {
                hasLoad = false;
                SetCargo(pickup.resourceCapacity.type, hasLoad);
            }
            else
            {
                playerResources.TryReturnResource(pickup.resourceCapacity);
                hasLoad = false;
                SetCargo(pickup.resourceCapacity.type, hasLoad);
            }

            if (!hasLoad)
                isWorking = false;

            await Awaitable.WaitForSecondsAsync(0.5f);
            if (!IsRequestValid(pickup, delivery))
            {
                CompleteRequest();
                return;
            }

            if (isWorking) //if this is true we got another job while we waited
            {
                return;
            }
            else
                isWorking = true;

            hoverMove.SetDestination(startLocation);
            while (hoverMove.isMoving)
            {
                await Awaitable.NextFrameAsync();
            }
            
            if (!IsRequestValid(pickup, delivery))
            {
                CompleteRequest();
                return;
            }

            if (hasLoad)
            {
                pickup.storage.DeliverResource(pickup.resourceCapacity);
                hasLoad = false;
                SetCargo(pickup.resourceCapacity.type, hasLoad);
            }
            isWorking = false;
        }

        private bool IsRequestValid(CargoManager.Request pickup, CargoManager.Request delivery)
        {
            if(pickup.storage == null || delivery.storage == null)
                return false;

            return pickup.storage.gameObject.activeInHierarchy && delivery.storage.gameObject.activeInHierarchy;
        }

        private async void CompleteRequest()
        {
            if (hoverMove == null || hoverMove.gameObject == null)
            {
                hasLoad = false;
                isWorking = false;
                SetCargo(ResourceType.Energy, false);
                return;
            }

            hoverMove.SetDestination(startLocation);
            while (hoverMove.isMoving)
            {
                await Awaitable.NextFrameAsync();
            }
            hasLoad = false;
            isWorking = false;
            SetCargo(ResourceType.Energy, false);
        }

        public Vector3 GetStartLocation()
        {
            return startLocation;
        }

        private void SetCargo(ResourceType type, bool hasLoad)        
        {
            if(activeCube != null && activeCube.gameObject != null)
                activeCube.ReturnToPool();
            if(hasLoad)
            {
                activeCube = cm.GetCargoCube(type);
                activeCube.transform.SetPositionAndRotation(cargoParent.position, cargoParent.rotation);
                activeCube.transform.SetParent(cargoParent);
            }
            else if(activeCube != null)
            {
                activeCube.transform.SetParent(null);
                activeCube = null;
            }
        }
    }

}