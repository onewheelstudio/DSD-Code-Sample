using HexGame.Grid;
using HexGame.Units;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HexGame.Resources
{
    [RequireComponent(typeof(Unit))]
    [RequireComponent(typeof(UnitStorageBehavior))]
    public class ResourceProductionBehavior : UnitBehavior, IProduceResource, IHaveReceipes, IHavePopupInfo, IHaveHappiness
    {
        [SerializeField]
        private float efficiencyTime = 1f;
        [SerializeField]
        protected ResourceProduction production;
        public event Action<ResourceProduction> recipeChanged;
        [SerializeField]
        protected List<ResourceProduction> receipes = new List<ResourceProduction>();
        protected Coroutine productionCorountine;
        protected UnitStorageBehavior usb;
        public static event Action<ResourceProductionBehavior> productionAdded;
        public static event Action<ResourceProductionBehavior> productionRemoved;
        public static event Action<ResourceProductionBehavior, ResourceAmount> resourceProduced;
        public static event Action<ResourceProductionBehavior, ResourceAmount> resourceConsumed;
        private StatusIndicator statusIndicator;
        private float timeToProduce;
        private float startTime;
        [SerializeField] private List<ResourceTile> resourceTiles = new List<ResourceTile>();
        [SerializeField] private List<HexTile> requiredTiles = new List<HexTile>();
        [SerializeField] private List<HexTileType> requiredTileTypes;
        private static ResourceProductionManager resourceProductionManager;
        [SerializeField] private bool moreTileMoreProduction = false;
        [SerializeField, ShowIf("moreTileMoreProduction")] private List<float> productionCurve = new List<float>(6);
        public bool isProducing = false;
        private float upTime;
        private List<ResourceType> missingResources = new();

        private Queue<int> upTimeLongQueue = new Queue<int>();
        private Queue<int> upTimeShortQueue = new Queue<int>();
        private int longUpTimeAcculumation = 0;
        private int shortUpTimeAcculumation = 0;
        private int longQueueSamples = 30;
        private int shortQueueSamples = 10;

        private Hex3 location;
        private Vector3 position;
        public Vector3 Position => position;
        private Camera _camera;
        private Camera Camera => _camera == null ? _camera = Camera.main : _camera;


        private void Awake()
        {
            if (resourceProductionManager == null)
                resourceProductionManager = FindObjectOfType<ResourceProductionManager>();

            //DuplicateUseNearSOs();
        }

        private void DuplicateUseNearSOs()
        {
            for (int i = 0; i < receipes.Count; i++)
            {
                for (int j = 0; j < receipes[i].useConditions.Count; j++)
                {
                    if (receipes[i].useConditions[j] is UseNearTile useNearTile)
                    {
                        receipes[i].useConditions[j] = Instantiate(receipes[i].useConditions[j]);
                        receipes[i].useConditions[j].name = receipes[i].useConditions[j].name + " Copy";
                    }
                }
            }
        }

        public override void StartBehavior()
        {
            isFunctional = true;
            position = this.transform.position;
            location = position.ToHex3();

            if (!Application.isPlaying)
                return;

            if (usb == null)
                usb = this.GetComponent<UnitStorageBehavior>();

            usb.resourceDelivered += CheckResources;
            //usb.RequestWorkers();

            //if (productionCorountine == null)
            //    productionCorountine = StartCoroutine(DoProduction(storageBehavior.StoreResource));

            if (statusIndicator == null)
                statusIndicator = this.GetComponentInChildren<StatusIndicator>();

            productionAdded?.Invoke(this);

            UpdateEfficiency();
            PlayerUnit.unitCreated += UpdateEfficiency;
            PlayerUnit.unitRemoved += UpdateEfficiency;

            if (moreTileMoreProduction)
                HexTile.NewHexTile += UpdateRequiredTiles;

            int recipeIndex = GetFirstFunctionableRecipe();
            SetReceipe(recipeIndex);
            WorkerManager.AddHappyBuilding(this, this.GetComponent<PlayerUnit>());
            SetWarningStatus();
        }

        private void UpdateEfficiency(Unit unit)
        {
            UpdateEfficiency();
        }

        private void UpdateEfficiency()
        {
            if (production != null)
                efficiencyTime = production.ProductivityBoost(this, location);
        }

        public override void StopBehavior()
        {
            if (GameStateManager.LeavingScene)
                return;

            isFunctional = false;
            if (!Application.isPlaying)
                return;
            isProducing = false;
            productionRemoved?.Invoke(this);
            PlayerUnit.unitCreated -= UpdateEfficiency;
            PlayerUnit.unitRemoved -= UpdateEfficiency;

            if (moreTileMoreProduction)
                HexTile.NewHexTile -= UpdateRequiredTiles;

            WorkerManager.RemoveHappyBuilding(this);
            if(warningIconInstance != null)
                warningIconInstance.gameObject.SetActive(false);
        }



        protected void OnDisable()
        {
            PlayerUnit.unitCreated -= UpdateEfficiency;
            PlayerUnit.unitRemoved -= UpdateEfficiency; 
            usb.resourceDelivered -= CheckResources;

            if (GameStateManager.LeavingScene)
                return;

            StopBehavior();
        }

        private void Update()
        {
            if (!_isFunctional)
            {
                statusIndicator?.SetStatus(StatusIndicator.Status.red);
                return;
            }
        }


        public virtual IEnumerator DoProduction(Action<ResourceAmount> request)
        {
            while (true)
            {
                //can we or should we do production
                if (!CanIProduce())
                {
                    SetWarningStatus();
                    yield return null;
                    continue;
                }
                else if (issueList.Count > 0)
                {
                    //possible to be low on workers and still functional
                    SetWarningStatus();
                }
                else if (hasWarningIcon)
                {
                    //turn off and return to pool
                    warningIconInstance.ToggleIconsOff();
                }

                //do this early to prevent double dipping? 
                RemoveResourceFromTile();

                //yes, we can do wait time to produce
                statusIndicator?.SetStatus(StatusIndicator.Status.green);

                timeToProduce = production.GetTimeToProduce() * production.ProductivityBoost(this, location) / (usb.efficiency);// * efficiencyTime);
                timeToProduce /= GameConstants.GameSpeed;
                if (GetStat(Stat.workers) > 0)
                    timeToProduce /= WorkerManager.globalWorkerEfficiency;

                startTime = Time.timeSinceLevelLoad;
                yield return new WaitForSeconds(timeToProduce);

                if (!CanIProduce())
                {
                    statusIndicator?.SetStatus(StatusIndicator.Status.yellow);
                    yield return null;
                    continue;
                }

                CreateProducts();
            }
        }

        public bool CanProduceAndUpdateStatus()
        {
            //can we or should we do production
            if (!CanIProduce())
            {
                SetWarningStatus();
                return false;
            }
            else if (issueList.Count > 0)
            {
                //possible to be low on workers and still functional
                SetWarningStatus();
            }
            else if (hasWarningIcon)
            {
                //turn off and return to pool
                warningIconInstance.ToggleIconsOff();
            }

            return true;
        }

        public void StartProduction()
        {
            //do this early to prevent double dipping? 
            RemoveResourceFromTile();
            isProducing = true;
            startTime = Time.time;

            //yes, we can do wait time to produce
            statusIndicator?.SetStatus(StatusIndicator.Status.green);
        }

        public void CreateProducts()
        {
            //do the resources exist to make our produces?
            if (usb.TryUseAllResources(production.GetCost().ToList()))
            {
                foreach (var resource in production.GetProduction())
                {
                    usb.StoreResource(new ResourceAmount(resource.type, resource.amount));
                    resourceProduced?.Invoke(this, new ResourceAmount(resource.type, resource.amount));
                }
                
                foreach (var resource in production.GetCost())
                {
                    resourceConsumed?.Invoke(this, new ResourceAmount(resource.type, resource.amount));
                }
            }

            isProducing = false;
        }

        private void CheckResources(UnitStorageBehavior behavior, ResourceAmount amount)
        {
            CanProduceAndUpdateStatus();
        }

        private void SetWarningStatus()
        {
            if (!IsPositionVisible(Camera))
                return;

            if (warningIconInstance == null)
            {
                warningIconInstance = UnitManager.warningIcons.PullGameObject(this.transform.position, Quaternion.identity).GetComponent<WarningIcons>();
                warningIconInstance.transform.SetParent(this.transform);
            }

            warningIconInstance.SetWarnings(issueList);
            //warningIconInstance.SetResourceWarnings(missingResources);
            statusIndicator?.SetStatus(StatusIndicator.Status.yellow);
        }

        private bool IsPositionVisible(Camera cam)
        {
            Vector3 viewportPoint = cam.WorldToViewportPoint(this.position);

            // Check if the point is in front of the camera
            if (viewportPoint.z < 0)
                return false;

            // Check if the point is within the camera's viewport rectangle (0 to 1 in x and y)
            return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                   viewportPoint.y >= 0 && viewportPoint.y <= 1;
        }

        private void RemoveResourceFromTile()
        {
            if (resourceTiles.Count == 0)
                return;

            foreach (var tile in resourceTiles.OrderBy(t => Guid.NewGuid()))
            {
                if(tile.TryExtractResource(production.GetProduction()[0].amount))
                    break;
            }
        }

        public bool CanIProduce()
        {
            //If we aren't visible then no need to update the issue list
            if (!IsPositionVisible(Camera))
                return CanIProduceFast();

            issueList.Clear();
            if (!production.CanProduce(this, location))
                issueList.Add(ProductionIssue.blocked);

            float efficiency = usb.efficiency;
            if (efficiency <= 0.01f)
                issueList.Add(ProductionIssue.noWorkers);
            else if(efficiency < 1f)
                issueList.Add(ProductionIssue.missingWorkers);

            if (!CanStoreProducts())
                issueList.Add(ProductionIssue.fullStorage);

            if (CheckMissingResource())
                issueList.Add(ProductionIssue.missingResources);

            if (issueList.Count == 1 && issueList[0] == ProductionIssue.missingWorkers)
                return true;

            //keep this at the end - because of return
            if(resourceTiles.Count > 0)
            {
                foreach (var tile in resourceTiles)
                {
                    //if one tile has resources, we are good
                    if (tile.ResourceAmount > production.GetProduction()[0].amount)
                        return issueList.Count == 0;
                }
                issueList.Add(ProductionIssue.missingResources);
            }

            return issueList.Count == 0;
        }

        private bool CheckMissingResource()
        {
            if (warningIconInstance == null)
            {
                warningIconInstance = UnitManager.warningIcons.PullGameObject(this.transform.position, Quaternion.identity).GetComponent<WarningIcons>();
                warningIconInstance.transform.SetParent(this.transform);
            }

            this.missingResources.Clear();

            var productionCost = this.production.GetCost();
            for (int i = 0; i < productionCost.Count; i++)
            {
                if (usb.GetAmountStored(productionCost[i].type) < productionCost[i].amount)
                {
                    missingResources.Add(productionCost[i].type);
                }
            }

            warningIconInstance.SetResourceWarnings(missingResources);

            return this.missingResources.Count > 0;
        }

        /// <summary>
        /// Does not create a list of issues. Should reduce GC.
        /// </summary>
        /// <returns></returns>
        public bool CanIProduceFast()
        {
            if (!production.CanProduce(this, location))
                return false;

            float efficiency = usb.efficiency;
            if (efficiency <= 0.01f)
                return false;

            if (!CanStoreProducts())
                return false;

            if (!usb.HasAllResources(production.GetCost()))
                return false;

            //keep this at the end - because of return
            if(resourceTiles.Count > 0)
            {
                foreach (var tile in resourceTiles)
                {
                    //if one tile has resources, we are good
                    if (tile.ResourceAmount > production.GetProduction()[0].amount)
                        return true;
                }
            }

            return false;
        }

        private bool CanStoreProducts()
        {
            foreach (var resource in production.GetProduction())
            {
                //checking if resources produced can be picked up after storage... naming is a bit confusing
                if (!usb.CanStoreForPickup(resource))
                {
                    return false;
                }
            }

            return true;
        }

        public List<ResourceProduction> GetReceipes()
        {
            List<ResourceProduction> recipes = new List<ResourceProduction>(); ;
            foreach (var receipe in receipes)
            {
                if(receipe.IsUnlocked)
                    recipes.Add(receipe);
            }
            return recipes;
        }

        public int GetCurrentRecipe()
        {
            //hackery due to the dropdown index not matching the receipes list index
            int index = receipes.IndexOf(production);
            int unlockedIndex = 0;
            for (int i = 0; i < receipes.Count; i++)
            {
                if (receipes[i].IsUnlocked && receipes[i] != production)
                {
                    unlockedIndex++;
                    continue;
                }
                else if (receipes[i].IsUnlocked && receipes[i] == production)
                {
                    break;
                }
            }

            return unlockedIndex;
        }

        public int GetRecipeIndex()
        {
            return receipes.IndexOf(production);
        }

        public float GetEfficiency()
        {
            return Mathf.Max(0, (1f / efficiencyTime)) * 100;
        }

        public float GetUpTime()
        {
            if(upTimeLongQueue.Count == 0)
                return 0f;

            float longUpTime = (float)longUpTimeAcculumation / (float)upTimeLongQueue.Count;
            float shortUpTime = (float)shortUpTimeAcculumation / (float)upTimeShortQueue.Count;

            return Mathf.Max(shortUpTime, longUpTime);
        }

        public void UpdateUpTime()
        {
            if(isProducing)
            {
                upTimeLongQueue.Enqueue(1);
                longUpTimeAcculumation += 1;
                upTimeShortQueue.Enqueue(1);
                shortUpTimeAcculumation += 1;
            }
            else
            {
                upTimeLongQueue.Enqueue(0); 
                upTimeShortQueue.Enqueue(0);
            }

            if (upTimeLongQueue.Count > longQueueSamples)
            {
                longUpTimeAcculumation -= upTimeLongQueue.Dequeue();
            }

            if(upTimeShortQueue.Count > shortQueueSamples)
            {
                shortUpTimeAcculumation -= upTimeShortQueue.Dequeue();
            }
        }

        private int GetFirstFunctionableRecipe()
        {
            foreach (var receipe in receipes)
            {
                if (receipe.IsUnlocked && receipe.CanProduce(this, location))
                    return receipes.IndexOf(receipe);
            }

            return 0;
        }
         
        public void SetReceipe(int receipeIndex)
        {
            if (receipes.Count <= receipeIndex)
            {
                MessagePanel.ShowMessage("That receipe doesn't exist.", this.gameObject);
                return;
            }
            else
            {
                //the index from the dropdown doesn't match the index of the receipes list
                int count = 0;
                for (int i = 0; i < receipes.Count; i++)
                {
                    if (receipes[i].IsUnlocked && count != receipeIndex)
                    {
                        count++;
                    }
                    else if (receipes[i].IsUnlocked && count == receipeIndex)
                    {
                        receipeIndex = i;
                        break;
                    }
                }

                usb.AdjustStorageForRecipe(production, receipes[receipeIndex]);
                production = receipes[receipeIndex];

                resourceTiles.Clear();
                requiredTiles.Clear();
                requiredTileTypes.Clear();
                foreach (var useCondition in production.useConditions)
                {
                    if (useCondition is UseNearTile useNearTile)
                        GetResourceTiles(useNearTile);

                    if (useCondition is UseOnTile useOnTile)
                        GetResourceTile(useOnTile);
                }

                recipeChanged?.Invoke(production);
            }
        }

        public void SetRecipeIndex(int index)
        {
            if(index >= receipes.Count)
            {
                index = GetFirstFunctionableRecipe();
            }

            usb.AdjustStorageForRecipe(production, receipes[index]);
            production = receipes[index];

            resourceTiles.Clear();
            requiredTiles.Clear();
            requiredTileTypes.Clear();
            foreach (var useCondition in production.useConditions)
            {
                if (useCondition is UseNearTile useNearTile)
                {
                    GetResourceTiles(useNearTile);
                    requiredTileTypes.AddRange(useNearTile.RequiredTiles);
                }

                if(useCondition is UseOnTile useOnTile)
                {
                    GetResourceTile(useOnTile);
                }
            }

            recipeChanged?.Invoke(production);
        }

        public void GetResourceTiles(UseNearTile useNearTile)
        {
            List<Hex3> neighbors = HexTileManager.GetHex3WithInRange(location, 0, useNearTile.Range);
            List<HexTile> tiles = HexTileManager.GetTilesAtLocations(neighbors);

            foreach (var tile in tiles)
            {
                if (!useNearTile.RequiredTiles.Contains(tile.TileType))
                    continue;
                else
                    requiredTiles.Add(tile);    

                if (!tile.TryGetComponent(out ResourceTile resourceTile))
                    continue;

                if (resourceTile.ResourceAmount <= 0)
                    continue;

                if (HelperFunctions.HexRangeFloat(tile.transform.position, this.transform.position) > useNearTile.Range)
                    continue;

                resourceTiles.Add(resourceTile);
            }
        }
        
        public void GetResourceTile(UseOnTile useOnTile)
        {

            HexTile tile = HexTileManager.GetHexTileAtLocation(location);
            if(useOnTile.CanUse(this, location) && tile.TryGetComponent(out ResourceTile resourceTile))
            {
                resourceTiles.Add(resourceTile);
            }
        }

        private void UpdateRequiredTiles(HexTile tile)
        {
            if(requiredTileTypes.Contains(tile.TileType))
            {
                requiredTiles.Add(tile);
            }
        }

        public List<PopUpInfo> GetPopupInfo()
        {
            return new List<PopUpInfo>()
            {
                new PopUpInfo($"\nEfficiency: {Mathf.RoundToInt(1f/production.ProductivityBoost(this, location) * 100f)}%", 1000, PopUpInfo.PopUpInfoType.stats)
            };
        }

        public float GetTimeToProduce()
        {
            if(usb == null || usb.efficiency <= 0.01f)
                return 0f;

            float time = production.GetTimeToProduce() * production.ProductivityBoost(this, location) / (usb.efficiency);

            if (GetStat(Stat.workers) > 0)
                time /= WorkerManager.globalWorkerEfficiency;

            time /= GameConstants.GameSpeed;

            if(moreTileMoreProduction)
            {
                time /= productionCurve[requiredTiles.Count];
            }

            return time;
        }

        public float GetStartTime()
        {
            if (!isFunctional)
                return Time.time - GetTimeToProduce();
            else
                return startTime;
        }

        public int GetHappiness()
        {
            return GetIntStat(Stat.happiness);
        }

        public string GetHappinessString()
        {
            return " - Near Housing";
        }



        public List<bool> GetRecipeStatus()
        {
            List<bool> recipeStatus = new List<bool>();
            foreach (var recipe in receipes)
            {
                if (recipe == null)
                {
                    Debug.Log($"Missing recipe from {this.gameObject.name}", this.gameObject);
                    continue;
                }
                recipeStatus.Add(recipe.IsUnlocked);
            }
            return recipeStatus;
        }

        public void SetRecipeStatus(List<bool> recipeStatus)
        {
            for (int i = 0; i < receipes.Count; i++)
            {
                if (receipes[i] == null)
                    continue;

                if(i < recipeStatus.Count && recipeStatus[i])
                    receipes[i].Unlock();
            }
        }
        public enum ProductionIssue
        {
            notPowered,
            blocked,
            fullStorage,
            missingResources,
            noWorkers,
            missingWorkers,
        }

        private Dictionary<int,List<Hex3>> neighborLocationCache = new();
        public List<Hex3> GetNeighborsInRange(int range)
        {
            if (neighborLocationCache.TryGetValue(range, out List<Hex3> neighbors))
                return neighbors;
            else if (range == 0)
            {
                neighborLocationCache.Add(range, new List<Hex3>() { location });
                return GetNeighborsInRange(0);
            }
            else
            {
                neighbors = new List<Hex3>();
                Hex3.GetNeighborsAtDistance(location, range, ref neighbors);
                neighborLocationCache.Add(range, neighbors);
                return GetNeighborsInRange(range);
            }
        }

    }
}
