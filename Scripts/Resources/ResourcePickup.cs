using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Resources;
using System;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;

namespace HexGame.Units
{

    [RequireComponent(typeof(ResourceUnit))]
    public class ResourcePickup : UnitBehavior, IPoolable<ResourcePickup>
    {
        [SerializeField]
        private ResourceAmount resourceStart;

        public ResourceType resourceType => resourceStart.type;
        public int amount => resourceStart.amount;
        public static event Action<ResourcePickup> pickUpCreated;
        private Action<ResourcePickup> returnToPool;
        private UnitStorageBehavior usb;
        public bool inUse;

        [SerializeField] private bool useDeactivation = false;
        [SerializeField, ShowIf("useDeactivation")] private float deactivationTime = 300f;
        public static event Action<ResourcePickup> resourceDeactivated;


        private void Awake()
        {
            usb = this.GetComponent<UnitStorageBehavior>();
            usb.createStartingConnection = false;
        }

        private void OnEnable()
        {
            if(useDeactivation)
                StartCoroutine(DeactivateAfterTime());
        }

        private void OnDisable()
        {
            if(resourceStart.type != ResourceType.Terrene)
                usb.resourcePickedUp -= ToggleOff;

            StopAllCoroutines();
            ReturnToPool();
        }

        private void ToggleOff(UnitStorageBehavior usb, ResourceAmount resource)
        {
            this.gameObject.SetActive(false);
        }

        public override void StartBehavior()
        {
            isFunctional = true;
            inUse = false;
            StartCoroutine(DelayPickUpCall());
        }

        IEnumerator DelayPickUpCall()
        {
            yield return null;
            pickUpCreated?.Invoke(this);
        }

        public override void StopBehavior()
        {
            isFunctional = false;
        }

        public void SetResource(ResourceAmount resource)
        {
            resourceStart = resource;

            if (resourceStart.type != ResourceType.Terrene)
            {
                usb.cargoWithoutConnection = true;
                usb.createStartingConnection = false;
                usb.resourcePickedUp += ToggleOff;
                usb.AddResourceForPickup(resourceStart);
                usb.AddToPickUpTypes(resourceStart.type);
                usb.AddToAllowedTypes(resourceStart.type);
            }
        }

        public void Initialize(Action<ResourcePickup> returnAction)
        {
            //cache reference to return action
            this.returnToPool = returnAction;
        }

        public void ReturnToPool()
        {
            //invoke and return this object to pool
            returnToPool?.Invoke(this);
        }

        private IEnumerator DeactivateAfterTime()
        {
            yield return new WaitForSeconds(deactivationTime);
            resourceDeactivated?.Invoke(this);
            this.gameObject.SetActive(false);
        }
    }

}