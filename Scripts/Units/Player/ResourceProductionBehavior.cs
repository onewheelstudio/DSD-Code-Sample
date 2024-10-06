using HexGame.Units;
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
        protected UnitStorageBehavior storageBehavior;
        public static event System.Action<ResourceProductionBehavior> productionAdded;
        public static event System.Action<ResourceProductionBehavior> productionRemoved;
        public static event System.Action<ResourceProductionBehavior, ResourceAmount> resourceProduced;
        public static event System.Action<ResourceProductionBehavior, ResourceAmount> resourceUsed;
        private StatusIndicator statusIndicator;
        private float timeToProduce;
        private float startTime;
        [SerializeField] private List<ResourceTile> resourceTiles = new List<ResourceTile>();
        private static ResourceProductionManager resourceProductionManager;
        public bool isProducing = false;

        private void Awake()
        {
            if (resourceProductionManager == null)
                resourceProductionManager = FindObjectOfType<ResourceProductionManager>();
        }

        public override void StartBehavior()
        {
            isFunctional = true;

            if (!Application.isPlaying)
                return;

            if (storageBehavior == null)
                storageBehavior = this.GetComponent<UnitStorageBehavior>();

            storageBehavior.resourceDelivered += CheckResources;
            storageBehavior.RequestWorkers();

            //if (productionCorountine == null)
            //    productionCorountine = StartCoroutine(DoProduction(storageBehavior.StoreResource));

            if (statusIndicator == null)
                statusIndicator = this.GetComponentInChildren<StatusIndicator>();

            productionAdded?.Invoke(this);

            UpdateEfficiency();
            PlayerUnit.unitCreated += UpdateEfficiency;
            PlayerUnit.unitRemoved += UpdateEfficiency;

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
                efficiencyTime = production.ProductivityBoost(this.gameObject);
        }

        public override void StopBehavior()
        {
            isFunctional = false;
            if (!Application.isPlaying)
                return;
            isProducing = false;
            productionRemoved?.Invoke(this);
            PlayerUnit.unitCreated -= UpdateEfficiency;
            PlayerUnit.unitRemoved -= UpdateEfficiency;
            WorkerManager.RemoveHappyBuilding(this);
            SetWarningStatus();
        }



        protected void OnDisable()
        {
            StopBehavior();
            storageBehavior.resourceDelivered -= CheckResources;
        }

        private void Update()
        {
            if (!_isFunctional)
            {
                statusIndicator?.SetStatus(StatusIndicator.Status.red);
                return;
            }

            if (isProducing)
                return;

                //check needed resources
            foreach (var resource in production.GetCost())
            {
                storageBehavior.CheckResourceLevels(resource);
                storageBehavior.RequestWorkers();
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

                timeToProduce = production.GetTimeToProduce() * production.ProductivityBoost(this.gameObject) / (storageBehavior.efficiency);// * efficiencyTime);
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
            if (storageBehavior.TryUseAllResources(production.GetCost().ToList()))
            {
                foreach (var resource in production.GetProduction())
                {
                    storageBehavior.StoreResource(new ResourceAmount(resource.type, resource.amount));
                    resourceProduced?.Invoke(this, new ResourceAmount(resource.type, resource.amount));
                }
                
                foreach (var resource in production.GetCost())
                {
                    resourceUsed?.Invoke(this, new ResourceAmount(resource.type, resource.amount));
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
            if (!hasWarningIcon)
            {
                warningIconInstance = UnitManager.warningIcons.PullGameObject(this.transform.position, Quaternion.identity).GetComponent<WarningIcons>();
                warningIconInstance.transform.SetParent(this.transform);
            }

            warningIconInstance.SetWarnings(issueList);
            statusIndicator?.SetStatus(StatusIndicator.Status.yellow);
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
            issueList.Clear();
            if (!production.CanProduce(this.gameObject))
                issueList.Add(ProductionIssue.blocked);

            float efficiency = storageBehavior.efficiency;
            if (efficiency <= 0.01f)
                issueList.Add(ProductionIssue.noWorkers);
            else if(efficiency < 1f)
                issueList.Add(ProductionIssue.missingWorkers);

            if (!CanStoreProducts())
                issueList.Add(ProductionIssue.fullStorage);

            if (!storageBehavior.HasAllResources(production.GetCost().ToList()))
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

        private bool CanStoreProducts()
        {
            foreach (var resource in production.GetProduction())
            {
                if (!storageBehavior.CanStoreResource(resource))
                {
                    return false;
                }
            }

            return true;
        }

        public List<ResourceAmount> GetResourcesUsed()
        {
            if (production == null)
                return new List<ResourceAmount>();

            return production.GetCost();
        }

        public List<ResourceAmount> GetResourcesProduced()
        {
            return production.GetProduction();
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
                    break;
                }
                else if (receipes[i].IsUnlocked && receipes[i] == production)
                {
                    break;
                }
            }

            return unlockedIndex;
        }

        public float GetEfficiency()
        {
            return Mathf.Max(0, (1f / efficiencyTime)) * 100;
        }

        private int GetFirstFunctionableRecipe()
        {
            foreach (var receipe in receipes)
            {
                if (receipe.IsUnlocked && receipe.CanProduce(this.gameObject))
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


                storageBehavior.AdjustStorageForRecipe(production, receipes[receipeIndex]);
                production = receipes[receipeIndex];

                resourceTiles.Clear();
                foreach (var useCondition in production.useConditions)
                {
                    if (useCondition is UseNearTile useNearTile)
                        GetResourceTiles(useNearTile);
                }

                recipeChanged?.Invoke(production);
            }
        }

        public void GetResourceTiles(UseNearTile useCondition)
        {
            foreach (var tile in FindObjectsOfType<ResourceTile>())
            {
                if (tile.TileType != useCondition.RequiredTileType)
                    continue;

                if(tile.ResourceAmount <= 0)
                    continue;

                if (HelperFunctions.HexRangeFloat(tile.transform.position, this.transform.position) > useCondition.Range)
                    continue;
                
                resourceTiles.Add(tile);
            }
        }

        public List<PopUpInfo> GetPopupInfo()
        {
            return new List<PopUpInfo>()
            {
                new PopUpInfo($"\nEfficiency: {Mathf.RoundToInt(1f/production.ProductivityBoost(this.gameObject) * 100f)}%", 1000, PopUpInfo.PopUpInfoType.stats)
            };
        }

        public float GetTimeToProduce()
        {
            if(storageBehavior == null || storageBehavior.efficiency <= 0.01f)
                return 0f;

            float time = production.GetTimeToProduce() * production.ProductivityBoost(this.gameObject) / (storageBehavior.efficiency);

            if (GetStat(Stat.workers) > 0)
                time /= WorkerManager.globalWorkerEfficiency;

            time /= GameConstants.GameSpeed;

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

        public enum ProductionIssue
        {
            notPowered,
            blocked,
            fullStorage,
            missingResources,
            noWorkers,
            missingWorkers,
        }
    }
}
