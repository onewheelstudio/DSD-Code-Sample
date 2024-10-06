using HexGame.Grid;
using HexGame.Resources;
using System;
using System.Collections.Generic;
using UnityEngine;
using Forge3D;
using static HexGame.Resources.ResourceProductionBehavior;

namespace HexGame.Units
{
    [RequireComponent(typeof(UnitToolTip))]
    [RequireComponent(typeof(OWS.ObjectPooling.PoolObject))]
    public class PlayerUnit : Unit, IPlaceable, IHavePopupInfo, IHavePopUpButtons, ICanToggle, IHaveStats
    {
        public PlayerUnitType unitType;
        [SerializeField] protected List<StatBoost> statBoosts = new List<StatBoost>();
        public static event Action<PlayerUnit> playerUnitDamaged;
        private ColorManager colorManager;
        protected bool isActive = true;
        protected WarningIcons warningIconInstance;
        public bool hasWarningIcon => warningIconInstance != null && warningIconInstance.transform.parent == this.transform;
        protected List<ProductionIssue> issueList = new List<ProductionIssue>();

        [Header("Location")]
        [SerializeField] private bool canMove = false;
        private Hex3 location;
        public Hex3 Location
        {
            get
            {
                if (canMove)
                    location = this.transform.position.ToHex3();

                return location;
            }
        }

        private Forcefield forcefield;  

        private new void OnEnable()
        {
            location = this.transform.position.ToHex3();
            base.OnEnable();
            stats.upgradeApplied += UpgradeApplied;
            colorManager = FindObjectOfType<ColorManager>();
            forcefield = this.GetComponentInChildren<Forcefield>();
        }

        private new void OnDisable()
        {
            base.OnDisable();
            stats.upgradeApplied -= UpgradeApplied;
            CargoManager.RemoveAllRequests(this.GetComponent<UnitStorageBehavior>());
            //remove reputation
        }

        private void UpgradeApplied(Stats stats, StatsUpgrade upgrade)
        {
            if (stats != this.stats)
                return;

            if (upgrade.unitType != unitType)
                return;

            if(upgrade.upgradeToApply.ContainsKey(Stat.shield))
            {
                if (forcefield == null)
                    return;

                this.RenewShields(0);
            }
            else if (upgrade.upgradeToApply.ContainsKey(Stat.sightDistance))
            {
                FogRevealer fogRevealer = this.GetComponentInChildren<FogRevealer>();
                fogRevealer.UpdateSightDistance();

            }
            else if(upgrade.upgradeToApply.ContainsKey(Stat.hitPoints))
            {
               RestoreHP(upgrade.upgradeToApply[Stat.hitPoints]);
            }
        }

        public override void Place()
        {
            base.Place();
            if(unitType != PlayerUnitType.buildingSpot && unitType != PlayerUnitType.cargoShuttle)
                ReputationManager.ChangeReputation((int)GetStat(Stat.reputation));
            location = this.transform.position.ToHex3();
            isActive = true;
        }

        public void HitShield(Transform projectile)
        {
            if(forcefield == null)
                return;

            if (localStats[Stat.shield] > 0)
                forcefield.HitCollider(projectile.position);
        }

        public override void DoDamage(float damage)
        {
            playerUnitDamaged?.Invoke(this);
            base.DoDamage(damage);
        }

        protected override void Die()
        {
            MessagePanel.ShowMessage($"{unitType.ToNiceString()} was destroyed", this.gameObject);
            base.Die();
            FindObjectOfType<UnitSelectionManager>().ClearSelection();
            this.gameObject.SetActive(false);
            if(unitType != PlayerUnitType.buildingSpot && unitType != PlayerUnitType.cargoShuttle)
                ReputationManager.ChangeReputation(-(int)GetStat(Stat.reputation));
        }

        /// <summary>
        /// to be called when the player deletes the unit
        /// </summary>
        public void DeleteUnit()
        {
            MessagePanel.ShowMessage($"{unitType.ToNiceString()} was removed.", this.gameObject);
            List<ResourceAmount> unitCost = FindObjectOfType<UnitManager>().GetUnitCost(unitType);

            float healthPercent = GetHP() / GetStat(Stat.hitPoints);

            FindObjectOfType<CargoManager>().PlaceCubes(unitCost, this.transform.position, healthPercent);
            FindObjectOfType<UnitSelectionManager>().ClearSelection();
            ReputationManager.ChangeReputation(-(int)GetStat(Stat.reputation));
            this.gameObject.SetActive(false);
        }

        public override float GetStat(Stat statType)
        {
            return stats.GetStat(statType) + this.GetBoost(statType);
        }

        private float GetBoost(Stat statType)
        {
            if (statBoosts.Count == 0)
                return 0;

            float boost = 0;
            foreach (var statboost in statBoosts)
            {
                if (statboost.stat == statType)
                    boost += statboost.Boost(this.gameObject);
            }
            return boost;
        }

        private Color GetStatColor(Stat statType)
        {
            float boost = GetBoost(statType);
            if (boost > 0)
                return ColorManager.GetColor(ColorCode.green);
            else if (boost < 0)
                return ColorManager.GetColor(ColorCode.red);
            else
                return Color.white;
        }

        public List<PopUpInfo> GetPopupInfo()
        {
            List<PopUpInfo> info = new List<PopUpInfo>();
            info.Add(new PopUpInfo(unitType.ToNiceString(), -1000, PopUpInfo.PopUpInfoType.name, (int)unitType));
            return info;
        }

        private string GetStatStringColor(string statString, Stat stat)
        {
            int colorCode = (int)GetBoost(stat);
            if (colorCode > 0)
                return TMPHelper.Color(statString, Color.green);
            else if (colorCode < 0)
                return TMPHelper.Color(statString, Color.red);
            else
                return statString;
        }

        public List<PopUpPriorityButton> GetPopUpButtons()
        {
            return new List<PopUpPriorityButton>()
            {
                new PopUpPriorityButton("Destroy", () => DestroyUnit(), 1000, true),
                //new PopUpButton("On/Off", () => CanToggleOff(), -1000)
            };
        }

        private void DestroyUnit()
        {
            FindObjectOfType<UnitSelectionManager>().ClearSelection();
            DropResources();
            this.gameObject.SetActive(false);
        }

        private void DropResources()
        {
            
        }

        public PopUpCanToggle CanToggleOff()
        {
            return new PopUpCanToggle(() => ToggleIsFunctional(), IsFunctional());
        }

        protected override void ToggleIsFunctional()
        {
            isFunctional = !isFunctional;
            isActive = isFunctional;
            ToggleBehaviors();

            DisplayPowerWarning();
        }

        protected void DisplayPowerWarning()
        {
            if (!hasWarningIcon)
            {
                warningIconInstance = this.gameObject.GetComponentInChildren<WarningIcons>(false);

                if(!hasWarningIcon)
                {
                    warningIconInstance = UnitManager.warningIcons.PullGameObject(this.transform.position, Quaternion.identity).GetComponent<WarningIcons>();
                    warningIconInstance.transform.SetParent(this.transform,true);
                }
            }

            warningIconInstance.ToggleIsPowered(isActive);
        }

        public List<PopUpStats> GetPopUpStats()
        {
            List<PopUpStats> popUpStats = new List<PopUpStats>( new PopUpStats[stats.instanceStats.Count + stats.stats.Count]);

            foreach (var stat in stats.instanceStats)
            {
                if (GetStat(stat.Key) == 0)
                    continue;

                popUpStats.Add(new PopUpStats(stat.Key, GetStat(stat.Key), 0, GetStatColor(stat.Key)));
            }

            foreach (var stat in stats.stats)
            {
                if (GetStat(stat.Key) == 0)
                    continue; 
                popUpStats.Add(new PopUpStats(stat.Key, GetStat(stat.Key), 0, GetStatColor(stat.Key)));
            }

            return popUpStats;
        }
    }
}

