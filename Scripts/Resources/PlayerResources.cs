using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using HexGame.Units;
using System.Collections.ObjectModel;
using System.Linq;

namespace HexGame.Resources
{
    public class PlayerResources : MonoBehaviour
    {
        public static Action<ResourceType, int, int> resourceChange;
        /// <summary>
        /// Used for ui bar updates as a given interval
        /// </summary>
        public static Action<ResourceType, int> resourceUpdate;
        /// <summary>
        /// Used to initialize ui bars
        /// </summary>
        public static Action<ResourceType, int> resourceInitialValue;
        private WaitForSeconds updateInterval = new WaitForSeconds(30f);

        [SerializeField]
        [AssetsOnly]
        private List<ResourceTemplate> resourceScriptableObjects = new List<ResourceTemplate>();

        [SerializeField]
        public static List<ResourceAmount> resourceStored = new List<ResourceAmount>();
        public static List<ResourceAmount> resourceInTransit = new List<ResourceAmount>();
        public static List<ResourceAmount> resourceRequested = new List<ResourceAmount>();
        public static List<ResourceAmount> questResources = new List<ResourceAmount>();
        public static List<ResourceAmount> producedResources = new List<ResourceAmount>();
        public static event Action<ResourceType> ResourceProductionStarted;
        public static Dictionary<ResourceType, int> resourceProducedToday = new Dictionary<ResourceType, int>();
        public static Dictionary<ResourceType, int> resourceProducedYesterday = new Dictionary<ResourceType, int>();
        public static Dictionary<ResourceType, int> resourceUsedToday = new Dictionary<ResourceType, int>();
        public static Dictionary<ResourceType, int> resourceUsedYesterday = new Dictionary<ResourceType, int>();

        [SerializeField]
        private int initialStorageLimit = 0;
        private static Dictionary<ResourceType, int> maxStorage = new Dictionary<ResourceType, int>();


        private void OnEnable()
        {
            GlobalStorageBehavior.gloablStorageAdded += StorageAdded;
            GlobalStorageBehavior.resourceAdded += ResourceUpdated;
            GlobalStorageBehavior.resourceRemoved += ResourceUpdated;
            ResourceSink.resourceLost += ResourceLost;
            ResourceProductionBehavior.resourceProduced += CountAsProduced;
            ResourceProductionBehavior.resourceUsed += ResourceUsed;
            CollectionBehavior.terreneStored += CountAsProduced;
            PlaceHolderTileBehavior.resourcesUsed += ResourceUsed;
            WorkerManager.workerConsumedResource += ResourceUsed;
            BuildingSpotBehavior.resourceConsumed += ResourceUsed;

            ResourceStart.initialResourcesAdded += InitializeResourceBars;
            DayNightManager.transitionToDay += ResetProducedToday;
        }



        private void OnDisable()
        {
            GlobalStorageBehavior.gloablStorageAdded -= StorageAdded;
            GlobalStorageBehavior.gloablStorageRemoved -= StorageRemoved;
            GlobalStorageBehavior.resourceAdded -= ResourceUpdated;
            GlobalStorageBehavior.resourceRemoved -= ResourceUpdated;
            ResourceProductionBehavior.resourceProduced -= CountAsProduced;
            ResourceProductionBehavior.resourceUsed -= ResourceUsed;
            CollectionBehavior.terreneStored -= CountAsProduced;
            PlaceHolderTileBehavior.resourcesUsed -= ResourceUsed;
            WorkerManager.workerConsumedResource -= ResourceUsed;
            BuildingSpotBehavior.resourceConsumed -= ResourceUsed;

            ResourceStart.initialResourcesAdded -= InitializeResourceBars;
            DayNightManager.transitionToDay -= ResetProducedToday;
            StopAllCoroutines();
        }


        private void Awake()
        {
            resourceStored.Clear();
            resourceInTransit.Clear();
            resourceRequested.Clear();
            questResources.Clear();
            InitializeStorage();
        }

        private void InitializeStorage()
        {
            maxStorage.Clear();
            foreach (ResourceType resource in System.Enum.GetValues(typeof(ResourceType)))
            {
                maxStorage.Add(resource, initialStorageLimit);
            }
        }

        public static void SetStorageLimit(ResourceType resouce, int amount)
        {
            maxStorage[resouce] = amount;
        }

        [Button]
        public static int GetStorageLimit(ResourceType resource)
        {
            return maxStorage[resource];
        }

        /// <summary>
        /// intended to be used by upgrades to add or subtract amount from max storage
        /// </summary>
        /// <param name="resouce"></param>
        /// <param name="amount"></param>
        public void ChangeStorageLimit(ResourceType resouce, int amount)
        {
            if(!maxStorage.TryGetValue(resouce, out int storage))
                maxStorage.Add(resouce, amount);
            else
            {
                maxStorage[resouce] += amount;
                resourceChange?.Invoke(resouce, GetAmountStored(resouce), GetStorageLimit(resouce));
            }
        }

        public static int GlobalStorageLimit()
        {
            int limit = 0;
            foreach (ResourceType resource in System.Enum.GetValues(typeof(ResourceType)))
            {
                limit += GetStorageLimit(resource);
            }

            return limit;
        }

        public static bool TryUseResource(ResourceAmount resource)
        {

            if (!HasResource(resource))
                return false;

            UseResource(resource);

            return true;
        }

        public static bool TryUseAllResources(List<ResourceAmount> resourceList)
        {
            if (resourceList.Count == 0)
                return true;

            if (!HasAllResources(resourceList))
                return false;

            foreach (var resource in resourceList)
                UseResource(resource);

            return true;
        }

        private static bool HasAllResources(List<ResourceAmount> resourceList)
        {
            foreach (var resource in resourceList)
            {
                if (!HasResource(resource))
                    return false;
            }

            return true;
        }
        public static bool HasResource(ResourceAmount resource)
        {
            if(resource.type == ResourceType.Workers)
            {
                if (WorkerManager.availableWorkers >= resource.amount)
                    return true;
                else
                    return false;
            }

            foreach (var r in resourceStored)
            {
                if (r.amount < 0)
                    Debug.Log($"{r.type} is less than zero");

                if (r.type == resource.type && r.amount >= resource.amount)
                    return true;
            }

            return false;
        }

        public static bool UseResource(ResourceAmount resource)
        {
            if (resource.amount < 0)
            {
                Debug.LogWarning("Use AddResource to add resource amount");
                return false;
            }

            if(resource.type == ResourceType.Workers)
            {
                return WorkerManager.TakeWorkers(resource.amount) == resource.amount;
            }

            for (int i = 0; i < resourceStored.Count; i++)
            {
                if (resourceStored[i].type == resource.type)
                {
                    resourceStored[i] -= resource;
                    resourceChange?.Invoke(resource.type, resourceStored[i].amount, GetStorageLimit(resource.type));
                    return true;
                }
            }

            return false;
        }

        public void AddResource(ResourceAmount resource)
        {
            AddResource(resource.type, resource.amount);
        }

        public void AddResource(ResourceType type, int amount)
        {
            if(!CanStore(new ResourceAmount(type, amount), out ResourceAmount canStore))
            {
                MessagePanel.ShowMessage($"{type.ToNiceString()} storage is full.", null);
            }

            if (canStore.amount <= 0)
                return;

            CountAsCollected(canStore);

            for (int i = 0; i < resourceStored.Count; i++)
            {
                if (resourceStored[i].type == type)
                {
                    resourceStored[i] += canStore;
                    resourceChange?.Invoke(resourceStored[i].type, resourceStored[i].amount, GetStorageLimit(resourceStored[i].type));
                    return;
                }
            }

            //if can't be found... why do I keep using lists for this shit??
            resourceStored.Add(canStore);
            resourceChange?.Invoke(canStore.type, canStore.amount, GetStorageLimit(canStore.type));
        }

        public bool CanStore(ResourceAmount wantToStore, out ResourceAmount canStore)
        {
            int capacity = GetStorageLimit(wantToStore.type) - GetAmountStored(wantToStore.type);

            if (capacity >= wantToStore.amount)
            {
                canStore = wantToStore;
                return true;
            }
            else
            {
                canStore.type = wantToStore.type;
                canStore.amount = capacity;
                return false;
            }
        }

        private void CountAsCollected(ResourceAmount resouce)
        {
            if (questResources.Any(x => x.type == resouce.type))
            {
                ResourceAmount resourceAmount = questResources.First(x => x.type == resouce.type);
                resourceAmount.amount += resouce.amount;
            }
            else
                questResources.Add(resouce);
        }

        private void CountAsProduced(ResourceAmount amount)
        {
            CountAsProduced(null, amount);
        }

        private void CountAsProduced(ResourceProductionBehavior producer, ResourceAmount resouce)
        {
            if (producedResources.Any(x => x.type == resouce.type))
            {
                ResourceAmount resourceAmount = producedResources.First(x => x.type == resouce.type);
                resourceAmount.amount += resouce.amount;
            }
            else
            { 
                ResourceProductionStarted?.Invoke(resouce.type);
                producedResources.Add(resouce); 
            }

            if (resourceProducedToday.ContainsKey(resouce.type))
            {
                resourceProducedToday[resouce.type] += resouce.amount;
            }
            else
            {
                resourceProducedToday.Add(resouce.type, resouce.amount);
            }
        }

        private void ResetProducedToday(int arg1, float arg2)
        {
            resourceProducedYesterday = new Dictionary<ResourceType, int>(resourceProducedToday);
            resourceUsedYesterday = new Dictionary<ResourceType,int>(resourceUsedToday);
            foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
            {
                if(resourceProducedToday.TryGetValue(resource, out int amount))
                    resourceProducedToday[resource] = 0;

                if(resourceUsedToday.TryGetValue(resource, out amount))
                    resourceUsedToday[resource] = 0;
            }

        }

        public static int GetAmountProducedYesterday(ResourceType resource)
        {
            if (resourceProducedYesterday.TryGetValue(resource, out int amount))
                return amount;
            else 
                return 0;
        }

        public static int GetAmountUsedYesderday(ResourceType resource)
        {
            if (resourceUsedYesterday.TryGetValue(resource, out int amount))
                return amount;
            else
                return 0;
        }

        private void ResourceUpdated(ResourceAmount resource)
        {
            foreach (var r in resourceStored)
            {
                if (r.type == resource.type)
                {
                    resourceChange?.Invoke(r.type, r.amount, GetStorageLimit(r.type));
                }
            }
        }

 
        private void ResourceUsed(ResourceProductionBehavior behavior, ResourceAmount resource)
        {
            ResourceUsed(resource);
        }
        private void ResourceUsed(List<ResourceAmount> list)
        {
            foreach (var item in list)
            {
                ResourceUsed(item);
            }
        }

        private void ResourceUsed(ResourceAmount resource)
        {
            if (resourceUsedToday.TryGetValue(resource.type, out int amount))
            {
                resourceUsedToday[resource.type] += resource.amount;
            }
            else
            {
                resourceUsedToday.Add(resource.type, resource.amount);
            }
        }

        public static List<ResourceTemplate> GetResourceList()
        {
            return FindObjectOfType<PlayerResources>().resourceScriptableObjects;
        }

        public ReadOnlyCollection<ResourceTemplate> GetResources()
        {
            return resourceScriptableObjects.AsReadOnly();
        }

        public ResourceTemplate GetResourceTemplate(ResourceType resourceType)
        {
            return resourceScriptableObjects.Where(x => x.type == resourceType).FirstOrDefault();
        }

        [Button]
        public void RefreshResourceList()
        {
            resourceScriptableObjects = HelperFunctions.GetScriptableObjects<ResourceTemplate>("Assets/Prefabs/Resource SOs").OrderBy(x => x.type).ToList();
        }

        private void StorageRemoved(GlobalStorageBehavior gsb)
        {
            foreach (ResourceType resource in gsb.GetAllowedTypes())
            {
                ChangeStorageLimit(resource, -Mathf.FloorToInt(gsb.GetResourceStorageLimit(resource)));
            }
        }

        private void StorageAdded(GlobalStorageBehavior gsb)
        {
            foreach (ResourceType resource in gsb.GetAllowedTypes())
            {
                if (resource == ResourceType.Workers)
                    continue;

                ChangeStorageLimit(resource, Mathf.FloorToInt(gsb.GetResourceStorageLimit(resource)));
            }
        }

        public static int GetAmountStored(ResourceType type)
        {
            if (type == ResourceType.Workers)
                return WorkerManager.availableWorkers;

            foreach (var resource in resourceStored)
            {
                if (type == resource.type)
                    return resource.amount;
            }

            return 0;
        }
        
        public static int GetAmountInTransit(ResourceType type)
        {
            foreach (var resource in resourceInTransit)
            {
                if (type == resource.type)
                    return resource.amount;
            }

            return 0;
        }

        public static int TotalStored()
        {
            int total = 0;
            foreach (var resource in resourceStored)
                total += resource.amount;

            foreach (var resource in resourceInTransit)
                total += resource.amount;

            foreach (var resource in resourceRequested)
                total += resource.amount;

            return total;
        }

        //return total global storage percentage
        public static float PercentFull()
        {
            return  (float)TotalStored()/ (float)GlobalStorageLimit();
        }
        
        public static float PercentFull(ResourceAmount resource)
        {
            return  (float)GetAmountStored(resource.type)/ (float)GetStorageLimit(resource.type);
        }

        public class Resource
        {
            public Resource(ResourceType type, int amount)
            {
                this.type = type;
                this.amount = amount;
            }

            public Resource(ResourceTemplate resource)
            {
                this.type = resource.type;
                this.storageLimit = resource.startingStorage;
            }

            public ResourceType type;
            public int amount;
            public int storageLimit;
        }

        private void ResourceLost(ResourceAmount resource)
        {
            if (resource.type == ResourceType.Workers)
                WorkerManager.WorkersLost(resource.amount);
            else
            {
                UseResource(resource);
                resourceChange?.Invoke(resource.type, resource.amount, GetStorageLimit(resource.type));
            }
        }
        private void InitializeResourceBars()
        {
            ResourceStart.initialResourcesAdded -= InitializeResourceBars;
            StartCoroutine(UpdateResourceValues());
        }

        private IEnumerator UpdateResourceValues()
        {
            foreach (var resource in resourceStored)
            {
                resourceInitialValue?.Invoke(resource.type, resource.amount);
            }

            while (true)
            {
                foreach (var resource in resourceStored)
                {
                    resourceUpdate?.Invoke(resource.type, resource.amount);
                }

                yield return updateInterval;
            }
        }
    }

    public enum ResourceType
    {
        Workers = 0,
        Food = 1,
        Water = 2,
        Energy = 3,
        FeOre = 4,
        FeIngot = 5,
        AlOre = 6,
        AlIngot = 7,
        TiOre = 8,
        TiIngot = 9,
        UOre = 10,
        UIngot = 11,
        Oil = 12,
        Gas = 13,
        Carbon = 14,
        BioWaste = 15,
        IndustrialWaste = 16,
        Terrene = 17,
        Thermite = 18,
        SteelPlate = 19,
        SteelCog = 20,
        AlPlate = 21,
        AlCog = 22,
        Hydrogen = 23,
        Nitrogen = 24,
        Oxygen = 25,
        AmmoniumNitrate = 26,
        cuOre = 27,
        cuIngot = 28,
        CannedFood = 29,
        FuelRod = 30,
        WeaponsGradeUranium = 31,
        ExplosiveShell = 32,
        Sulfer = 33,
        Plastic = 34,
        CarbonFiber = 35,
        Sand = 36,
        Electronics = 37,
        UraniumShells = 38,
        SulfuricAcid = 39,
    }

}
