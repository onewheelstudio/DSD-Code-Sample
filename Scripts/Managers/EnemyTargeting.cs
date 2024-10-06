using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;
using Sirenix.OdinInspector;
using HexGame.Units;

public class EnemyTargeting : MonoBehaviour
{
    [ShowInInspector]
    private static List<Unit> playerUnits = new List<Unit>();
    [ShowInInspector]
    private static List<PlayerUnitTarget> playerUnitTargets = new List<PlayerUnitTarget>();
    [SerializeField] private List<PlayerUnitType> typesToIgnore = new List<PlayerUnitType>() { PlayerUnitType.landMine };
    private static Seeker seeker;
    [ShowInInspector]
    private static Transform _target;

    private void OnEnable()
    {
        Unit.unitCreated += AddUnit;
        Unit.unitRemoved += RemoveUnit;

        if (seeker == null)
            seeker = this.gameObject.AddComponent<Seeker>();

        GetAllPlayerUnits();
    }

    private void OnDisable()
    {
        Unit.unitCreated -= AddUnit;
        Unit.unitRemoved -= RemoveUnit;
    }

    public static Transform GetNearestTarget(Unit enemy)
    {
        CleanUpPlayerUnitList();

        if (playerUnits.Count == 0)
            return null;

        float dist = Mathf.Infinity;
        Transform target = null;

        foreach (var playerUnit in playerUnits)
        {
            //if (playerUnit.GetComponent<TowerBehavior>() == null)
            //continue;

            float distance = (enemy.transform.position - playerUnit.transform.position).sqrMagnitude;
            if (distance < dist)
            {
                dist = distance;
                target = playerUnit.transform;
            }
            _target = target;
        }
        return target;
    }

    /// <summary>
    /// Uses the target value to determine the best target
    /// </summary>
    /// <param name="enemy"></param>
    /// <returns></returns>
    public static PlayerUnit GetHighestValueTarget(Vector3 startingPoint, float minRange = 0f)
    {
        CleanUpPlayerUnitList();
        if (playerUnits.Count == 0)
            return null;

        foreach (var target in playerUnitTargets)
            target.SetTargetValue(startingPoint);

        if(minRange > 0)
        {
            PlayerUnitTarget target = playerUnitTargets.Where(o => (o.target.transform.position - startingPoint).sqrMagnitude > minRange * minRange)
                                                       .OrderByDescending(o => o.targetValue)
                                                       .FirstOrDefault();

            if (target != null)
                return target.target;
            else
                return null;
        }
        else 
            return playerUnitTargets.OrderByDescending(o => o.targetValue).First().target;
    }

    private void GetAllPlayerUnits()
    {
        foreach (var p in FindObjectsOfType<PlayerUnit>().ToList())
        {
            if (p.unitType == PlayerUnitType.landMine)
                continue;

            AddUnit(p);
        }
    }

    private void AddUnit(Unit unit)
    {
        if (unit is PlayerUnit)
        {
            if (((PlayerUnit)unit).unitType == PlayerUnitType.landMine)
                return;

            playerUnits.Add(unit);
            playerUnitTargets.Add(new PlayerUnitTarget(unit as PlayerUnit));
        }
    }

    private void RemoveUnit(Unit unit)
    {
        if (unit is PlayerUnit)
        {
            playerUnits.Remove(unit);

            foreach (PlayerUnitTarget playerUnitTarget in playerUnitTargets)
            {
                if (playerUnitTarget.target == unit)
                {
                    playerUnitTargets.Remove(playerUnitTarget);
                    return;
                }
            }
        }
    }

    private static void CleanUpPlayerUnitList()
    {
        for(int i = playerUnits.Count - 1; i >= 0; i--)
        {
            if (playerUnits[i] == null || playerUnits[i].gameObject.activeSelf == false)
                playerUnits.RemoveAt(i);
        }

        for (int i = playerUnitTargets.Count - 1; i >= 0; i--)
        {
            if (playerUnitTargets[i].target == null || playerUnitTargets[i].target.gameObject.activeSelf == false)
                playerUnitTargets.RemoveAt(i);
        }
    }

    public void CheckPath(EnemyUnit enemy, Vector3 targetLocation)
    {
        //seeker = GetComponent<Seeker>();
        //seeker.StartPath(enemy.transform.position, targetLocation, OnPathComplete);
    }

    //IEnumerator Start()
    //{
    //    var path = seeker.StartPath(transform.position, transform.position + transform.forward * 10, OnPathComplete);
    //    // Wait... (may take some time depending on how complex the path is)
    //    // The rest of the game will continue to run while waiting
    //    yield return StartCoroutine(path.WaitForPath());
    //    // The path is calculated now
    //}

    [System.Serializable]
    private class PlayerUnitTarget
    {
        public PlayerUnitTarget(PlayerUnit playerUnit)
        {
            this.target = playerUnit;
            this.unitType = playerUnit.unitType;
        }

        public PlayerUnit target;
        public PlayerUnitType unitType;
        public float targetValue = 1f;

        public float SetTargetValue(Vector3 attackerPosition)
        {
            if (target == null) //handles target being destroyed?
                return 0f;

            switch (unitType)
            {
                case PlayerUnitType.singleTower:
                    targetValue = 2f;
                    break;
                case PlayerUnitType.cargoShuttle:
                    targetValue = 0;
                    break;
                case PlayerUnitType.collectionTower:
                    targetValue = 3f;
                    break;
                default:
                    targetValue = 1f;
                    break;
            }

            float distance = (target.transform.position - attackerPosition).sqrMagnitude;
            targetValue /= distance;

            return targetValue;
        }
    }

}
