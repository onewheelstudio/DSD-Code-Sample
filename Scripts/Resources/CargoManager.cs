using DG.Tweening;
using HexGame.Units;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HexGame.Resources
{
    public class CargoManager : MonoBehaviour
    {
        private static List<GlobalStorageBehavior> storageBuildings = new List<GlobalStorageBehavior>();

        private static List<CargoShuttleBehavior> globalShuttles = new List<CargoShuttleBehavior>();
        private static List<CargoShuttleBehavior> localShuttles = new List<CargoShuttleBehavior>();
        public static int transportAmount = 5;
        public static int transportRange = 5;
        [ShowInInspector]
        private static RequestQueue pickupRequests = new RequestQueue();
        [ShowInInspector]
        private static RequestQueue deliveryRequests = new RequestQueue();
        [SerializeField]
        private CargoCubeList cargoCubeList;
        [SerializeField] private GameObject emptyDropPrefab;
        private static ObjectPool<ResourcePickup> emptyDropPool;

        //request completion
        private RequestPrioritySorting requestPrioritySorting = new RequestPrioritySorting();
        private bool sortRequests = false;


        private void OnEnable()
        {
            GlobalStorageBehavior.gloablStorageAdded += AddStorageBuilding;
            GlobalStorageBehavior.gloablStorageRemoved += RemoveStorageBuilding;
            UnitInfoWindow.prorityChanged += SetSortRequests;

            emptyDropPool = new ObjectPool<ResourcePickup>(emptyDropPrefab);
        }

        private void OnDisable()
        {
            GlobalStorageBehavior.gloablStorageAdded -= AddStorageBuilding;
            GlobalStorageBehavior.gloablStorageRemoved -= RemoveStorageBuilding;
            UnitInfoWindow.prorityChanged -= SetSortRequests;

            DOTween.Kill(this,true);
        }



        public static void MakeRequest(UnitStorageBehavior unit, ResourceAmount resource, RequestType requestType)
        {
            if(resource.amount <= 0)
                return;

            int requestsNeeded = Mathf.FloorToInt(resource.amount / CargoManager.transportAmount);
            for (int i = 0; i < requestsNeeded; i++)
                PlaceRequest(new Request(unit, new ResourceAmount(resource.type, CargoManager.transportAmount), requestType));

            //get remainder amount
            if (resource.amount % CargoManager.transportAmount > 0)
                PlaceRequest(new Request(unit, new ResourceAmount(resource.type, resource.amount % CargoManager.transportAmount), requestType));
        }

        private static void PlaceRequest(Request request)
        {
            if(request.requestType == RequestType.pickup)
                pickupRequests.AddRequest(request);
            else
                deliveryRequests.AddRequest(request);
        }

        
        //pretty sure this function doesn't work like I think it does...
        public static void RequestPickupAll(UnitStorageBehavior unit, ResourceAmount resource)
        {
            int requestsNeeded = Mathf.FloorToInt(resource.amount / CargoManager.transportAmount);
            for (int i = 0; i < requestsNeeded; i++)
                pickupRequests.AddRequest(new Request(unit, resource, RequestType.pickup));

            //get remainder amount
            pickupRequests.AddRequest(new Request(unit, resource, RequestType.pickup));
        }

        public static void AddShuttle(CargoShuttleBehavior shuttle)
        {
            if(!localShuttles.Contains(shuttle))
                localShuttles.Add(shuttle);
        }

        public static void RemoveShuttle(CargoShuttleBehavior shuttle)
        {
            globalShuttles.Remove(shuttle);
        }

        public static void AddStorageBuilding(GlobalStorageBehavior storage)
        {
            if (!storageBuildings.Contains(storage))
                storageBuildings.Add(storage);
        }

        public static void RemoveStorageBuilding(GlobalStorageBehavior storage)
        {
            storageBuildings.Remove(storage);
        }

        private void Update()
        {
            if (DayNightManager.isNight)
                return;

            if(!GameOverMenu.isGameOver)
                ProcessRequests();
        }

        private void ProcessRequests()
        {
            if (sortRequests && Time.frameCount % 3 == 0)
                SortRequests();
            if(Time.frameCount % 2 == 0)
                CompleteRequests(deliveryRequests, pickupRequests);
            else
                CompleteRequests(pickupRequests, deliveryRequests);
        }

        private void SetSortRequests()
        {
            sortRequests = true;
        }

        private void SortRequests()
        {
            deliveryRequests.requests.Sort(requestPrioritySorting);
            pickupRequests.requests.Sort(requestPrioritySorting);
            sortRequests = false;
        }

        private void CompleteRequests(RequestQueue requestQueue, RequestQueue matchQueue)
        {
            if(requestQueue.requests.Count == 0)
                return;

            foreach (var request in requestQueue.requests)
            {
                //filter for buildings that are turned off
                if (request.priority == RequestPriority.off)
                    continue;

                if(!KeepRequestInQueue(request))
                {
                    requestQueue.RemoveRequest(request);
                    return; //return because inside foreach loop
                }

                //check if building is functional
                if (!IsRequestValid(request))
                {
                    requestQueue.requests.MoveToLast(request);
                    return; //return because inside foreach loop
                }

                CargoShuttleBehavior shuttleBehavior = null;

                //check if a match can be found
                foreach (var match in matchQueue.requests)
                {
                    if(match.storage.GetPriority() == RequestPriority.off)
                        continue;

                    //is the attempted match connected to the requesting building?
                    if (!request.storage.GetConnections().Contains(match.storage) && 
                        !match.storage.GetConnections().Contains(request.storage))
                        continue;

                    if (request.resourceCapacity.type != match.resourceCapacity.type)
                        continue;

                    //this should never happen
                    if (request.storage == match.storage)
                        continue;

                    //trying to only have the "deliver" shuttle do the work
                    if (request.requestType == RequestType.deliver && !IsLocalShuttleAvailable(match.storage, out shuttleBehavior))
                        continue;
                    else if (match.requestType == RequestType.deliver && !IsLocalShuttleAvailable(request.storage, out shuttleBehavior))
                        continue;

                    if (HelperFunctions.HexRange(shuttleBehavior.GetStartLocation(), request.storage.transform.position) <= CargoManager.transportRange
                        && HelperFunctions.HexRange(shuttleBehavior.GetStartLocation(), match.storage.transform.position) <= CargoManager.transportRange)
                    {
                        shuttleBehavior.AddRequest(request, match);

                        requestQueue.RemoveRequest(request);
                        matchQueue.RemoveRequest(match);

                        return; //found a match - we'll find more next frame - avoid messy list issues and accounting
                    }
                }

                //are we connected to storage that can supply us?
                Request deliveryRequest = null;

                //used mostly by building spots
                //this allows storage buildings to deliver to buildings without a return connection
                if(deliveryRequest == null && !request.storage.cargoWithoutConnection)
                {
                    foreach (var globalStorage in storageBuildings)
                    {
                        if (!globalStorage.isFunctional)
                            continue;

                        if (!TryGetShuttle(request.storage, globalStorage, out shuttleBehavior))
                            continue;

                        if (request.requestType == RequestType.pickup && 
                            request.storage.GetConnections().Contains(globalStorage) && 
                            globalStorage.CanStoreResource(request.resourceCapacity))
                        {
                            deliveryRequest = new Request(globalStorage, request.resourceCapacity, RequestType.deliver);
                            break;
                        }
                        else if (request.requestType == RequestType.deliver && 
                                 globalStorage.GetConnections().Contains(request.storage) && 
                                 globalStorage.CanPickUpResource(request.resourceCapacity))
                        {
                            deliveryRequest = new Request(globalStorage, request.resourceCapacity, RequestType.pickup);
                            break;
                        }                        
                        else if (request.requestType == RequestType.deliver && 
                                 request.storage is GlobalStorageBehavior &&
                                 HelperFunctions.HexRange(request.storage.transform.position, globalStorage.transform.position) <= transportRange && 
                                 globalStorage.CanPickUpResource(request.resourceCapacity))
                        {
                            deliveryRequest = new Request(globalStorage, request.resourceCapacity, RequestType.pickup);
                            break;
                        }
                    }
                }

                //special kiddy gloves for placeholder tiles
                if(deliveryRequest == null && request.storage.cargoWithoutConnection)
                {
                    foreach (var globalStorage in storageBuildings)
                    {
                        if (!globalStorage.isFunctional)
                            continue;

                        if (HelperFunctions.HexRange(globalStorage.transform.position, request.storage.transform.position) >= CargoManager.transportRange + 1)
                            continue;

                        //if (!TryGetShuttle(request.storage, globalStorage, out shuttleBehavior))
                        if (!IsLocalShuttleAvailable(globalStorage, out shuttleBehavior))
                                continue;

                        if (request.requestType == RequestType.pickup && globalStorage.CanStoreResource(request.resourceCapacity))
                        {
                            deliveryRequest = new Request(globalStorage, request.resourceCapacity, RequestType.deliver);
                            break;
                        }
                        else if (request.requestType == RequestType.deliver && globalStorage.CanPickUpResource(request.resourceCapacity))
                        {
                            deliveryRequest = new Request(globalStorage, request.resourceCapacity, RequestType.pickup);
                            break;
                        }
                    }
                }

                //attempt partial delivery from global storage
                if(deliveryRequest == null && request.requestType == RequestType.deliver)
                {
                    foreach (var globalStorage in storageBuildings)
                    {
                        if (!globalStorage.isFunctional)
                            continue;

                        if (!globalStorage.GetConnections().Contains(request.storage))
                            continue;

                        if (!TryGetShuttle(request.storage, globalStorage, out shuttleBehavior))
                            continue;

                        //are there any colonists available?
                        int resourceAvailable = globalStorage.GetAmountStored(request.resourceCapacity.type);
                        if (resourceAvailable == 0)
                            break;

                        if(resourceAvailable > request.resourceCapacity.amount)
                        {
                            Debug.Log("This shouldn't happen - resource available more than requested during partial load.");
                            break;
                        }
          
                        deliveryRequest = new Request(globalStorage, new ResourceAmount(request.resourceCapacity.type, resourceAvailable), RequestType.pickup);

                        //we've completed partial deliver now create new request for remaining workers
                        int remainingNeeded = request.resourceCapacity.amount - resourceAvailable;
                        request.resourceCapacity.amount = resourceAvailable;
                        MakeRequest(request.storage, new ResourceAmount(request.resourceCapacity.type, remainingNeeded), RequestType.deliver);
                        break;
                    }
                }

                if (deliveryRequest != null && shuttleBehavior != null)
                {
                    shuttleBehavior.AddRequest(request, deliveryRequest);
                    requestQueue.RemoveRequest(request);
                    return;
                }
            }
        }



        private bool IsRequestValid(Request request)
        {
            if (request.storage != null && request.storage is GlobalStorageBehavior)
                return true;

            if (request.storage != null && !request.storage.isFunctional)
                return false;

            return true;
        }
        
        private bool KeepRequestInQueue(Request request)
        {
            if (request.storage == null)
                return false;

            if (request.storage != null && request.storage.gameObject.activeSelf == false)
                return false;

            return true;
        }

        /// <summary>
        /// Tries to find a local shuttle first. Then looks for global shuttle. Pickup location is prioritized over delivery location.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cargoShuttle"></param>
        /// <returns></returns>
        public bool TryGetShuttle(Request request, Request match, out CargoShuttleBehavior cargoShuttle)
        {
            return TryGetShuttle(request.storage, match.storage, out cargoShuttle);
        }

        /// <summary>
        /// Tries to find a local shuttle first. Then looks for global shuttle. Pickup location is prioritized over delivery location.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="match"></param>
        /// <param name="cargoShuttle"></param>
        /// <returns></returns>
        private bool TryGetShuttle(UnitStorageBehavior request, UnitStorageBehavior match, out CargoShuttleBehavior cargoShuttle)
        {
            if (IsLocalShuttleAvailable(request, out cargoShuttle))
                return true;
            else if (IsLocalShuttleAvailable(match, out cargoShuttle))
                return true;
            else
            {
                cargoShuttle = null;
                return false;
            }
        }

        private bool IsLocalShuttleAvailable(UnitStorageBehavior storage, out CargoShuttleBehavior cargoShuttle)
        {
            cargoShuttle = null;

            foreach (var shuttle in storage.shuttles)
            {
                if (shuttle.IsAvailable())
                {
                    cargoShuttle = shuttle;
                    return true;
                }
            }

            return false;
        }

        public GameObject GetCargoCube(ResourceType resourceType)
        {
            return cargoCubeList.GetCargoCube(resourceType);
        }

        /// <summary>
        /// Places cubes when building or storage is destroyed
        /// </summary>
        /// <param name="unitCost"></param>
        /// <param name="position"></param>
        /// <param name="percentRecovered"></param>
        public void PlaceCubes(List<ResourceAmount> unitCost, Vector3 position, float percentRecovered)
        {
            if (GameConstants.recoverResourcePercent != 1f)
                unitCost.ForEach(x => x.amount = Mathf.RoundToInt(x.amount * GameConstants.recoverResourcePercent * percentRecovered));

            foreach (ResourceAmount resource in unitCost)
            {
                int numberToDrop = Mathf.RoundToInt(resource.amount / CargoManager.transportAmount);
                for (int i = 0; i < numberToDrop; i++)
                {
                    GameObject cube = GetCargoCube(resource.type);
                    ResourcePickup cubeParent = emptyDropPool.Pull();
                    cubeParent.SetResource(new ResourceAmount(resource.type, CargoManager.transportAmount));
                    cube.transform.SetParent(cubeParent.transform);
                    
                    //random offset used to avoid z fighting
                    cube.transform.localScale = Vector3.one * 25f;
                    cube.transform.localPosition = Vector3.zero + new Vector3(0f, 0.08f + UnityEngine.Random.Range(-0.005f, 0.005f), 0f);
                    cube.transform.eulerAngles = new Vector3(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                    Vector3 offset = Random.insideUnitCircle * 0.5f;
                    cubeParent.transform.position = position + new Vector3(offset.x, 0f, offset.y);
                    cubeParent.transform.position += new Vector3(0f, 0.08f + UnityEngine.Random.Range(0.05f, 0.075f), 0f);

                    Vector3 finalPosition = position;
                    float time = Mathf.Sqrt((finalPosition - cube.transform.position).y / (0.5f * Physics.gravity.y));
                    cubeParent.transform.DOMoveY(finalPosition.y, time).SetEase(Ease.InQuad);
                }
            }
        }

        public static void RemoveAllRequests(UnitStorageBehavior usb)
        {
            pickupRequests.RemoveRequestBy(usb);
            deliveryRequests.RemoveRequestBy(usb);
        }

        private class RequestQueue
        {
            public RequestQueue()
            {

            }

            public List<Request> requests = new List<Request>();

            public bool AddRequest(Request request)
            {
                //int index = requests.FindIndex(r => r.priority > request.priority);
                int index = -1;

                for (int i = 0; i < requests.Count - 1; i++)
                {
                    if (requests.Count < 2)
                        break;

                    if (requests[i].priority == request.priority && requests[i+1].priority < request.priority)
                    {
                        index = i + 1;
                        break;
                    }
                }
                
                if(index < 0)
                    requests.Add(request);
                else
                    requests.Insert(index, request);
                return true;
            }

            public bool RemoveRequest(Request request)
            {
                if(requests.Contains(request))
                {
                    requests.Remove(request);
                    return true;
                }
                else
                    return false;
            }

            public void RemoveRequestBy(UnitStorageBehavior usb)
            {
                for(int i = requests.Count - 1; i >= 0; i--)
                {
                    if (requests[i].storage == usb)
                    {
                        requests.RemoveAt(i);
                    }
                }
            }
        }

        [System.Serializable]
        public class Request
        {
            public Request(UnitStorageBehavior storage, ResourceAmount resourceAmount, RequestType requestType)
            {
                this.storage = storage;
                this.resourceCapacity = resourceAmount;
                this.requestType = requestType;
                this.timePlaced = Time.timeSinceLevelLoad;
            }

            public float timePlaced;
            public UnitStorageBehavior storage;
            public RequestType requestType = RequestType.pickup;
            public ResourceAmount resourceCapacity;

            /// <summary>
            /// Returns the delivery priority first.
            /// Returns pickup priority if delivery is null
            /// </summary>
            public RequestPriority priority
            {
                get => storage.GetPriority();
            }

            public override string ToString()
            {
                return $"Request by {storage.name} : {requestType.ToString()} : {resourceCapacity.ToString()}";
            }

        }

        /// <summary>
        /// Sorts requests by priority from high to low
        /// </summary>
        public class RequestPrioritySorting : IComparer<Request>
        {
            public int Compare(Request x, Request y)
            {
                int primary = y.priority.CompareTo(x.priority); //reverse order
                if(primary != 0)
                    return primary;

                return x.timePlaced.CompareTo(y.timePlaced);
            }
        }

        public class DistanceSorting : IComparer<CargoShuttleBehavior>
        {
            public int Compare(CargoShuttleBehavior x, CargoShuttleBehavior y)
            {
                return (x.transform.position - y.transform.position).sqrMagnitude.CompareTo((y.transform.position - x.transform.position).sqrMagnitude);
            }
        }

        public enum RequestPriority
        {
            off,
            low,
            medium,
            high
        }

        public enum RequestType
        {
            pickup,
            deliver
        }

    }

}
