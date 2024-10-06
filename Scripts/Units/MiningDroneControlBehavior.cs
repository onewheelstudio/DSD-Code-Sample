using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class MiningDroneControlBehavior : UnitBehavior
{
    [SerializeField] private HexTileType requiredTileType;  
    private ResourceProductionBehavior resourceProductionBehavior;
    private Drone[] miningDrones;
    private WaitForSeconds miningDelay = new WaitForSeconds(0.25f);

    private void OnEnable()
    {
        resourceProductionBehavior = this.GetComponentInParent<ResourceProductionBehavior>();
        if(resourceProductionBehavior != null)
            resourceProductionBehavior.recipeChanged += RecipeChanged;
    }

    private void OnDisable()
    {
        resourceProductionBehavior.recipeChanged -= RecipeChanged;
    }

    public override void StartBehavior()
    {
        this.isFunctional = true;
        miningDrones = this.GetComponentsInChildren<Drone>();
        StartCoroutine(DoDroneMovement());
    }

    public override void StopBehavior()
    {
       this.isFunctional = false;
    }

    IEnumerator DoDroneMovement()
    {
        if(miningDrones == null)
            yield break;

        //wait until we can produce at least on start up
        yield return new WaitUntil(() => resourceProductionBehavior.CanIProduce());

        while(true)
        {
            foreach (var drone in miningDrones)
            {
                if(drone.IsMining)
                {
                    yield return null;
                    continue;
                }

                yield return miningDelay;

                if(GetNearbyTile(this.gameObject, requiredTileType, out Hex3 location))
                    drone.MoveToLocation(location);

                if(!this.isFunctional)
                {
                    drone.DoReturnToStart();
                    yield break;
                }
            }

            if(!resourceProductionBehavior.CanIProduce())
            {
                foreach (var drone in miningDrones)
                {
                    drone.DoReturnToStart();
                }

                yield return new WaitUntil(() => resourceProductionBehavior.CanIProduce());
            }
        }
    }

    public bool GetNearbyTile(GameObject gameObject, HexTileType requiredTileType, out Hex3 location)
    {      
        IOrderedEnumerable<Hex3> neighbors = Hex3.GetNeighborsAtDistance(gameObject.transform.position, 1)
                                                .OrderBy(x => Guid.NewGuid());
        HexTile tile;
        foreach (var hex3 in neighbors)
        {
            tile = HexTileManager.GetHexTileAtLocation(hex3);
            if (tile != null && tile.TileType == requiredTileType)
            {
                location = hex3;
                return true;
            }
        }

        location = new Hex3();
        return false;
    }

    private void RecipeChanged(ResourceProduction production)
    {
        foreach (var useCondition in production.useConditions)
        {
            if (useCondition is UseNearTile useNearTile)
            {
                requiredTileType = useNearTile.RequiredTileType;
                return;
            }
        }
    }

}
