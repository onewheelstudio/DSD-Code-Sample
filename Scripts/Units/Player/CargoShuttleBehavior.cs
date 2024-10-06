using DG.Tweening;
using HexGame.Resources;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexGame.Units
{
    public class CargoShuttleBehavior : UnitBehavior
    {
        public bool isWorking { get; private set; }
        public bool hasLoad { get; private set; }
        private HoverMoveBehavior hoverMove;
        private Vector3 startLocation;
        private WaitForSeconds wait = new WaitForSeconds(0.5f);
        private Coroutine returningHome;
        public CargoManager.Request request;
        [SerializeField]
        private Transform cargoParent;
        private GameObject activeCube;
        private AudioSource audioSource;
        private UnitStorageBehavior usb;
        public UnitStorageBehavior Usb => usb;
        private static CargoManager cm;

        private void Awake()
        {
            if (cm == null)
                cm = FindFirstObjectByType<CargoManager>();
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
            return isFunctional && _isFunctional && !isWorking && !hasLoad;
        }

        public void AddRequest(CargoManager.Request request, CargoManager.Request match)
        {
            this.request = request;

            if (IsAvailable() && request.requestType == CargoManager.RequestType.pickup)
                StartCoroutine(DoRequest(request, match));
            else if(IsAvailable() && request.requestType == CargoManager.RequestType.deliver)
                StartCoroutine(DoRequest(match, request));
        }

        IEnumerator DoRequest(CargoManager.Request pickup, CargoManager.Request delivery)
        {
            isWorking = true;

            if ((pickup.storage.transform.position - this.transform.position).sqrMagnitude > 1f)
                hoverMove.SetDestination(pickup.storage.landingPadPosition);
            pickup.storage.ReserveForPickup(pickup.resourceCapacity);
            delivery.storage.ReserveForDelivery(pickup.resourceCapacity);

            yield return new WaitWhile(() => hoverMove.isMoving);
            pickup.storage.PickupResource(pickup.resourceCapacity);
            hasLoad = true;
            //cargoBox.gameObject.SetActive(hasLoad);
            SetCargo(pickup.resourceCapacity.type, hasLoad);

            hoverMove.SetDestination(delivery.storage.landingPadPosition);
            yield return new WaitWhile(() => hoverMove.isMoving);
            delivery.storage.DeliverResource(pickup.resourceCapacity);
            hasLoad = false;
            //cargoBox.gameObject.SetActive(hasLoad);
            SetCargo(pickup.resourceCapacity.type, hasLoad);


            yield return wait;
            hoverMove.SetDestination(startLocation);
            yield return new WaitWhile(() => hoverMove.isMoving);
            isWorking = false;
        }

        public Vector3 GetStartLocation()
        {
            return startLocation;
        }

        private void SetCargo(ResourceType type, bool hasLoad)        
        {
            activeCube?.SetActive(false);
            if(hasLoad)
            {
                activeCube = cm.GetCargoCube(type);
                activeCube.transform.SetPositionAndRotation(cargoParent.position, cargoParent.rotation);
                activeCube.transform.SetParent(cargoParent);
            }
            else
                activeCube = null;
        }
    }

}