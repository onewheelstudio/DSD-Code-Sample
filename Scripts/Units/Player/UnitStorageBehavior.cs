using HexGame.Grid;
using HexGame.Resources;
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
    public class UnitStorageBehavior : UnitBehavior, IStoreResource, IHaveRequestPriority, IHaveResources, IHaveButtons
    {
        [SerializeField] private LandingPad[] landingPads;
        public Vector3 landingPadPosition => GetLandingPosition();

        [SerializeField]
        private CargoManager.RequestPriority requestPriority = CargoManager.RequestPriority.medium;
        private CargoManager.RequestPriority previousPriority = CargoManager.RequestPriority.medium;

        [SerializeField]
        protected List<ResourceAmount> resourceStored = new List<ResourceAmount>();
        protected List<ResourceAmount> resourceInTransit = new List<ResourceAmount>();
        protected List<ResourceAmount> resourcePickup = new List<ResourceAmount>();
        //protected object listLock = new object();

        [SerializeField] private List<ResourceAmount> storageLimits = new List<ResourceAmount>();

        [SerializeField] 
        protected HashSet<ResourceType> allowedTypes = new HashSet<ResourceType>();
        public HashSet<ResourceType> AllowedTypes => allowedTypes;
        [SerializeField, OnValueChanged("ResourcesSet")]
        protected HashSet<ResourceType> pickUpTypes = new HashSet<ResourceType>();
        [SerializeField, OnValueChanged("ResourcesSet")]
        protected HashSet<ResourceType> deliverTypes = new HashSet<ResourceType>();


        [SerializeField] private bool alwaysFillUp = false;
        [SerializeField] private bool deliverWithOutShuttle = false;
        public bool DeliverWithOutShuttle => deliverWithOutShuttle;
        private int noShuttleRange = 0;
        public int NoShuttleRange => noShuttleRange <= 0 ? (int)unit.GetStat(Stat.maxRange) : noShuttleRange;
        [ShowIf("deliverWithOutShuttle")]
        public List<ResourceType> noShuttleResouces = new();

        public event System.Action<UnitStorageBehavior, ResourceAmount> resourceUsed;
        public event System.Action<UnitStorageBehavior, ResourceAmount> resourceDelivered;
        public event System.Action<UnitStorageBehavior, ResourceAmount> resourcePickedUp;
        public static event System.Action<UnitStorageBehavior> unitStorageAdded;
        public static event System.Action<UnitStorageBehavior> unitStorageRemoved;

        private StatusIndicator statusIndicator;

        [Header("Delivery Locations")]
        [SerializeField]protected HashSet<UnitStorageBehavior> connections = new HashSet<UnitStorageBehavior>();
        private List<UnitStorageBehavior> connectionByPriority = new List<UnitStorageBehavior>();
        private bool connectionsLocked;

        protected static CargoManager cargoManager;
        public event Action<UnitStorageBehavior> connectionChanged;
        public static Action<UnitStorageBehavior, UnitStorageBehavior> connectionAdded;
        public static Action<UnitStorageBehavior, UnitStorageBehavior> connectionRemoved;
        public static event Action<UnitStorageBehavior> startListeningAddConnection;
        public static event Action<UnitStorageBehavior> stopListeningAddConnection;
        public static event Action<UnitStorageBehavior> startListeningRemoveConnection;
        public static event Action<UnitStorageBehavior> stopListeningRemoveConnection;
        private static UIControlActions uiControls;
        public bool createStartingConnection = true;
        public bool cargoWithoutConnection = false;
        public bool preventConnections = false;
        [SerializeField] protected bool preventUserMadeConnections = false;
        public List<CargoShuttleBehavior> shuttles = new List<CargoShuttleBehavior>();
        private static bool connectionUnlocked = false;
        public bool IsActive => isActive;
        protected bool isActive = true;
        public Vector3 Position => position;
        protected Vector3 position;

        public bool IsTransport => isTransport;
        protected bool isTransport = false;

        public bool IsSupplyShip => isSupplyShip;
        protected bool isSupplyShip = false;

        //resource monitoring
        private List<ResourceAmount> productionCost;


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

        protected void Awake()
        {
            if(cargoManager == null)
                cargoManager = FindFirstObjectByType<CargoManager>();

            if(uiControls == null)
                uiControls = new UIControlActions();

            landingPads = this.GetComponentsInChildren<LandingPad>();
        }

        protected void OnEnable()
        {
            isActive = true;
            this.position = this.transform.position;
            if (statusIndicator == null)
                statusIndicator = this.GetComponentInChildren<StatusIndicator>();

            InitializeResources(pickUpTypes, deliverTypes);
            if (requestPriority == CargoManager.RequestPriority.off) //allows tiles to be set to low by default
                requestPriority = CargoManager.RequestPriority.medium;

            uiControls.UI.RightClick.canceled += StopListeningForConnection;
            UnitStorageBehavior.unitStorageRemoved += CleanUpAllConnections;
            RepairBehavior.repairMovedStarted += CleanUpAllConnections;
            UnitInfoWindow.urgentPriorityTurnOn += UrgentPrioritySet;

            SaveLoadManager.LoadComplete += CheckResourceLevels;
        }

        protected void OnDisable()
        {
            isActive = false;
            if(GameStateManager.LeavingScene)
                return;

            StopBehavior();

            //recoup the cost of the resources if not a building spot
            if(!this.gameObject.GetComponent<BuildingSpotBehavior>() && 
               !this.gameObject.GetComponent<PlaceHolderTileBehavior>() &&
               !this.gameObject.GetComponent<ResourcePickupBehavior>())
                cargoManager?.PlaceCubes(resourceStored, this.transform.position, 1f);
            
            ClearResources();
            uiControls.UI.RightClick.canceled -= StopListeningForConnection;
            UnitStorageBehavior.unitStorageRemoved -= CleanUpAllConnections;
            UnitInfoWindow.urgentPriorityTurnOn -= UrgentPrioritySet;
            SaveLoadManager.LoadComplete -= CheckResourceLevels;
        }


        public override void StartBehavior()
        {
            unitStorageAdded?.Invoke(this);
            isFunctional = true;
            if (this.unit == null)
            {
                //do something. This is to get around some background thread issues :)
            }

            RequestWorkers();
        }

        public override void StopBehavior()
        {
            if (GameStateManager.LeavingScene)
                return;

            unitStorageRemoved?.Invoke(this);
            isFunctional = false;
            ReturnWorkers();
        }

        public void InitializeResources(HashSet<ResourceType> pickupTypes, HashSet<ResourceType> deliverTypes)
        {
            List<ResourceType> resources = new List<ResourceType>();
            resources.AddRange(pickupTypes);
            resources.AddRange(deliverTypes);
            resources = resources.Distinct().ToList();

            resourceStored.Clear();
            foreach (ResourceType resource in resources)
            {
                resourceStored.Add(new ResourceAmount(resource, 0));
            }
        }

        public virtual bool DeliverResources(List<ResourceAmount> resources)
        {
            foreach (var resource in resources)
            {
                if(deliverTypes.Contains(resource.type))
                    continue;

                AddResourceToList(resource, resourceStored);
                resourceDelivered?.Invoke(this, resource);
                if (resource.type == ResourceType.Workers)
                    WorkerManager.UpdateWorkerCounts();
            }

            return true;
        }

        public virtual bool DeliverResource(ResourceAmount resource)
        {
            if (!this.gameObject.activeSelf)
                return false;

            if (!deliverTypes.Contains(resource.type) && 
                resource.type != ResourceType.Workers &&
                !ResourceInTransit(resource))
                return false;

            RemoveResourceFromList(resource, resourceInTransit);
            AddResourceToList(resource, resourceStored);
            resourceDelivered?.Invoke(this, resource);
            if (resource.type == ResourceType.Workers)
            {
                WorkerManager.UpdateWorkerCounts();
                workersRequested -= resource.amount;
            }
            return true;
        }

        public void AddResourceForPickup(ResourceAmount resource)
        {
            AddResourceToList(resource, resourceStored);
            resourceDelivered?.Invoke(this, resource);
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

        public virtual void ReserveForDelivery(ResourceAmount deliver)
        {
            AddResourceToList(deliver, resourceInTransit);
        }

        public virtual ResourceAmount PickupResource(ResourceAmount resource)
        {
            if (resource.type == ResourceType.Workers)
            {
                //PickUpWorkers(resource);
                resourcePickedUp?.Invoke(this, resource);
                return resource;
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
            if(pickUp.type == ResourceType.Workers)
            {
                RemoveResourceFromList(pickUp, resourceInTransit);
                WorkerManager.UpdateWorkerCounts();
                return;
            }

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

        [Button]
        public void CheckResourceLevels()
        {
            if(SaveLoadManager.Loading)
                return;

            RequestWorkers();
        }

        private void RequestWorkers()
        {
            if (GetStat(Stat.workers) == 0 || SaveLoadManager.Loading)
                return;

            int requestAmount = (int)GetStat(Stat.workers) - GetResourceTotal(ResourceType.Workers) - workersRequested;
            if (requestAmount <= 0)
                return;
            
            workersRequested = requestAmount;
            WorkerManager.RequestWorkers(requestAmount, this);
        }

        public void ReturnWorkers()
        {
            if (GetStat(Stat.workers) == 0)
                return;

            int requestAmount = GetAmountStored(ResourceType.Workers);
            WorkerManager.ReturnWorkers(requestAmount);
        }

        public virtual int GetWorkerTotal()
        {
            return GetAmountStored(ResourceType.Workers);
        }

        public virtual int GetWorkersNeed()
        {
            return (int)GetStat(Stat.workers) - GetWorkerTotal();
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

        protected int GetAmountInTransit(ResourceType type)
        {
            //lock(listLock)
            //{
                for (int i = 0; i < resourceInTransit.Count; i++)
                {
                    ResourceAmount resource = resourceInTransit[i];
                    if (type == resource.type)
                        return resource.amount;
                }
            //}

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
            for (int i = 0; i < resourceStored.Count; i++)
            {
                ResourceAmount resource = resourceStored[i];
                total += resource.amount;
            }

            //foreach (var resource in resouceInTransit)
            //    total += resource.amount;

            return total;
        }
        
        public int GetResourceTotal(ResourceType type)
        {
            int total = 0;
            //lock(listLock)
            //{
                for (int i = 0; i < resourceStored.Count; i++)
                {
                    ResourceAmount resource = resourceStored[i];
                    if (resource.type == type)
                        total += resource.amount;
                }
                for (int i = 0; i < resourceInTransit.Count; i++)
                {
                    ResourceAmount resource = resourceInTransit[i];
                    if (resource.type == type)
                        total += resource.amount;
                }
            //}
            return total;
        }

        public bool TryUseAllResources(List<ResourceAmount> resourceList)
        {
            if (resourceList.Count == 0)
                return true;

            if (!HasAllResources(resourceList))
                return false;

            for (int i = 0; i < resourceList.Count; i++)
            {
                RemoveResourceFromList(resourceList[i], resourceStored);
                resourceUsed?.Invoke(this, resourceList[i]);
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
            for (int i = 0; i < resourceList.Count; i++)
            {
                if (!HasResource(resourceList[i]))
                    return false;
            }
            return true;
        }

        public virtual bool HasResource(ResourceAmount resource)
        {
            for (int i = 0; i < resourceStored.Count; i++)
            {
                if (resource.type == resourceStored[i].type && resource.amount <= resourceStored[i].amount)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Can the resources be storaged as a delivery?
        /// </summary>
        /// <param name="deliver"></param>
        /// <returns></returns>
        public virtual bool CanDeliverResource(ResourceAmount deliver)
        {
            if (!deliverTypes.Contains(deliver.type))
                return false;

            return GetResourceStorageLimit(deliver) - GetResourceTotal(deliver.type) >= deliver.amount;
        }

        private bool ResourceInTransit(ResourceAmount deliver)
        {
            for (int i = 0; i < resourceInTransit.Count; i++)
            {
                if (resourceInTransit[i].type == deliver.type)
                    return true;
            }
            return false;
        }
        
        public virtual bool CanStoreForPickup(ResourceAmount deliver)
        {
            if (!pickUpTypes.Contains(deliver.type))
                return false;

            return GetResourceStorageLimit(deliver) - GetResourceTotal(deliver.type) >= deliver.amount;
        }
        
        /// <summary>
        /// Can the the resource be stored for pickip?
        /// </summary>
        /// <param name="pickUp"></param>
        /// <returns></returns>
        public virtual bool CanPickupResource(ResourceAmount pickUp)
        {
            if (!pickUpTypes.Contains(pickUp.type))
                return false;

            return GetResourceTotal(pickUp.type) >= pickUp.amount;
        }

        public float GetResourceStorageLimit(ResourceAmount resourceType)
        {
            return GetResourceStorageLimit(resourceType.type);
        }
        
        public float GetResourceStorageLimit(ResourceType resourceType)
        {
            for (int i = 0; i < storageLimits.Count; i++)
            {
                if (storageLimits[i].type == resourceType)
                {
                    return storageLimits[i].amount;
                }
            }

            return GetStat(Stat.maxStorage);
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
            //lock(listLock)
            //{
                for (int i = 0; i < resourceStored.Count; i++)
                {
                    if (resourceStored[i].type == type)
                        resourceStored[i] = new ResourceAmount(resourceStored[i].type, amount);
                }
            //}
        }

        private void ClearResources()
        {
            resourceStored.Clear();
            resourceInTransit.Clear();
        }

        protected void AddResourceToList(ResourceAmount resource, List<ResourceAmount> list)
        {
            //lock(listLock)
            //{
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
            //}
        }

        protected void RemoveResourceFromList(ResourceAmount resource, List<ResourceAmount> list)
        {
            //lock(listLock)
            //{
                for (int i = 0; i < list.Count; i++)
                {
                    if (resource.type == list[i].type)
                    {
                        list[i] -= resource;
                        if (list[i].amount < 0)
                        {
                            Debug.Log($"{resource.type} is negative {list[i].amount}", this.gameObject);
                            list[i] = new ResourceAmount(resource.type, 0);
                        }
                        return;
                    }
                }
            //}
        }

        public RequestStorageInfo GetPopUpRequestPriority()
        {
            return new RequestStorageInfo()
            {
                priority = this.requestPriority,
                setPriority = SetRequestPriority,
                getPriority = GetPriority,
                revertPrioity = RevertRequestPriority,
                connections = connections,
                startAddConnection = StartAddConnection,
                startRemoveConnection = StartRemoveConnection,
            };
        }

        private void UrgentPrioritySet(PlayerUnit unit)
        {
            if (unit == null || unit.gameObject == null)
                return;

            //only have one unit that is at urgent priority
            if (unit.gameObject != this.gameObject && requestPriority == CargoManager.RequestPriority.urgent)
                RevertRequestPriority();
            else if(unit.gameObject == this.gameObject)
                SetRequestPriority(CargoManager.RequestPriority.urgent);
        }

        private void StartAddConnection()
        {
            if (UnitSelectionManager.changingConnections)
            {
                StopListeningChangingConnection();
                return;
            }

            UnitSelectionManager.changingConnections = true;
            UnitSelectionManager.unitClicked += AddNewConnection;
            uiControls.Enable();
            startListeningAddConnection?.Invoke(this);
        }
        private void StopListeningForConnection(InputAction.CallbackContext context)
        {
            StopListeningChangingConnection();
        }

        private void StartRemoveConnection()
        {
            if (UnitSelectionManager.changingConnections)
            {
                StopListeningChangingConnection();
                return;
            }

            UnitSelectionManager.changingConnections = true;
            UnitSelectionManager.unitClicked += RemoveConnection;
            uiControls.Enable();
            startListeningRemoveConnection?.Invoke(this);
        }

        protected void StopListeningChangingConnection()
        {
            UnitSelectionManager.changingConnections = false;
            UnitSelectionManager.unitClicked -= AddNewConnection;
            UnitSelectionManager.unitClicked -= RemoveConnection;
            uiControls?.Disable();
            stopListeningAddConnection?.Invoke(this);
            stopListeningRemoveConnection?.Invoke(this);
        }

        private void AddNewConnection(PlayerUnit playerUnit)
        {
            if (playerUnit.gameObject == this.gameObject)
                return;

            UnitStorageBehavior usb = playerUnit.GetComponent<UnitStorageBehavior>();

            AddDeliverConnection(usb);
        }

        public virtual void AddDeliverConnection(UnitStorageBehavior usb, bool suppressWarning = false)
        {
            if (connections.Contains(usb))
                return;

            if(usb == null)
                return;

            if (usb == this)
                return;

            if (this.preventUserMadeConnections)
                return;

            if (this.preventConnections || usb.preventConnections)
            {
                SFXManager.PlaySFX(SFXType.error);
                return;
            }

            if (!suppressWarning && HelperFunctions.HexRange(this.transform.position, usb.transform.position) > CargoManager.transportRange)
            {
                MessagePanel.ShowMessage("Connection out of range", this.gameObject);
                SFXManager.PlaySFX(SFXType.error);
                return;
            }

            connections.Add(usb);
            connectionAdded?.Invoke(this, usb);
            OrderConnections();

            if(SaveLoadManager.Loading)
                return;

            if (UnitSelectionManager.selectedUnit != null && UnitSelectionManager.selectedUnit.gameObject == this.gameObject)
                ConnectionChanged();

            if (!Keyboard.current.shiftKey.isPressed)
                StopListeningChangingConnection();
        }

        protected void ConnectionChanged()
        {
            connectionChanged?.Invoke(this);
        }

        private void CleanUpAllConnections(RepairBehavior behavior, UnitStorageBehavior usb)
        {
            CleanUpAllConnections(usb);
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
        public void RemoveConnection(UnitStorageBehavior usb)
        {
            connectionRemoved?.Invoke(this, usb);
            RemoveConnectionFromList(usb);
        }

        private void RemoveConnection(PlayerUnit playerUnit)
        {
            if (playerUnit.gameObject == this.gameObject)
                return;

            UnitStorageBehavior usb = playerUnit.GetComponent<UnitStorageBehavior>();

            RemoveConnectionFromList(usb);
        }

        //removes only the local connection
        public void RemoveConnectionFromList(UnitStorageBehavior behavior)
        {
            connections.Remove(behavior);
            connectionRemoved?.Invoke(this, behavior);
            if (UnitSelectionManager.selectedUnit?.gameObject == this.gameObject)
                connectionChanged?.Invoke(this);
            StopListeningChangingConnection();
            OrderConnections();
        }

        public void SetRequestPriority(CargoManager.RequestPriority requestType)
        {
            //always store the last non-urgent priority
            if (this.requestPriority != CargoManager.RequestPriority.urgent) 
                this.previousPriority = this.requestPriority;
            this.requestPriority = requestType;
        }

        public void RevertRequestPriority()
        {
            this.requestPriority = this.previousPriority;
        }

        public CargoManager.RequestPriority GetPriority()
        {
            return this.requestPriority;
        }

         
        public void SetDeliverTypes(HashSet<ResourceType> deliverTypes)
        {
            this.deliverTypes = deliverTypes;
            this.allowedTypes.UnionWith(deliverTypes);
        }

        public void AddDeliverType(ResourceType resource)
        {
            this.deliverTypes.Add(resource);
            this.allowedTypes.Add(resource);
        }

        public void SetPickUpTypes(HashSet<ResourceType> pickupTypes)
        {
            this.pickUpTypes = pickupTypes;
            this.allowedTypes.UnionWith(pickupTypes);
        }

        public void AddPickUpType(ResourceType resource)
        {
            this.pickUpTypes.Add(resource);
            this.allowedTypes.Add(resource);
        }

        public void RemovePickUpTypes(ResourceType resource)
        {
            this.pickUpTypes.Remove(resource);
            this.allowedTypes.Remove(resource);
        }

        public void RemoveDeliverTypes(ResourceType resource)
        {
            this.deliverTypes.Remove(resource);
            this.allowedTypes.Remove(resource);
        }

        public void AddToPickUpTypes(ResourceType resourceType)
        {
            if (!pickUpTypes.Contains(resourceType))
                pickUpTypes.Add(resourceType);
        }
        public void ClearPickupTypes()
        {
            pickUpTypes.Clear();
        }
        public void ClearDeliveryTypes()
        {
            deliverTypes.Clear();
        }

        public List<PopUpResourceAmount> GetPopUpResources()
        {
            List<PopUpResourceAmount> resourceInfos = new List<PopUpResourceAmount>();
            foreach (var resource in resourceStored)
            {
                if(!deliverTypes.Contains(resource.type) && !pickUpTypes.Contains(resource.type))
                    continue;

                resourceInfos.Add(new PopUpResourceAmount(resource, GetResourceStorageLimit(resource), 0, Color.white));
            }

            //trying to get the case where the building needs workers but doesn't have any yet
            if(GetStat(Stat.workers) > 0)
            {
                int workerCount = GetAmountStored(ResourceType.Workers);
                ResourceAmount workers = new ResourceAmount(ResourceType.Workers, workerCount);
                resourceInfos.Add(new PopUpResourceAmount(workers, GetIntStat(Stat.workers), 0, Color.white));
            }

            foreach (var resource in pickUpTypes.Union(deliverTypes))
            {
                if(!ListContainsResource(resourceStored, resource))
                    resourceInfos.Add(new PopUpResourceAmount(new ResourceAmount(resource, 0), GetResourceStorageLimit(resource), 0, Color.white));
            }

            return resourceInfos;
        }

        protected bool ListContainsResource(List<ResourceAmount> list, ResourceType type)
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
            landingPads = new LandingPad[] { landingPad.GetComponent<LandingPad>() };
        }

        internal void AdjustStorageForRecipe(ResourceProduction oldRecipe, ResourceProduction newRecipe)
        {
            foreach (var resource in oldRecipe.GetCost())
            {
                RemoveDeliverTypes(resource.type);
                if(!RecipeContains(newRecipe, resource.type))
                    RequestPickUpOfAllResource(resource.type);
            }

            foreach (var resource in oldRecipe.GetProduction())
            {
                RemovePickUpTypes(resource.type);
                if (!RecipeContains(newRecipe, resource.type))
                    RequestPickUpOfAllResource(resource.type);
            }

            foreach (var resource in newRecipe.GetCost())
            {
                AddDeliverType(resource.type);
                //make sure we don't accidentally pickup things we need
                RemovePickUpTypes(resource.type);
            }

            foreach (var resource in newRecipe.GetProduction())
            {
                AddPickUpType(resource.type);
            }

            this.productionCost = newRecipe.GetCost();
        }

        //public List<ResourceType> GetAllowedResources()
        //{
        //    return allowedTypes;
        //}

        public HashSet<ResourceType> GetPickUpTypes() => pickUpTypes;
        public HashSet<ResourceType> GetDeliverTypes() => deliverTypes;
        public HashSet<ResourceType> GetAllowedResources()
        {
            var allowedTypes = new HashSet<ResourceType>(pickUpTypes);
            allowedTypes.UnionWith(deliverTypes);
            return allowedTypes;
        }

        public virtual int GetStorageCapacity()
        {
            return this.GetIntStat(Stat.maxStorage) * GetAllowedResources().Count;
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

        public HashSet<UnitStorageBehavior> GetConnections()
        {
            return connections;
        }

        public List<UnitStorageBehavior> GetConnectionsByPriority()
        {
            return connectionByPriority;
        }

        protected async Awaitable OrderConnections()
        {
            while (connectionsLocked)
            {
                await Awaitable.NextFrameAsync();
            }
            
            connectionByPriority = connections.OrderByDescending(c => c.GetPriority()).ToList();
        }

        public void LockConnections(bool locked)
        {
            this.connectionsLocked = locked;
        }

        public List<ConnectionStatusInfo> GetConnectionInfo()
        {
            List<ConnectionStatusInfo> connectionStatusInfos = new List<ConnectionStatusInfo>();
            foreach (var connection in connections)
            {
                ConnectionStatusInfo connectionStatusInfo = new ConnectionStatusInfo();
                connectionStatusInfo.storage = connection;
                connectionStatusInfo.status = GetConnectionStatus(connection);
                connectionStatusInfo.resources = null;
                connectionStatusInfos.Add(connectionStatusInfo);
            }

            return connectionStatusInfos;
        }
        
        public List<ConnectionStatusInfo> GetConnectionInfoWithResource()
        {
            List<ConnectionStatusInfo> connectionStatusInfos = new List<ConnectionStatusInfo>();
            foreach (var connection in connections)
            {
                ConnectionStatusInfo connectionStatusInfo = new ConnectionStatusInfo();
                connectionStatusInfo.storage = connection;
                connectionStatusInfo.status = GetConnectionStatus(connection);
                connectionStatusInfo.resources = GetShippedResourceTypes(connection);
                connectionStatusInfos.Add(connectionStatusInfo);
            }

            return connectionStatusInfos;
        }
        
        public List<ConnectionStatusInfo> GetConnectionInfoByResource(ResourceType resource)
        {
            List<ConnectionStatusInfo> connectionStatusInfos = new List<ConnectionStatusInfo>();
            foreach (var connection in connections)
            {
                if (!GetShippedResourceTypes(connection).Contains(resource))
                    continue;

                ConnectionStatusInfo connectionStatusInfo = new ConnectionStatusInfo();
                connectionStatusInfo.storage = connection;
                connectionStatusInfo.status = GetConnectionStatus(connection);
                connectionStatusInfo.resources = GetShippedResourceTypes(connection);
                connectionStatusInfos.Add(connectionStatusInfo);
            }

            return connectionStatusInfos;
        }

        public virtual ConnectionStatus GetConnectionStatus(UnitStorageBehavior destination)
        {
            //if(this.isSupplyShip && destination.allowAllTypes)
            //    return ConnectionStatus.deliverable;

            if(this.pickUpTypes.Count == 0)
                return ConnectionStatus.unDeliverable;

            if(destination.deliverTypes.Count() == 0)
                return ConnectionStatus.unDeliverable;

            if (destination.deliverTypes.Overlaps(this.pickUpTypes))
                return ConnectionStatus.deliverable;

            return ConnectionStatus.unDeliverable;
        }

        /// <summary>
        /// returns a collection of the resouces that CAN move between this and the destination
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public HashSet<ResourceType> GetShippedResourceTypes(UnitStorageBehavior destination)
        {
            return this.pickUpTypes.Intersect(destination.deliverTypes).ToHashSet();
        }

        public ReadOnlyCollection<ResourceAmount> GetStoredResources()
        {
            return resourceStored.AsReadOnly();
        }

        public List<PopUpButtonInfo> GetButtons()
        {
            if (this.preventConnections || this.preventUserMadeConnections)
                return new List<PopUpButtonInfo>();

            List<PopUpButtonInfo> buttons = new List<PopUpButtonInfo>()
            {
                new PopUpButtonInfo(ButtonType.connections, null),
            };

            return buttons;
        }
        
        public List<ResourceAmount> GetResourcesToSave()
        {
            List<ResourceAmount> resources = new List<ResourceAmount>(resourceStored);
            //look for resources in transit that originated from this storage
            //add them to the stored list
            foreach (var resource in resourceInTransit)
            {
                if (!pickUpTypes.Contains(resource.type) && resource.type != ResourceType.Workers)
                    continue;

                for (int i = 0; i < resources.Count; i++)
                {
                    if (resources[i].type == resource.type)
                    {
                        resources[i] += resource;
                    }
                }
            }

            return resources;
        }

        public List<Hex3> GetConnectionLocations()
        {
            List<Hex3> locations = new List<Hex3>();
            foreach (var connection in connections)
            {
                locations.Add(connection.transform.position);
            }

            return locations;
        }
        public void LoadStoredResources(List<ResourceAmount> resources)
        {
            foreach (var resource in resources)
            {
                if(pickUpTypes.Contains(resource.type) || resource.type == ResourceType.Workers)
                    StoreResource(resource);
                else
                    DeliverResource(resource);
            }
        }

        public void LoadConnections(List<Hex3> connectionLocations, Func<bool> buildingsCreated)
        {
            if (connectionLocations == null || connectionLocations.Count == 0)
                return;

            StartCoroutine(AddConnectionsDelayed(connectionLocations, buildingsCreated));
        }

        private IEnumerator AddConnectionsDelayed(List<Hex3> connectionLocations, Func<bool> buildingsCreated)
        {
            yield return new WaitUntil(buildingsCreated);
            yield return null;

            foreach (var location in connectionLocations)
            {
                if (UnitManager.TryGetPlayerUnitAtLocation(location, out PlayerUnit playerUnit))
                    AddDeliverConnection(playerUnit.GetComponent<UnitStorageBehavior>(), true);
            }
        }

        public void AdjustStorageForProject(SpecialProjectProduction project)
        {
            storageLimits.Clear();
            pickUpTypes.Clear();
            allowedTypes.Clear();
            foreach (var resource in project.GetCost())
            {
                if (!allowedTypes.Contains(resource.type))
                    allowedTypes.Add(resource.type);
                //make sure we don't accidentally pickup things we need
                storageLimits.Add(resource);
            }
        }
        
        /// <summary>
        /// Sets the storage limits in the UnitStorageBehavior to match the build costs
        /// This ensures the building spot can only store the exact amount needed for construction
        /// </summary>
        /// <param name="buildCosts">The resource costs required to build the unit</param>
        public void AdjustStorageForBuildCosts(List<ResourceAmount> buildCosts)
        {
            // Clear existing storage limits and set new ones based on build costs
            storageLimits.Clear();
            deliverTypes.Clear();
            allowedTypes.Clear();
            
            foreach (var resource in buildCosts)
            {
                storageLimits.Add(resource);
                deliverTypes.Add(resource.type);
                allowedTypes.Add(resource.type);
            }
        }

        private Vector3 GetLandingPosition()
        {
            if(landingPads == null || landingPads.Length == 0)
            {
                return this.transform.position;
            }

            return landingPads[UnityEngine.Random.Range(0, landingPads.Length - 1)].transform.position;
        }

        /// <summary>
        /// This position is used by the cargo system.
        /// </summary>
        public void UpdateStoredPosition()
        {
            this.position = this.transform.position;
        }

        private void ResourcesSet()
        {
            allowedTypes.Clear();
            allowedTypes.UnionWith(deliverTypes);
            allowedTypes.UnionWith(pickUpTypes);
        }
    }

    public class ConnectionStatusInfo
    {
        public UnitStorageBehavior storage;
        public ConnectionStatus status;
        public HashSet<ResourceType> resources;
    }

    public enum ConnectionStatus
    {
        deliverable,
        storageFull,
        unDeliverable,
    }

}