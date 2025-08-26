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
    public class PlayerResources : MonoBehaviour, ISaveData
    {
        public static Action<ResourceType, int> resourceChange;
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


        private void OnEnable()
        {
            ResourceProductionBehavior.resourceProduced += ResourceProduced;
            ResourceProductionBehavior.resourceConsumed += ResourceConsumed;
            ShipStorageBehavior.resourceConsumed += ResourceConsumed;
            ShipStorageBehavior.resourceImported += ResourceProduced;

            ResourceSink.resourceLost += ResourceConsumed;
            CollectionBehavior.terreneStored += ResourceProduced;
            PlaceHolderTileBehavior.resourcesUsed += ResourceConsumed;
            WorkerManager.workerConsumedResource += ResourceConsumed;
            BuildingSpotBehavior.resourceConsumed += ResourceConsumed;

            ResourceStart.initialResourcesAdded += InitializeResourceBars;
            DayNightManager.transitionToDay += ResetProducedToday;
        }

        private void OnDisable()
        {
            ResourceProductionBehavior.resourceProduced -= ResourceProduced;
            ResourceProductionBehavior.resourceConsumed -= ResourceConsumed;
            ShipStorageBehavior.resourceConsumed -= ResourceConsumed;
            ShipStorageBehavior.resourceImported -= ResourceProduced;

            ResourceSink.resourceLost -= ResourceConsumed;
            CollectionBehavior.terreneStored -= ResourceProduced;
            PlaceHolderTileBehavior.resourcesUsed -= ResourceConsumed;
            WorkerManager.workerConsumedResource -= ResourceConsumed;
            BuildingSpotBehavior.resourceConsumed -= ResourceConsumed;

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
            RegisterDataSaving();
        }

        private void InitializeStorage()
        {
            if (SaveLoadManager.Loading)
                return;

            foreach (ResourceType resource in System.Enum.GetValues(typeof(ResourceType)))
            {
                resourceStored.Add(new ResourceAmount(resource, 0));
            }
        }

        private void ResourceProduced(ResourceAmount amount) => ResourceProduced(null, amount);
        private void ResourceProduced(ResourceProductionBehavior behavior, ResourceAmount amount)
        {
            AddResource(amount);
            CountAsProduced(amount);
        }

        private void ResourceConsumed(List<ResourceAmount> resources)
        {
            for (int i = 0; i < resources.Count; i++)
            {
                ResourceConsumed(resources[i]);
            }
        }

        private void ResourceConsumed(ResourceAmount amount) => ResourceConsumed(null, amount);
        private void ResourceConsumed(ResourceProductionBehavior behavior, ResourceAmount amount)
        {
            RemoveResource(amount);
            CountAsUsed(amount);
        }

        private void ResourceShipped(SupplyShipBehavior behavior, RequestType requestType, ResourceType resourceType)
        {
            if(requestType == RequestType.sell)
                ResourceConsumed(null, new ResourceAmount(resourceType, SupplyShipManager.supplyShipCapacity));
            else
                ResourceProduced(null, new ResourceAmount(resourceType, SupplyShipManager.supplyShipCapacity));
        }

        private bool RemoveResource(ResourceAmount resource)
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
                    resourceChange?.Invoke(resource.type, resourceStored[i].amount);
                    return true;
                }
            }

            return false;
        }

        public void AddResource(List<ResourceAmount> resources)
        {
            for (int i = 0; i < resources.Count; i++)
            {
                AddResource(resources[i]);
            }
        }

        public void AddResource(ResourceAmount resource)
        {
            CountAsCollected(resource);

            for (int i = 0; i < resourceStored.Count; i++)
            {
                if (resourceStored[i].type == resource.type)
                {
                    resourceStored[i] += resource;
                    resourceChange?.Invoke(resourceStored[i].type, resourceStored[i].amount);
                    return;
                }
            }

            //if can't be found... why do I keep using lists for this shit??
            resourceStored.Add(resource);
            resourceChange?.Invoke(resource.type, resource.amount);
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

        private void CountAsProduced(ResourceAmount resouce)
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
            IList list = Enum.GetValues(typeof(ResourceType));
            for (int i = 0; i < list.Count; i++)
            {
                ResourceType resource = (ResourceType)list[i];
                if (resourceProducedToday.TryGetValue(resource, out int amount))
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

        private void ResourceUpdated(ResourceProductionBehavior rpb, ResourceAmount resource)
        {
            for (int i = 0; i < resourceStored.Count; i++)
            {
                ResourceAmount r = resourceStored[i] + resource;
                if (r.type == resource.type)
                {
                    resourceChange?.Invoke(r.type, r.amount);
                }
            }
        }

        private void CountAsUsed(List<ResourceAmount> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                this.CountAsUsed(list[i]);
            }
        }

        private void CountAsUsed(ResourceAmount resource)
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
            resourceScriptableObjects = HelperFunctions.GetScriptableObjects<ResourceTemplate>("Assets/ScriptableObjects/Resource SOs").OrderBy(x => x.type).ToList();
        }

        public static int GetAmountStored(ResourceType type)
        {
            if (type == ResourceType.Workers)
                return WorkerManager.AvailableWorkers;

            for (int i = 0; i < resourceStored.Count; i++)
            {
                ResourceAmount resource = resourceStored[i];
                if (type == resource.type)
                    return resource.amount;
            }

            return 0;
        }

        public void TryReturnResource(ResourceAmount resourceCapacity)
        {
            AddResource(resourceCapacity);
        }

        private void ResourceLost(ResourceAmount resource)
        {
            if (resource.type == ResourceType.Workers)
                WorkerManager.WorkersLost(resource.amount);
            else
            {
                ResourceConsumed(resource);
                resourceChange?.Invoke(resource.type, resource.amount);
            }
        }
        private void InitializeResourceBars()
        {
            ResourceStart.initialResourcesAdded -= InitializeResourceBars;
            StartCoroutine(UpdateResourceValues());
        }

        private IEnumerator UpdateResourceValues()
        {
            for (int i = 0; i < resourceStored.Count; i++)
            {
                ResourceAmount resource = resourceStored[i];
                resourceInitialValue?.Invoke(resource.type, resource.amount);
            }

            while (true)
            {
                for (int i = 0; i < resourceStored.Count; i++)
                {
                    ResourceAmount resource = resourceStored[i];
                    resourceUpdate?.Invoke(resource.type, resource.amount);
                }

                yield return updateInterval;
            }
        }

        private const string RESOURCE_SAVE_PATH = "ResourceData";

        public void RegisterDataSaving()
        {
            SaveLoadManager.RegisterData(this,2);
        }

        public void Save(string savePath, ES3Writer writer)
        {
            PlayerResourceData playerResourceData = new PlayerResourceData
            {
                resourceStored = PlayerResources.resourceStored,
                questResources = PlayerResources.questResources,
                producedResources = PlayerResources.producedResources,
                resourceProducedToday = PlayerResources.resourceProducedToday,
                resourceProducedYesterday = PlayerResources.resourceProducedYesterday,
                resourceUsedToday = PlayerResources.resourceUsedToday,
                resourceUsedYesterday = PlayerResources.resourceUsedYesterday,
                maxStorage = null
            };

            writer.Write<PlayerResourceData>(RESOURCE_SAVE_PATH, playerResourceData);
        }

        public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
        {
            if(ES3.KeyExists(RESOURCE_SAVE_PATH, loadPath))
            {
                PlayerResourceData playerResourceData = ES3.Load<PlayerResourceData>(RESOURCE_SAVE_PATH, loadPath);

                //PlayerResources.resourceStored = playerResourceData.resourceStored.CombineLists(PlayerResources.resourceInTransit);
                //foreach (var resource in PlayerResources.resourceStored)
                //{
                //    postUpdateMessage?.Invoke($"Storing {resource.type.ToNiceString()}");
                //    resourceChange?.Invoke(resource.type, resource.amount);
                //}
                PlayerResources.questResources = playerResourceData.questResources;
                PlayerResources.producedResources = playerResourceData.producedResources;
                PlayerResources.resourceProducedToday = playerResourceData.resourceProducedToday;
                PlayerResources.resourceProducedYesterday = playerResourceData.resourceProducedYesterday;
                PlayerResources.resourceUsedToday = playerResourceData.resourceUsedToday;
                PlayerResources.resourceUsedYesterday = playerResourceData.resourceUsedYesterday;
            }

            StartCoroutine(UpdateResourceValues());
            yield return null;
        }

        internal static int GetStorageLimit(ResourceType resource)
        {
            return 1000;
        }

        public struct PlayerResourceData
        {
            public List<ResourceAmount> resourceStored;
            public List<ResourceAmount> questResources;
            public List<ResourceAmount> producedResources;
            public Dictionary<ResourceType, int> resourceProducedToday;
            public Dictionary<ResourceType, int> resourceProducedYesterday;
            public Dictionary<ResourceType, int> resourceUsedToday;
            public Dictionary<ResourceType, int> resourceUsedYesterday;
            public Dictionary<ResourceType, int> maxStorage;
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
        IronCog = 20,
        AlPlate = 21,
        AlCog = 22,
        Hydrogen = 23,
        Nitrogen = 24,
        Oxygen = 25,
        AmmoniumNitrate = 26,
        CuOre = 27,
        CuIngot = 28,
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
        Biomass = 40,
        TerraFluxCell = 41,
    }

}
