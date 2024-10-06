using HexGame.Resources;
using OWS.Nova;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HexGame.Units
{
    /// <summary>
    /// Designed as small storage for units that use resources
    /// Not designed for storage buildings?
    /// </summary>
    public class UnitStorageBehavior : UnitBehavior, IStoreResource, ISelfValidator, IHaveRequestPriority, IHaveResources
    {
        [SerializeField]
        private Transform landingPad;
        public Vector3 landingPadPosition => landingPad != null ? landingPad.position : this.transform.position;

        [SerializeField]
        private CargoManager.RequestPriority requestPriority = CargoManager.RequestPriority.medium;

        [SerializeField]
        private List<ResourceAmount> resourceStored = new List<ResourceAmount>();
        private List<ResourceAmount> resourceInTransit = new List<ResourceAmount>();
        private List<ResourceAmount> resourceRequested = new List<ResourceAmount>();
        private List<ResourceAmount> resourcePickup = new List<ResourceAmount>();

        [SerializeField] private List<ResourceAmount> storageLimits = new List<ResourceAmount>();

        [SerializeField]
        protected bool allowAllTypes = false;
        [SerializeField]
        [HideIf("allowAllTypes")]
        protected List<ResourceType> allowedTypes = new List<ResourceType>();
        [SerializeField]
        protected List<ResourceType> pickUpTypes = new List<ResourceType>();
        [SerializeField] private bool alwaysFillUp = false;

        public event System.Action<UnitStorageBehavior, ResourceAmount> resourceUsed;
        public event System.Action<UnitStorageBehavior, ResourceAmount> resourceDelivered;
        public event System.Action<UnitStorageBehavior, ResourceAmount> resourcePickedUp;
        public static event System.Action<UnitStorageBehavior> unitStorageAdded;
        public static event System.Action<UnitStorageBehavior> unitStorageRemoved;

        private StatusIndicator statusIndicator;

        [Header("Delivery Locations")]
        [SerializeField]private List<UnitStorageBehavior> connections = new List<UnitStorageBehavior>();
        protected static CargoManager cargoManager;
        public event Action<UnitStorageBehavior> connectionChanged;
        public static Action<UnitStorageBehavior, UnitStorageBehavior> connectionAdded;
        public static Action<UnitStorageBehavior, UnitStorageBehavior> connectionRemoved;
        public static event Action<UnitStorageBehavior> startListeningForConnection;
        public static event Action<UnitStorageBehavior> stopListeningForConnection;
        private UIControlActions uiControls;
        public bool createStartingConnection = true;
        public bool cargoWithoutConnection = false;
        public List<CargoShuttleBehavior> shuttles = new List<CargoShuttleBehavior>();

        public float efficiency
        {
            get
            {
                if (GetStat(Stat.workers) <= 0.01f)
                    return 1f;
                else
                {
                    return (float)GetAmountStored(ResourceType.Workers) / (float)GetStat(Stat.workers);
                }
            }
        }
        protected void OnValidate()
        {
            //if (TryGetComponent<ResourceProductionBehavior>(out ResourceProductionBehavior rpb))
            //{
            //    allowedTypes.Clear();
            //    pickUpTypes.Clear();
            //    foreach (var resource in rpb.GetResourcesUsed())
            //    {
            //        allowedTypes.Add(resource.type);
            //    }

            //    foreach (var resource in rpb.GetResourcesProduced())
            //    {
            //        allowedTypes.Add(resource.type);
            //        pickUpTypes.Add(resource.type);
            //    }
            //}
        }

        private void Awake()
        {
            if(cargoManager == null)
                cargoManager = FindObjectOfType<CargoManager>();
            uiControls = new UIControlActions();
        }

        private void OnEnable()
        {
            if (statusIndicator == null)
                statusIndicator = this.GetComponentInChildren<StatusIndicator>();

            InitializeResources(allowedTypes);
            if (requestPriority == CargoManager.RequestPriority.off) //allows tiles to be set to low by default
                requestPriority = CargoManager.RequestPriority.medium;

            uiControls.UI.RightClick.canceled += StopListeningForConnection;
            UnitStorageBehavior.unitStorageRemoved += CleanUpAllConnections;
            GlobalStorageBehavior.gloablStorageRemoved += CleanUpAllConnections;
        }



        protected void OnDisable()
        {
            StopBehavior();

            //recoup the cost of the resources if not a building spot
            if(!this.gameObject.GetComponent<BuildingSpotBehavior>() && !this.gameObject.GetComponent<PlaceHolderTileBehavior>())
                FindObjectOfType<CargoManager>()?.PlaceCubes(resourceStored, this.transform.position, 1f);
            
            ClearResources();
            uiControls.UI.RightClick.canceled -= StopListeningForConnection;
            UnitStorageBehavior.unitStorageRemoved -= CleanUpAllConnections;
            GlobalStorageBehavior.gloablStorageRemoved -= CleanUpAllConnections;
        }

        public override void StartBehavior()
        {
            LandingPad lp = this.GetComponentInChildren<LandingPad>();
            if (lp != null)
                landingPad = lp.transform;

            unitStorageAdded?.Invoke(this);
            isFunctional = true;
            AddStartingConnection();
        }

        public override void StopBehavior()
        {
            unitStorageRemoved?.Invoke(this);
            isFunctional = false;
        }

        public void InitializeResources(List<ResourceType> resources)
        {
            resourceStored.Clear();
            foreach (ResourceType resource in resources)
            {
                resourceStored.Add(new ResourceAmount(resource, 0));
            }
        }

        public bool DeliverResources(List<ResourceAmount> resources)
        {
            foreach (var resource in resources)
            {
                if(!allowAllTypes && !allowedTypes.Contains(resource.type))
                    continue;

                AddResourceToList(resource, resourceStored);
                resourceDelivered?.Invoke(this, resource);
                if (resource.type == ResourceType.Workers)
                    WorkerManager.DeliverWorkers(resource.amount);
            }

            return true;
        }

        public virtual bool DeliverResource(ResourceAmount resource)
        {
            if (!allowAllTypes && !allowedTypes.Contains(resource.type) && resource.type != ResourceType.Workers)
                return false;

            RemoveResourceFromList(resource, resourceInTransit);
            AddResourceToList(resource, resourceStored);
            resourceDelivered?.Invoke(this, resource);
            if (resource.type == ResourceType.Workers)
                WorkerManager.DeliverWorkers(resource.amount);
            return true;
        }

        public void AddResourceForPickup(ResourceAmount resource)
        {
            AddResourceToList(resource, resourceInTransit);
            CargoManager.MakeRequest(this, resource, CargoManager.RequestType.pickup);
        }

        /// <summary>
        /// Used by the supply ship to request a pickup of a given resource.
        /// </summary>
        /// <param name="resource"></param>
        public void RequestPickup(List<ResourceAmount> resources)
        {
            foreach (var resource in resources)
            {
                CargoManager.MakeRequest(this, new ResourceAmount(resource.type, GetAmountStored(resource.type)), CargoManager.RequestType.pickup);
            }
        }
        
        public void RequestPickup(ResourceAmount resources)
        {
            CargoManager.MakeRequest(this, resources, CargoManager.RequestType.pickup);
        }

        /// <summary>
        /// used by supply ship to cancel requests for failed quest
        /// </summary>
        /// <param name="resources"></param>
        public void RemoveRequestForResources(List<ResourceAmount> resources)
        {
            foreach (var resource in resources)
            {
                for (var i = resourceRequested.Count - 1; i >= 0; i--)
                {
                    if (resourceRequested[i].type == resource.type)
                        resourceRequested.RemoveAt(i);
                }
            }
        }

        public virtual void ReserveForDelivery(ResourceAmount deliver)
        {
            AddResourceToList(deliver, resourceInTransit);
            RemoveResourceFromList(deliver, resourceRequested);
        }

        public virtual ResourceAmount PickupResource(ResourceAmount resource)
        {
            if(resource.type == ResourceType.Workers)
            {
                resource.amount = WorkerManager.TakeWorkers(resource.amount);
            }

            if (GetAmountInTransit(resource.type) >= resource.amount)
            {
                RemoveResourceFromList(resource, resourceInTransit);
                resourcePickedUp?.Invoke(this, resource);
                return resource;
            }
            else
            {
                ResourceAmount r = new ResourceAmount(resource.type, GetAmountInTransit(resource.type));
                RemoveResourceFromList(r, resourceInTransit);
                resourcePickedUp?.Invoke(this, r);
                return r;
            }
        }

        public virtual void ReserveForPickup(ResourceAmount pickUp)
        {
            if (GetAmountStored(pickUp.type) >= pickUp.amount)
            {
                RemoveResourceFromList(pickUp, resourceStored);
                AddResourceToList(pickUp, resourceInTransit);
            }
            else
            {
                ResourceAmount newPickup = new ResourceAmount(pickUp.type, GetAmountStored(pickUp.type));
                RemoveResourceFromList(newPickup, resourceStored);
                AddResourceToList(newPickup, resourceInTransit);
            }
        }

        public void CheckResourceLevels()
        {
            RequestWorkers();

            foreach (var resource in allowedTypes)
            {
                if (resource == ResourceType.Workers)
                    continue;

                int amount = (int)GetResourceStorageLimit(resource) - GetResourceTotal(resource);

                if (amount <= 0)
                    continue;

                if (alwaysFillUp)
                {
                    //this needs a better solution...
                    CargoManager.MakeRequest(this, new ResourceAmount(resource, amount), CargoManager.RequestType.deliver);
                    AddResourceToList(new ResourceAmount(resource, amount), resourceRequested);
                }
                else if (GetResourceStorageLimit(resource) - GetResourceTotal(resource) >= CargoManager.transportAmount)
                {
                    CargoManager.MakeRequest(this, new ResourceAmount(resource, CargoManager.transportAmount), CargoManager.RequestType.deliver);
                    AddResourceToList(new ResourceAmount(resource, CargoManager.transportAmount), resourceRequested);
                }
            }
        }

        public void CheckResourceLevels(ResourceAmount resource)
        {
            if (GetResourceStorageLimit(resource) - GetResourceTotal(resource.type) >= CargoManager.transportAmount)
            {
                CargoManager.MakeRequest(this, new ResourceAmount(resource.type, CargoManager.transportAmount), CargoManager.RequestType.deliver);
                AddResourceToList(new ResourceAmount(resource.type, CargoManager.transportAmount), resourceRequested);
            }
        }

        public void RequestWorkers()
        {
            if (GetStat(Stat.workers) == 0)
                return;

            int requestAmount = (int)GetStat(Stat.workers) - GetResourceTotal(ResourceType.Workers);
            if (requestAmount > 0)
            {
                CargoManager.MakeRequest(this, new ResourceAmount(ResourceType.Workers, requestAmount), CargoManager.RequestType.deliver);
                AddResourceToList(new ResourceAmount(ResourceType.Workers, requestAmount), resourceRequested);
            }
        }

        public void RequestMaxWorkers()
        {
            if (GetStat(Stat.housing) == 0)
                return;

            ResourceAmount colonists = resourceStored.Where(x => x.type == ResourceType.Workers).FirstOrDefault();
            colonists.amount = GetIntStat(Stat.housing) - colonists.amount;

            if (GetResourceStorageLimit(colonists) - GetResourceTotal(colonists.type) >= 0)
            {
                Debug.Log($"requesting {colonists.amount} workers");
                CargoManager.MakeRequest(this, colonists, CargoManager.RequestType.deliver);
                AddResourceToList(new ResourceAmount(colonists.type, CargoManager.transportAmount), resourceRequested);
            }
        }
                
        public bool RequestResource(ResourceAmount resource)
        {
            if (GetResourceStorageLimit(resource) - GetResourceTotal(resource.type) >= 0)
            {
                CargoManager.MakeRequest(this, resource, CargoManager.RequestType.deliver);
                AddResourceToList(resource, resourceRequested);
                return true;
            }
            else
                return false;
        }

        public virtual int GetAmountStored(ResourceType type)
        {
            foreach (var resource in resourceStored)
            {
                if (type == resource.type)
                    return resource.amount;
            }

            return 0;
        }

        private int GetAmountInTransit(ResourceType type)
        {
            foreach (var resource in resourceInTransit)
            {
                if (type == resource.type)
                    return resource.amount;
            }

            return 0;
        }

        private int GetAmountRequested(ResourceType type)
        {
            foreach (var resource in resourceRequested)
            {
                if (type == resource.type)
                    return resource.amount;
            }

            return 0;
        }

        internal void DestroyResource(ResourceAmount resourceAmount)
        {
            if (HasResource(resourceAmount))
                RemoveResourceFromList(resourceAmount, resourceStored);
        }

        public virtual int TotalStored()
        {
            int total = 0;
            foreach (var resource in resourceStored)
                total += resource.amount;

            //foreach (var resource in resouceInTransit)
            //    total += resource.amount;

            //foreach (var resource in resourceRequested)
            //    total += resource.amount;

            return total;
        }
        
        public int GetResourceTotal(ResourceType type)
        {
            int total = 0;
            foreach (var resource in resourceStored)
            {
                if (resource.type == type)
                    total += resource.amount;
            }
            foreach (var resource in resourceInTransit)
            {
                if (resource.type == type)
                    total += resource.amount;
            }
            foreach (var resource in resourceRequested)
            {
                if (resource.type == type)
                    total += resource.amount;
            }
            return total;
        }

        public virtual int GetWorkerTotal()
        {
            int total = 0;
            total += resourceStored.Where(x => x.type == ResourceType.Workers).FirstOrDefault().amount;
            total += resourceInTransit.Where(x => x.type == ResourceType.Workers).FirstOrDefault().amount;
            return total;
        }

        public virtual int GetWorkersNeed()
        {
            int workersNeeded = 0;
            foreach (var resource in resourceRequested)
            {
                if (resource.type == ResourceType.Workers)
                    workersNeeded += resource.amount;
            }

            return workersNeeded;
        }

        public bool TryUseAllResources(List<ResourceAmount> resourceList)
        {
            if (resourceList.Count == 0)
                return true;

            if (!HasAllResources(resourceList))
                return false;

            foreach (var resource in resourceList)
            {
                RemoveResourceFromList(resource, resourceStored);
                resourceUsed?.Invoke(this, resource);
            }

            return true;
        }

        public bool TryUseResource(ResourceAmount resource)
        {
            if (!HasResource(resource))
                return false;

            RemoveResourceFromList(resource, resourceStored);
            resourceUsed?.Invoke(this, resource);

            return true;
        }

        public virtual bool HasAllResources(List<ResourceAmount> resourceList)
        {
            foreach (var resource in resourceList)
            {
                if (!HasResource(resource))
                    return false;
            }

            return true;
        }
        public virtual bool HasResource(ResourceAmount resource)
        {
            foreach (var r in resourceStored)
            {
                if (r.type == resource.type && r.amount >= resource.amount)
                    return true;
            }

            return false;
        }

        public bool TryStoreResource(ResourceAmount resource)
        {
            if (!CanStoreResource(resource))
                return false;
            else
            {
                StoreResource(resource);
                return true;
            }
        }

        public virtual bool CanStoreResource(ResourceAmount deliver)
        {
            if (!allowAllTypes && !allowedTypes.Contains(deliver.type))
                return false;
            //I think GetAmountStored should be GetResourceTotal
            if (GetResourceStorageLimit(deliver) - GetResourceTotal(deliver.type) >= deliver.amount)
                return true;
            else
                return false;
        }

        public float GetResourceStorageLimit(ResourceAmount resourceType)
        {
            return GetResourceStorageLimit(resourceType.type);
        }
        
        public float GetResourceStorageLimit(ResourceType resourceType)
        {
            float maxStorage;
            //is there limit on this type of resource
            if (storageLimits.Any(x => x.type == resourceType))
                maxStorage = storageLimits.Where(x => x.type == resourceType).First().amount;
            else
                maxStorage = GetStat(Stat.maxStorage);
            return maxStorage;
        }

        public virtual bool CanPickUpResource(ResourceAmount deliver)
        {
            if (!pickUpTypes.Contains(deliver.type))
                return false;

            if (GetAmountStored(deliver.type) >= deliver.amount)
                return true;
            else
                return false;
        }


        /// <summary>
        /// Used to request a delivery of a given resource.
        /// </summary>
        /// <param name="resource"></param>
        public void MakeDeliveryRequest(ResourceAmount resource, bool totalRequest = false)
        {
            //checks how much is currently stored or requested
            if (totalRequest)
            {
                int amount = GetAmountStored(resource.type) + GetAmountRequested(resource.type);
                resource = new ResourceAmount(resource.type, resource.amount - amount);
            }

            if(resource.amount <= 0)
                return;

            CargoManager.MakeRequest(this, resource, CargoManager.RequestType.deliver);
            AddResourceToList(new ResourceAmount(resource.type, resource.amount), resourceRequested);
        }

        public virtual void StoreResource(ResourceAmount resource)
        {
            //attemp to filter deliveries larger than the transport amount
            if(resource.amount > CargoManager.transportAmount)
            {
                //break up in to smaller chunks
                int loadsNeeded = resource.amount / CargoManager.transportAmount;
                int remainder = resource.amount % CargoManager.transportAmount;
                for (int i = 0; i < loadsNeeded; i++)
                {
                    StoreResource(new ResourceAmount(resource.type, CargoManager.transportAmount));
                }

                if(remainder > 0)
                    StoreResource(new ResourceAmount(resource.type, remainder));

                return;
            }

            //get amount until pickup BEFORE delivery
            int amountUntilPickup = CargoManager.transportAmount - GetAmountStored(resource.type) % CargoManager.transportAmount;

            if (resource.amount + GetResource(resource.type).amount <= GetResourceStorageLimit(resource))
            {
                AddResourceToList(resource, resourceStored);
                resourceDelivered?.Invoke(this, resource);
            }
            else
            {
                SetResourceAmount(resource.type, (int)GetResourceStorageLimit(resource));
                resourceDelivered?.Invoke(this, new ResourceAmount(resource.type, 1));
            }

            if (amountUntilPickup > 0 && amountUntilPickup <= resource.amount)
            {
                CargoManager.MakeRequest(this, new ResourceAmount(resource.type, CargoManager.transportAmount), CargoManager.RequestType.pickup);
            }
        }

        public ResourceAmount GetResource(ResourceType type)
        {
            foreach (var resource in resourceStored)
            {
                if (type == resource.type)
                    return resource;
            }

            return new ResourceAmount();
        }

        private void SetResourceAmount(ResourceType type, int amount)
        {
            for (int i = 0; i < resourceStored.Count; i++)
            {
                if (resourceStored[i].type == type)
                    resourceStored[i] = new ResourceAmount(resourceStored[i].type, amount);
            }
        }

        private void ClearResources()
        {
            resourceStored.Clear();
            resourceRequested.Clear();
            resourceInTransit.Clear();
        }

        protected void AddResourceToList(ResourceAmount resource, List<ResourceAmount> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (resource.type == list[i].type)
                {
                    list[i] += resource;
                    if (list[i].amount < 0)
                        Debug.Log($"resource is negative {list[i].amount}");
                    return;
                }
            }

            list.Add(resource);
        }

        protected void RemoveResourceFromList(ResourceAmount resource, List<ResourceAmount> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (resource.type == list[i].type)
                {
                    list[i] -= resource;
                    if (list[i].amount < 0)
                        Debug.Log($"{resource.type} is negative {list[i].amount}", this.gameObject); 
                    return;
                }
            }
        }

        public void Validate(SelfValidationResult result)
        {
            if (!allowAllTypes && allowedTypes.Count == 0)
                result.AddError("Is not global storage and no allowed types set.");
        }

        public RequestStorageInfo GetPopUpRequestPriority()
        {
            return new RequestStorageInfo()
            {
                priority = this.requestPriority,
                setPriority = SetRequestPriority,
                getPriority = GetPriority,
                connections = connections,
                startAddConnection = StartAddConnection,
            };
        }

        protected virtual void AddStartingConnection()
        {
            if (this.TryGetComponent<GlobalStorageBehavior>(out GlobalStorageBehavior gsb))
                return;

            if (!this.createStartingConnection)
                return;
            
            if(this.GetComponent<BuildingSpotBehavior>() == null)
            {
                GlobalStorageBehavior globalStorageBehavior = FindObjectsOfType<GlobalStorageBehavior>()
                                                .OrderBy(x => Vector3.Distance(x.transform.position, this.transform.position))
                                                .FirstOrDefault();

                if (globalStorageBehavior == null)
                    return;

                //suppressing warnings for connection only for building spots
                AddDeliverConnection(globalStorageBehavior, true);

                if(HasIncomingDelivery(this) || this.allowAllTypes)
                    globalStorageBehavior.AddDeliverConnection(this, true);
            }
            else //building spots connect to all nearby global storage
            {
                GlobalStorageBehavior[] globalStorageBehaviors = FindObjectsOfType<GlobalStorageBehavior>()
                                .Where(x => HelperFunctions.HexRangeFloat(x.transform.position, this.transform.position) <= CargoManager.transportRange)
                                .ToArray();

                foreach (var gs in globalStorageBehaviors)
                    gs.AddDeliverConnection(this);
            }

        }

        //compares allowed types to pick up types to determine if deliveries can be made
        private bool HasIncomingDelivery(UnitStorageBehavior usb)
        {
            foreach (var resouces in usb.allowedTypes)
            {
                //if (resouces == ResourceType.Terrene)
                //    return true;//special flower :) This is for the collection tower.

                if(!usb.pickUpTypes.Contains(resouces))
                    return true;
            }

            return false;
        }

        private void StartAddConnection()
        {
            if (UnitSelectionManager.addConnection)
            {
                StopListeningForConnection();
                return;
            }

            UnitSelectionManager.addConnection = true;
            UnitSelectionManager.unitClicked += AddNewConnection;
            uiControls.Enable();
            startListeningForConnection?.Invoke(this);
        }
        private void StopListeningForConnection(InputAction.CallbackContext context)
        {
            StopListeningForConnection();
        }
        private void StopListeningForConnection()
        {
            UnitSelectionManager.addConnection = false;
            UnitSelectionManager.unitClicked -= AddNewConnection;
            uiControls?.Disable();
            stopListeningForConnection?.Invoke(this);
        }

        private void AddNewConnection(PlayerUnit playerUnit)
        {
            if (playerUnit.gameObject == this.gameObject)
                return;
            UnitStorageBehavior usb = playerUnit.GetComponent<UnitStorageBehavior>();
            AddDeliverConnection(usb);
            if(usb is GlobalStorageBehavior) //only double connect if it's a global storage
                usb.AddDeliverConnection(this);
        }

        public void AddDeliverConnection(UnitStorageBehavior usb, bool suppressWarning = false)
        {
            if (connections.Contains(usb))
                return;

            if(usb == null)
                return;

            if (usb == this)
                return;

            if (!suppressWarning && HelperFunctions.HexRange(this.transform.position, usb.transform.position) > CargoManager.transportRange)
            {
                MessagePanel.ShowMessage("Connection out of range", this.gameObject);
                SFXManager.PlaySFX(SFXType.error);
                return;
            }

            connections.Add(usb);
            connectionAdded?.Invoke(this, usb);
            if (UnitSelectionManager.selectedUnit?.gameObject == this.gameObject)
                connectionChanged?.Invoke(this);

            if (!Keyboard.current.shiftKey.isPressed)
                StopListeningForConnection();
        }

        internal void MoveConnectionDown(int index)
        {
            if(index == connections.Count - 1)
                return;

            UnitStorageBehavior unitStorageBehavior = connections[index];
            UnitStorageBehavior unitStorageBehavior2 = connections[index + 1];
            connections[index] = unitStorageBehavior2;
            connections[index + 1] = unitStorageBehavior;

            connectionChanged?.Invoke(this);
        }

        internal void MoveConnectionUp(int index)
        {
            if(index == 0)
                return;

            UnitStorageBehavior unitStorageBehavior = connections[index];
            UnitStorageBehavior unitStorageBehavior2 = connections[index - 1];
            connections[index] = unitStorageBehavior2;
            connections[index - 1] = unitStorageBehavior;

            connectionChanged?.Invoke(this);
        }

        /// <summary>
        /// removes all local connections and the connections from the other storage
        /// </summary>
        /// <param name="behavior"></param>
        public void CleanUpAllConnections(UnitStorageBehavior behavior)
        {
            if(behavior == this) 
            {
                foreach (var connection in connections)
                {
                    connection.RemoveConnectionFromList(this);
                }
                connections.Clear();
                if(UnitSelectionManager.selectedUnit?.gameObject == this.gameObject)
                    connectionChanged?.Invoke(this);
            }
            else if(this.connections.Contains(behavior))
            {
                this.RemoveConnectionFromList(behavior);
            }
        }

        /// <summary>
        /// removes local connection and the connection from the other storage
        /// </summary>
        /// <param name="behavior"></param>
        public void RemoveConnection(int index)
        {
            if(index >= connections.Count)
                return;

            if (connections[index] is GlobalStorageBehavior)
                connections[index].RemoveConnectionFromList(this);
            
            connectionRemoved?.Invoke(this, connections[index]);
            RemoveConnectionFromList(connections[index]);
        }

        //removes only the local connection
        public void RemoveConnectionFromList(UnitStorageBehavior behavior)
        {
            connections.Remove(behavior);
            connectionRemoved?.Invoke(this, behavior);
            if (UnitSelectionManager.selectedUnit?.gameObject == this.gameObject)
                connectionChanged?.Invoke(this);
        }

        public void SetRequestPriority(CargoManager.RequestPriority requestType)
        {
            this.requestPriority = requestType;
        }

        public CargoManager.RequestPriority GetPriority()
        {
            return this.requestPriority;
        }

        public void SetAllowedTypes(List<ResourceType> resourceTypes)
        {
            this.allowedTypes = resourceTypes;
        }

        public IReadOnlyCollection<ResourceType> GetAllowedTypes()
        {
            if(allowAllTypes)
            {
                List<ResourceType> _allowedTypes = new List<ResourceType>();
                foreach (ResourceType resource in System.Enum.GetValues(typeof(ResourceType)))
                    _allowedTypes.Add(resource);
                return _allowedTypes.AsReadOnly();
            }
            else
                return allowedTypes.AsReadOnly();
        }

        public List<PopUpResource> GetPopUpResources()
        {
            List<PopUpResource> resourceInfos = new List<PopUpResource>();
            foreach (var resource in resourceStored)
            {
                if(resource.amount == 0 && !allowedTypes.Contains(resource.type) && !pickUpTypes.Contains(resource.type))
                    continue;

                resourceInfos.Add(new PopUpResource(resource, GetResourceStorageLimit(resource), 0, Color.white));
            }

            foreach (var resource in allowedTypes)
            {
                if(!ListContainsResource(resourceStored, resource))
                    resourceInfos.Add(new PopUpResource(new ResourceAmount(resource, 0), GetResourceStorageLimit(resource), 0, Color.white));
            }

            return resourceInfos;
        }

        private bool ListContainsResource(List<ResourceAmount> list, ResourceType type)
        {
            foreach (var resource in list)
            {
                if (resource.type == type)
                    return true;
            }

            return false;
        }

        public void SetLandingPosition(Transform landingPad)
        {
            this.landingPad = landingPad;
        }

        internal void AdjustStorageForRecipe(ResourceProduction oldRecipe, ResourceProduction newRecipe)
        {
            foreach (var resource in oldRecipe.GetCost())
            {
                allowedTypes.Remove(resource.type);
                if(!RecipeContains(newRecipe, resource.type))
                    RequestPickUpOfAllResource(resource.type);
            }

            foreach (var resource in oldRecipe.GetProduction())
            {
                allowedTypes.Remove(resource.type);
                pickUpTypes.Remove(resource.type);
                if (!RecipeContains(newRecipe, resource.type))
                    RequestPickUpOfAllResource(resource.type);
            }

            foreach (var resource in newRecipe.GetCost())
            {
                if(!allowedTypes.Contains(resource.type))
                    allowedTypes.Add(resource.type);
                //make sure we don't accidentally pickup things we need
                pickUpTypes.Remove(resource.type);
                CheckResourceLevels(resource);
            }

            foreach (var resource in newRecipe.GetProduction())
            {
                if(!allowedTypes.Contains(resource.type))
                    allowedTypes.Add(resource.type);
                if(!pickUpTypes.Contains(resource.type))
                    pickUpTypes.Add(resource.type);
            }
        }

        public void AddToPickUpTypes(ResourceType resourceType)
        {
            if(!pickUpTypes.Contains(resourceType))
                pickUpTypes.Add(resourceType);
        }

        public void ClearPickupTypes()
        {
            pickUpTypes.Clear();
        }

        public void AddToAllowedTypes(ResourceType resourceType)
        {
            if(!allowedTypes.Contains(resourceType))
                allowedTypes.Add(resourceType);
        }
        public void ClearAllowedTypes()
        {
            allowedTypes.Clear();
        }

        private bool RecipeContains(ResourceProduction recipe, ResourceType type)
        {
            foreach (var resource in recipe.GetCost())
            {
                if (resource.type == type)
                    return true;
            }

            foreach (var resource in recipe.GetProduction())
            {
                if (resource.type == type)
                    return true;
            }

            return false;
        }

        public void RequestPickUpOfAllResource(ResourceType resourceType)
        {
            CargoManager.MakeRequest(this, new ResourceAmount(resourceType, GetAmountStored(resourceType)), CargoManager.RequestType.pickup);
        }

        public List<UnitStorageBehavior> GetConnections()
        {
            return connections;
        }

        public List<ConnectionStatusInfo> GetConnectionInfo()
        {
            List<ConnectionStatusInfo> connectionStatusInfos = new List<ConnectionStatusInfo>();
            foreach (var connection in connections)
            {
                ConnectionStatusInfo connectionStatusInfo = new ConnectionStatusInfo();
                connectionStatusInfo.storage = connection;
                connectionStatusInfo.status = GetConnectionStatus(connection);
                connectionStatusInfos.Add(connectionStatusInfo);
            }

            return connectionStatusInfos;
        }

        public ConnectionStatus GetConnectionStatus(UnitStorageBehavior destination)
        {
            if(!this.allowAllTypes && (this.allowedTypes == null || this.allowedTypes.Count == 0))
                return ConnectionStatus.unDeliverable;

            if (destination.allowAllTypes)
                return ConnectionStatus.deliverable;

            if(destination.allowedTypes.Except(destination.pickUpTypes).Count() == 0)
                return ConnectionStatus.unDeliverable;

            //deliverable
            if (this.allowAllTypes && destination.allowedTypes.Count > 0)
            {
                return ConnectionStatus.deliverable;

                //foreach (ResourceType resourceType in destination.allowedTypes)
                //{
                //    if (destination.CanStoreResource(new ResourceAmount(resourceType, 1)))
                //        return ConnectionStatus.deliverable;
                //    else
                //        return ConnectionStatus.storageFull;
                //}
            }
            else if (!this.allowAllTypes && destination.allowedTypes.Count > 0)
            {
                foreach (var resource in this.pickUpTypes)
                {
                    if (destination.allowedTypes.Contains(resource))// && destination.CanStoreResource(new ResourceAmount(resource, 1)))
                        return ConnectionStatus.deliverable;
                    //else if(destination.allowedTypes.Contains(resource))
                    //    return ConnectionStatus.storageFull;
                }
            }

            //if we got this far and its global storage, then we are full
            //if (destination is GlobalStorageBehavior)
            //    return ConnectionStatus.deliverable;
                //return ConnectionStatus.storageFull;
                
            //foreach (var resource in this.pickUpTypes)
            //{
            //    if (!destination.pickUpTypes.Contains(resource) && destination.allowedTypes.Contains(resource))
            //        return ConnectionStatus.storageFull;
            //}
            
            return ConnectionStatus.unDeliverable;
        }

        public ReadOnlyCollection<ResourceAmount> GetStoredResources()
        {
            return resourceStored.AsReadOnly();
        }
    }

    public class ConnectionStatusInfo
    {
        public UnitStorageBehavior storage;
        public ConnectionStatus status;
    }

    public enum ConnectionStatus
    {
        deliverable,
        storageFull,
        unDeliverable,
    }

}