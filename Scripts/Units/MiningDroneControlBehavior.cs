using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MiningDroneControlBehavior : UnitBehavior
{
    [SerializeField] private List<HexTileType> requiredTileTypes;  
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
        DoDroneMovement();
    }

    public override void StopBehavior()
    {
       this.isFunctional = false;
    }

    private async void DoDroneMovement()
    {
        if(miningDrones == null)
            return;

        //wait until we can produce at least on start up
        while (resourceProductionBehavior != null && !resourceProductionBehavior.CanIProduceFast())
            await Awaitable.NextFrameAsync();

        while (true && !this.destroyCancellationToken.IsCancellationRequested)
        {
            foreach (var drone in miningDrones)
            {
                if(this.destroyCancellationToken.IsCancellationRequested)
                    return;

                if(drone.IsDoing)
                {
                    await Awaitable.NextFrameAsync();
                    continue;
                }

                if (this.destroyCancellationToken.IsCancellationRequested)
                    return; 
                await Awaitable.WaitForSecondsAsync(0.25f);

                if(GetNearbyTile(this.gameObject, requiredTileTypes, out Hex3 location))
                    drone.MoveToLocation(location);

                if(!this.isFunctional)
                {
                    drone.DoReturnToPosition();
                    return;
                }
            }

            if (resourceProductionBehavior == null)
                return;

            if(!resourceProductionBehavior.CanIProduceFast())
            {
                foreach (var drone in miningDrones)
                {
                    drone.DoReturnToPosition();
                }

                if (this.destroyCancellationToken.IsCancellationRequested)
                    return;

                while (resourceProductionBehavior != null && !resourceProductionBehavior.CanIProduceFast())
                    await Awaitable.NextFrameAsync();
            }
        }
    }

    public bool GetNearbyTile(GameObject gameObject, List<HexTileType> requiredTileTypes, out Hex3 location)
    {      
        IOrderedEnumerable<Hex3> neighbors = Hex3.GetNeighborsAtDistance(gameObject.transform.position, 1)
                                                .OrderBy(x => Guid.NewGuid());
        HexTile tile;
        foreach (var hex3 in neighbors)
        {
            tile = HexTileManager.GetHexTileAtLocation(hex3);
            if (tile != null && requiredTileTypes.Contains(tile.TileType))
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
                //create new list so we don't modify the original
                requiredTileTypes = new List<HexTileType>(useNearTile.RequiredTiles);
                return;
            }
        }
    }

}
