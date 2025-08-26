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
        private static List<CargoShuttleBehavior> globalShuttles = new List<CargoShuttleBehavior>();
        private static List<CargoShuttleBehavior> localShuttles = new List<CargoShuttleBehavior>();
        public static int transportAmount = 5;
        public static int transportRange = 5;
        [ShowInInspector]
        private static RequestQueue pickupRequests = new RequestQueue();
        [SerializeField]
        private CargoCubeList cargoCubeList;
        [SerializeField] private GameObject emptyDropPrefab;
        private static ObjectPool<ResourcePickupBehavior> emptyDropPool;
        private UnitManager unitManager;

        //request completion
        private RequestPrioritySorting requestPrioritySorting = new RequestPrioritySorting();
        private bool sortRequests = false;

        private void Awake()
        {
            cargoCubeList.ClearCargoPools();
            unitManager = FindFirstObjectByType<UnitManager>();
        }


        private void OnEnable()
        {
            UnitInfoWindow.priorityChanged += SetSortRequests;

            emptyDropPool = new ObjectPool<ResourcePickupBehavior>(emptyDropPrefab);
        }

        private void OnDisable()
        {
            UnitInfoWindow.priorityChanged -= SetSortRequests;

            DOTween.Kill(this,true);

            globalShuttles.Clear();
            localShuttles.Clear();
        }

        private void Start()
        {
            ProcessRequests();
        }

        public static void MakeRequest(UnitStorageBehavior unit, ResourceAmount resource, RequestType requestType, bool onMainThread = true)
        {
            if(resource.amount <= 0)
                return;

            int requestsNeeded = Mathf.FloorToInt(resource.amount / CargoManager.transportAmount);
            for (int i = 0; i < requestsNeeded; i++)
                PlaceRequest(new Request(unit, new ResourceAmount(resource.type, CargoManager.transportAmount), requestType, onMainThread));

            //get remainder amount
            if (resource.amount % CargoManager.transportAmount > 0)
                PlaceRequest(new Request(unit, new ResourceAmount(resource.type, resource.amount % CargoManager.transportAmount), requestType, onMainThread));
        }

        private static void PlaceRequest(Request request)
        {
            if(request.requestType == RequestType.pickup)
                pickupRequests.AddRequest(request);
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

        private async Awaitable ProcessRequests()
        {
            while(!destroyCancellationToken.IsCancellationRequested)
            {
                if (sortRequests)
                    SortRequests();

                if(DayNightManager.isNight || GameOverMenu.isGameOver)
                {
                    await Awaitable.MainThreadAsync();
                    await Awaitable.NextFrameAsync();
                    continue;
                }

                try
                {
                    //await CompleteRequests(deliveryRequests, pickupRequests);
                    await CompleteRequests(pickupRequests);
                    await Awaitable.NextFrameAsync();
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error in CargoManager - ProcessRequests");
                    Debug.LogError(e);
                    throw;
                }

            }

            await Awaitable.MainThreadAsync();
            Debug.Log("Cancellation token requested - stopping request processing.");
        }

        private void SetSortRequests()
        {
            sortRequests = true;
        }

        private void SortRequests()
        {
            pickupRequests.requests.Sort(requestPrioritySorting);
            sortRequests = false;
        }

        private async Awaitable CompleteRequests(RequestQueue requestQueue)
        {
            if(requestQueue.requests.Count == 0)
                return;

            await Awaitable.BackgroundThreadAsync();

            for (int i = 0; i < requestQueue.requests.Count; i++)
            {
                Request request = requestQueue.requests[i];

                if(request == null)
                    continue;

                //filter for buildings that are turned off
                if (request.priority == RequestPriority.off)
                    continue;

                if(!KeepRequestInQueue(request))
                {
                    requestQueue.RemoveRequest(request);
                    continue;
                }

                //check if building is functional
                if (!IsRequestValid(request))
                {
                    requestQueue.requests.MoveToLast(request); //should this be a remove instead?
                    continue;
                }

                CargoShuttleBehavior shuttleBehavior = null;
                request.storage.LockConnections(true);
                List<UnitStorageBehavior> storageConnections = request.storage.GetConnectionsByPriority();
                for (int j = 0; j < storageConnections.Count; j++)
                {
                    UnitStorageBehavior connection = storageConnections[j];
                    if (connection == null)
                        continue;

                    if (!connection.CanDeliverResource(request.resourceCapacity))
                        continue;

                    if (!request.storage.CanPickupResource(request.resourceCapacity))
                        continue;

                    if (!TryGetShuttle(request.storage, connection, out shuttleBehavior))
                        continue;
                    RequestType requestType = request.requestType == RequestType.pickup ? RequestType.deliver : RequestType.pickup;
                    Request matchRequest = new Request(connection, request.resourceCapacity, requestType, false);

                    requestQueue.RemoveRequest(request);
                    await Awaitable.MainThreadAsync();
                    shuttleBehavior.AddRequest(request, matchRequest);
                    break;
                }
                request.storage.LockConnections(false);
            }
        }

        private bool IsRequestValid(Request request)
        {
            if (request.storage == null)
                return false;

            if (!request.storage.isFunctional)
                return false;

            return true;
        }
        
        private bool KeepRequestInQueue(Request request)
        {
            if (request.storage == null || !request.storage.IsActive)
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

            for (int i = storage.shuttles.Count - 1; i >= 0; i--)
            {
                if (storage.shuttles[i].IsAvailable())
                {
                    cargoShuttle = storage.shuttles[i];
                    return true;
                }
            }

            return false;
        }

        public CargoCube GetCargoCube(ResourceType resourceType)
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
            if (unitCost == null || unitCost.Count == 0)
                return;

            if (GameConstants.recoverResourcePercent != 1f)
                unitCost.ForEach(x => x.amount = Mathf.RoundToInt(x.amount * GameConstants.recoverResourcePercent * percentRecovered));

            if (unitCost.Sum(r => r.amount) == 0)
                return;

            ResourcePickupBehavior rpb = null;
            if (UnitManager.TryGetPlayerUnitAtLocation(position, out PlayerUnit playerUnit))
                playerUnit.TryGetComponent(out rpb);
            
            if(rpb == null)
                rpb = emptyDropPool.Pull();
            rpb.transform.position = position;
            foreach (ResourceAmount resource in unitCost)
            {
                int numberToDrop = Mathf.RoundToInt(resource.amount / CargoManager.transportAmount);
                for (int i = 0; i < numberToDrop; i++)
                {
                    CargoCube cube = GetCargoCube(resource.type);
                    cube.gameObject.SetActive(true);
                    rpb.AddResourcePickup(cube);
                    cube.transform.SetParent(rpb.transform);
                    
                    //random offset used to avoid z fighting
                    cube.transform.localScale = Vector3.one * 25f;
                    cube.transform.localPosition = Vector3.zero + new Vector3(0f, 0.08f + UnityEngine.Random.Range(-0.005f, 0.005f), 0f);
                    cube.transform.eulerAngles = new Vector3(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                    Vector3 offset = Random.insideUnitCircle * 0.5f;
                    cube.transform.position = position + new Vector3(offset.x, 0f, offset.y);
                    cube.transform.position += new Vector3(0f, 0.08f + UnityEngine.Random.Range(0.05f, 0.075f), 0f);

                    Vector3 finalPosition = position + Vector3.up * 0.0625f;
                    float time = Mathf.Sqrt((finalPosition - cube.transform.position).y / (0.5f * Physics.gravity.y));
                    cube.transform.DOMoveY(finalPosition.y, time).SetEase(Ease.InQuad);
                }
            }

            playerUnit = rpb.GetComponent<PlayerUnit>();
            playerUnit.Place();
            unitManager.AddResourceDrop(playerUnit);
        }

        public static void RemoveAllRequests(UnitStorageBehavior usb)
        {
            pickupRequests.RemoveRequestBy(usb);
        }

        public static void RemovePickupRequests(UnitStorageBehavior usb, ResourceType resource)
        {
            pickupRequests.RemoveRequestsByResource(usb, resource);
        }

        private class RequestQueue
        {
            public RequestQueue()
            {

            }

            public List<Request> requests = new List<Request>();

            public bool AddRequest(Request request)
            {
                int index = -1;

                for (int i = 0; i < requests.Count - 1; i++)
                {
                    if (requests.Count < 2)
                        break;

                    if (requests[i] == null)
                        continue;

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
                    if (requests[i] == null)
                        continue;

                    if (requests[i].storage == usb)
                    {
                        requests.RemoveAt(i);
                    }
                }
            }

            public void RemoveRequestsByResource(UnitStorageBehavior usb, ResourceType resouce)
            {
                for (int i = requests.Count - 1; i >= 0; i--)
                {
                    if (requests[i] == null)
                        continue;

                    if (requests[i].resourceCapacity.type == resouce && requests[i].storage == usb)
                    {
                        requests.RemoveAt(i);
                    }
                }
            }
        }

        [System.Serializable]
        public class Request
        {
            public Request(UnitStorageBehavior storage, ResourceAmount resourceAmount, RequestType requestType, bool onMainThread = true)
            {
                this.storage = storage;
                this.resourceCapacity = resourceAmount;
                this.requestType = requestType;
                if (onMainThread)
                    this.timePlaced = Time.timeSinceLevelLoad;
                else
                    this.timePlaced = -1f;
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
            high,
            urgent,
        }

        public enum RequestType
        {
            pickup,
            deliver
        }

    }

}
