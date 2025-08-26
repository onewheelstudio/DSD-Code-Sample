using UnityEngine;
using System;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using HexGame.Grid;

namespace HexGame.Units
{
    public abstract class Unit : SerializedMonoBehaviour, IPlaceable
    {
        public static event Action<Unit> unitCreated;
        public static event Action<Unit> unitRemoved;
        public event Action<Unit, float> unitDamaged;
        public event Action<Unit, float> unitRepaired;
        [SerializeField]
        [BoxGroup("Stats")]
        [InlineEditor(Expanded = true)]
        protected Stats stats;
        protected bool isPlaced = false;
        protected bool isFunctional = false;
        [SerializeField]protected bool invuneralble = false;
        protected Dictionary<Stat, float> localStats = new Dictionary<Stat, float>();
        protected static ParticleManager particleManager;

        private void Awake()
        {
            if (particleManager == null)
                particleManager = FindObjectOfType<ParticleManager>();
        }

        protected virtual void OnEnable()
        {
            Intialize();
            if (localStats.TryGetValue(Stat.shield, out float shield))
                DayNightManager.toggleDay += RenewShields;
        }

        protected virtual void OnDisable()
        {
            if(GameStateManager.LeavingScene)
                return;

            Remove();
            ToggleBehaviorsOff();
            unitRemoved?.Invoke(this);
            DayNightManager.toggleDay -= RenewShields;
        }



        public virtual void Intialize()
        {
            //clone the dictionary from the stats SO
            unitCreated?.Invoke(this);
            if(!SaveLoadManager.Loading || this is EnemyUnit)
                localStats = new Dictionary<Stat, float>(stats.instanceStats);
        }

        public virtual void RestoreHP(float amount)
        {
            localStats[Stat.hitPoints] += amount;
            if (localStats[Stat.hitPoints] > stats[Stat.hitPoints])
                localStats[Stat.hitPoints] = stats[Stat.hitPoints];

            unitRepaired?.Invoke(this, amount);
        }

        public float GetHP()
        {
            if (localStats.TryGetValue(Stat.hitPoints, out float hitPoints))
                return hitPoints;
            else
            {
                Debug.LogError($"No HP found for {this.gameObject.name}", this.gameObject);
                return 0;
            }
        }
        
        public float GetShield()
        {
            if (localStats.TryGetValue(Stat.shield, out float shield))
                return shield;
            else
                return 0;
        }
        protected void RenewShields(int obj)
        {
            if(localStats == null)
                return;

            if(localStats.TryGetValue(Stat.shield, out float shield) && this.transform != null)
            {
                if(shield < stats[Stat.shield])
                    particleManager.GetRechargeParticles(this.transform.position + Vector3.up * 0.1f);
                localStats[Stat.shield] = stats[Stat.shield];
            }
        }

        [Button]
        public virtual void DoDamage(float damage)
        {
            if(invuneralble)
                return;

            //float armorModification = GetStat(Stat.armor) <= 0 ? 1f : Mathf.Pow(0.99f, GetStat(Stat.armor));
            if(localStats.TryGetValue(Stat.shield, out float currentShield))
            {
                float shieldModification = MathF.Min(damage, currentShield);
                localStats[Stat.shield] = currentShield - shieldModification;
                damage -= shieldModification;
            }

            float armorModification = GetStat(Stat.armor) <= 0 ? 1f : Mathf.Pow(0.99f, GetStat(Stat.armor));
            //damage is local so change local stats
            if (localStats.TryGetValue(Stat.hitPoints, out float hitPoints))
            {
                localStats[Stat.hitPoints] = hitPoints - damage * armorModification;
                unitDamaged?.Invoke(this, hitPoints - damage * armorModification);
                CheckForDead();
            }
            else
                Debug.LogError($"Trying to damage {gameObject.name} and there is no instance stat for Hit Points");
        }

        protected virtual void CheckForDead()
        {
            if (localStats[Stat.hitPoints] <= 0)
                Die();
        }

        protected virtual void Die()
        {
            stats.DoDeathParticles(this.transform.position);
            this.gameObject.SetActive(false);
        }

        public virtual void Place()
        {
            isPlaced = true;
            isFunctional = true;
            ToggleBehaviorsOn();
        }

        public void Remove()
        {
            isPlaced = false;
        }

        public bool IsPlaced()
        {
            return isPlaced;
        }

        public bool IsFunctional()
        {
            return isFunctional && isPlaced;
        }

        protected void ToggleBehaviors()
        {
            foreach (var behavior in this.GetComponentsInChildren<UnitBehavior>())
            {
                //don't disable shuttles mid-flight :)
                if (behavior is CargoShuttleBehavior)
                    continue;

                if (isPlaced && isFunctional)
                    behavior.StartBehavior();
                else
                    behavior.StopBehavior();
            }
        }

        protected void ToggleBehaviorsOn()
        {
            //foreach (var behavior in this.GetComponents<UnitBehavior>())
            //    behavior.StartBehavior();
            
            foreach (var behavior in this.GetComponentsInChildren<UnitBehavior>(true))
                behavior.StartBehavior();
        }

        protected void ToggleBehaviorsOff()
        {
            //    foreach (var behavior in this.GetComponents<UnitBehavior>())
            //        behavior.StopBehavior();

            foreach (var behavior in this.GetComponentsInChildren<UnitBehavior>())
                behavior.StopBehavior();
        }

        protected virtual void ToggleIsFunctional()
        {
            isFunctional = !isFunctional;
            ToggleBehaviors();
        }

        public virtual float GetStat(Stat stat)
        {
            return stats.GetStat(stat);
        }

        public bool HasStat(Stat stat)
        {
            return stats.HasStat(stat);
        }

        public float GetLocalStat(Stat stat)
        {
            return localStats[stat];
        }

        public Stats GetStats()
        {
            return stats;
        }

        public virtual Transform GetTarget()
        {
            return this.transform;
        }

        public bool PlacementListContains(Resources.HexTileType hexTileType)
        {
            if (stats.placementList.Count == 0)
                return false;

            return stats.placementList.Contains(hexTileType);
        }
    }

    public enum PlayerUnitType
    {
        hq = 0,
        doubleTower = 1,
        artillery = 2,
        tank = 3,
        sandPit = 4,
        housing = 5,
        farm = 6,
        mine = 7,
        scoutTower = 8,
        gasRefinery = 9,
        smelter = 10,
        repair = 11,
        spaceLaser = 12,
        storage = 13,
        powerPlant = 14,
        solarPanel = 15,
        cargoShuttle = 16,
        shuttlebase = 17,
        spaceElevator = 18,
        buildingSpot = 19,
        missileTower = 20,
        missileSilo = 21,
        bomberBase = 22,
        infantry = 23,
        singleTower = 24,
        barracks = 25,
        waterPump = 26,
        oilPlatform = 27,
        factory = 28,
        landMine = 29,
        supplyShip = 30,
        collectionTower = 31,
        deepMine = 32,
        atmosphericCondenser = 33,
        chemicalPlant = 34,
        pub = 35,
        foundry = 36,
        nuclearPlant = 37,
        centrifuge = 38,
        biomassHarvester = 39,
        bioReactor = 40,
        orbitalBarge = 41,
        resourcePile = 42,
        transportHub = 43,
    }


    public enum EnemyUnitType
    {
        serpent,
        flying,
        structure,
        serpentElite,
        rangedTank,
    }
}

