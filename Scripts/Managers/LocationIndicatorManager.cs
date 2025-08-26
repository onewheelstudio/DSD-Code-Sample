using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class LocationIndicatorManager : MonoBehaviour
{
    private static ObjectPool<HexIndicator> indicatorPool;
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField, Range(0f, 0.25f)] private float IndicatorOffset = 0.1f;
    [SerializeField] private Material blueIndicator;
    [SerializeField] private Material yellowIndicator;
    [SerializeField] private Material redIndicator;
    [SerializeField] private List<HexIndicator> indicatorList;
    private Func<List<IndicatorInfo>> currentIndicator;

    [SerializeField] private Camera playerCamera;

    private void Awake()
    {
        indicatorPool = new ObjectPool<HexIndicator>(indicatorPrefab);    
    }

    private void OnEnable()
    {
        UnitManager.unitPlaced += UpdateIndicators;
        MarineBehavior.maringMovingToLocation += BehaviorMoving;
        FogGroundTile.TileRevealed += TileRevealed;
        UnitManager.unitPlacementStarted += UnitPlacementStarted;
        UnitManager.unitPlacementFinished += HideIndicators;
        UnLockTechTree.unLockTechTree += CompleteTutorial;
        SaveLoadManager.LoadComplete += CompleteTutorial; //turn off this manager if loading a game.
        //FogGroundTile.TileHidden += TileHidden;

    }

    private void OnDisable()
    {
        UnitManager.unitPlaced -= UpdateIndicators;
        MarineBehavior.maringMovingToLocation -= BehaviorMoving;
        FogGroundTile.TileRevealed -= TileRevealed;
        UnitManager.unitPlacementStarted -= UnitPlacementStarted;
        UnitManager.unitPlacementFinished -= HideIndicators;
        UnLockTechTree.unLockTechTree -= CompleteTutorial;
        SaveLoadManager.LoadComplete -= CompleteTutorial; //turn off this manager if loading a game.

        //FogGroundTile.TileHidden -= TileHidden;
    }

    private void CompleteTutorial()
    {
        UnitManager.unitPlaced -= UpdateIndicators;
        MarineBehavior.maringMovingToLocation -= BehaviorMoving;
        FogGroundTile.TileRevealed -= TileRevealed;
        UnitManager.unitPlacementStarted -= UnitPlacementStarted;
        UnitManager.unitPlacementFinished -= HideIndicators;
        UnLockTechTree.unLockTechTree -= CompleteTutorial;
    }

    private void TileHidden(FogGroundTile tile)
    {
        Hex3 location = tile.transform.position.ToHex3();
        HexIndicator indicator = indicatorList.FirstOrDefault(l => l.transform.position.ToHex3() == location);
        indicator?.PopOut();
    }

    private void TileRevealed(FogGroundTile tile)
    {
        if (currentIndicator == null)
            return;

        Hex3 location = tile.transform.position.ToHex3();
        List<IndicatorInfo> currentLocation = currentIndicator?.Invoke();

        if(currentLocation.Any(l => l.location == location))
        {
            HexIndicator indicator = indicatorPool.Pull();
            indicator.transform.position = (Vector3)location + Vector3.up * IndicatorOffset;
            indicator.MeshRenderer.sharedMaterial = blueIndicator;
            indicatorList.Add(indicator);
        }
    }

    private void BehaviorMoving(UnitBehavior behavior, Hex3 location)
    {
        if (indicatorList.Count == 0)
            return;

        HexIndicator indicator = indicatorList.FirstOrDefault(i => i.transform.position.ToHex3() == behavior.transform.position.ToHex3());
        if (indicator != null)
            indicator.MeshRenderer.material = blueIndicator;


        indicator = indicatorList.FirstOrDefault(i => i.transform.position.ToHex3() == location);
        if (indicator != null)
            indicator.MeshRenderer.material = redIndicator;
    }

    private void UpdateIndicators(Unit unit)
    {
        if (indicatorList.Count == 0)
            return;

        HexIndicator indicator = indicatorList.FirstOrDefault(i => i.transform.position.ToHex3() == unit.transform.position.ToHex3());
        if (indicator == null)
            return;

        indicator.MeshRenderer.material = redIndicator;
    }

    [Button]
    private void PlaceIndicators(List<Hex3> locations)
    {
        //indicatorMaterial.SetColor("_BaseColor", color);

        foreach (Hex3 location in locations)
        {
            HexIndicator indicator = indicatorPool.Pull();
            indicator.transform.position = (Vector3)location + Vector3.up * IndicatorOffset;
            indicator.MeshRenderer.material = blueIndicator;
            indicatorList.Add(indicator);
        }
    }

    [Button]
    private void HideIndicators()
    {
        foreach (var indicator in indicatorList)
        {
            indicator.PopOut();
        }

        indicatorList.Clear();
        currentIndicator = null;
    }

    private void UnitPlacementStarted(PlayerUnitType type)
    {
        switch(type)
        {
            case PlayerUnitType.mine:
                AddIronMineIndicators();
                break;
            case PlayerUnitType.waterPump:
                AddWaterPumpIndicators();
                break;
            case PlayerUnitType.supplyShip:
            case PlayerUnitType.housing:
            case PlayerUnitType.farm:
            case PlayerUnitType.hq:
            case PlayerUnitType.solarPanel:
                ShowPossibleGrassLocations();
                break;
            case PlayerUnitType.collectionTower:
                ShowBuildableNearTerrene();
                break;
        }
    }



    [Button]
    public void AddIronMineIndicators()
    {
        HideIndicators();
        List<IndicatorInfo> indicatorInfo = GetGrassNearStartingIronOre();
        currentIndicator = GetGrassNearStartingIronOre;

        foreach (var info in indicatorInfo)
        {
            HexIndicator indicator = indicatorPool.Pull();
            indicator.transform.position = (Vector3)info.location + Vector3.up * IndicatorOffset;
            indicator.MeshRenderer.sharedMaterial = info.material;
            indicatorList.Add(indicator);
        }
    }

    [Button]
    public void AddWaterPumpIndicators()
    {
        HideIndicators();
        List<IndicatorInfo> indicatorInfo = GetGrassNearStartingWater();
        currentIndicator = GetGrassNearStartingWater;

        foreach (var info in indicatorInfo)
        {
            HexIndicator indicator = indicatorPool.Pull();
            indicator.transform.position = (Vector3)info.location + Vector3.up * IndicatorOffset;
            indicator.MeshRenderer.sharedMaterial = info.material;
            indicatorList.Add(indicator);
        }
    }

    [Button]
    private void ShowBuildableNearTerrene()
    {
        HideIndicators();
        List<IndicatorInfo> indicatorInfo = GetBuildableNearTerrene();
        currentIndicator = GetGrassNearStartingWater;

        if(currentIndicator == null || indicatorInfo == null || indicatorInfo.Count == 0)
        {
            Debug.LogError("No buildable locations found");
            return;
        }

        foreach (var info in indicatorInfo)
        {
            HexIndicator indicator = indicatorPool.Pull();
            indicator.transform.position = (Vector3)info.location + Vector3.up * IndicatorOffset;
            indicator.MeshRenderer.sharedMaterial = info.material;
            indicatorList.Add(indicator);
        }
    }

    [Button] 
    private void ShowPossibleGrassLocations()
    {
        HideIndicators();
        List<IndicatorInfo> indicatorInfo = GetAllGrass();
        currentIndicator = GetAllGrass;

        foreach (var info in indicatorInfo)
        {
            HexIndicator indicator = indicatorPool.Pull();
            indicator.transform.position = (Vector3)info.location + Vector3.up * IndicatorOffset;
            indicator.MeshRenderer.sharedMaterial = info.material;
            indicatorList.Add(indicator);
        }
    }

    private List<IndicatorInfo> GetGrassNearStartingIronOre()
    {
        List<HexTile> oretiles = HexTileManager.GetAllTilesOfType(HexTileType.feOre, GetVisibleLocations());
        List<IndicatorInfo> grassLocation = new List<IndicatorInfo>();
        for(int i = oretiles.Count - 1; i >= 0; i--)
        {
            if (HelperFunctions.HexRangeFloat(oretiles[i].hexPosition, Hex3.Zero) > 5)
                continue;
            
            foreach(Hex3 location in Hex3.GetNeighborLocations(oretiles[i].hexPosition))
            {
                if (grassLocation.Any(g => g.location == location))
                    continue;

                if (HexTileManager.GetHexTileAtLocation(location)?.TileType != HexTileType.grass)
                    continue;

                //exclude unrevealed locations
                if (HexTileManager.NumberOfRevealersAtLocation(location) == 0)
                    continue;

                if (UnitManager.PlayerUnitAtLocation(location) != null)
                {
                    IndicatorInfo indicatorInfo = new IndicatorInfo();
                    indicatorInfo.location = location;
                    indicatorInfo.material = redIndicator;
                    grassLocation.Add(indicatorInfo);
                }
                else
                {
                    IndicatorInfo indicatorInfo = new IndicatorInfo();
                    indicatorInfo.location = location;
                    indicatorInfo.material = blueIndicator;
                    grassLocation.Add(indicatorInfo);
                }
            }
        }

        return grassLocation;
    }

    private List<IndicatorInfo> GetGrassNearStartingWater()
    {
        List<HexTile> waterTiles = HexTileManager.GetAllTilesOfType(HexTileType.water, GetVisibleLocations());
        List<IndicatorInfo> grassLocation = new List<IndicatorInfo>();
        for (int i = waterTiles.Count - 1; i >= 0; i--)
        {
            if (HelperFunctions.HexRangeFloat(waterTiles[i].hexPosition, Hex3.Zero) > 5)
                continue;

            foreach (Hex3 location in Hex3.GetNeighborLocations(waterTiles[i].hexPosition))
            {
                if (grassLocation.Any(g => g.location == location))
                    continue;

                if (HexTileManager.GetHexTileAtLocation(location) == null)
                    continue;

                if (HexTileManager.GetHexTileAtLocation(location).TileType != HexTileType.grass)
                    continue;

                //exclude unrevealed locations
                if (HexTileManager.NumberOfRevealersAtLocation(location) == 0)
                    continue;

                if (UnitManager.PlayerUnitAtLocation(location) != null)
                {
                    IndicatorInfo indicatorInfo = new IndicatorInfo();
                    indicatorInfo.location = location;
                    indicatorInfo.material = redIndicator;
                    grassLocation.Add(indicatorInfo);
                }
                else
                {
                    IndicatorInfo indicatorInfo = new IndicatorInfo();
                    indicatorInfo.location = location;
                    indicatorInfo.material = blueIndicator;
                    grassLocation.Add(indicatorInfo);
                }
            }
        }

        return grassLocation;
    }

    private List<IndicatorInfo> GetAllGrass()
    {
        //List<HexTile> possibleGrassLocations = HexTileManager.GetAllTilesOfType(HexTileType.grass);
        List<HexTile> possibleGrassLocations = HexTileManager.GetAllTilesOfType(HexTileType.grass, GetVisibleLocations());
        List<IndicatorInfo> grassLocation = new List<IndicatorInfo>();
        for (int i = possibleGrassLocations.Count - 1; i >= 0; i--)
        {

            //exclude unrevealed locations
            if (HexTileManager.NumberOfRevealersAtLocation(possibleGrassLocations[i].hexPosition) == 0)
                continue;

            if (UnitManager.PlayerUnitAtLocation(possibleGrassLocations[i].hexPosition) != null)
            {
                IndicatorInfo indicatorInfo = new IndicatorInfo();
                indicatorInfo.location = possibleGrassLocations[i].hexPosition;
                indicatorInfo.material = redIndicator;
                grassLocation.Add(indicatorInfo);
            }
            else
            {
                IndicatorInfo indicatorInfo = new IndicatorInfo();
                indicatorInfo.location = possibleGrassLocations[i].hexPosition;
                indicatorInfo.material = blueIndicator;
                grassLocation.Add(indicatorInfo);
            }
        }

        return grassLocation;
    }

    private List<IndicatorInfo> GetBuildableNearTerrene()
    {
        ResourcePickup[] resources = FindObjectsOfType<ResourcePickup>();
        Hex3 closestLocation = Hex3.Zero;
        int distance = int.MaxValue;
        foreach (var resource in resources)
        {
            if (resource.resourceType != ResourceType.Terrene)
                continue;

            if(resource.transform.position.ToHex3().Min() < distance)
            {
                distance = resource.transform.position.ToHex3().Min();
                closestLocation = resource.transform.position.ToHex3();
            }
        }

        if (closestLocation == Hex3.Zero)
            return null;

        Hex3 hqLocation = UnitManager.GetPlayerUnitByType(PlayerUnitType.hq)[0].transform.position.ToHex3();
        List<Hex3> buildableLocations = Hex3.GetNeighborsInRange(hqLocation, 5);
        List<IndicatorInfo> indicatorInfos = new List<IndicatorInfo>();

        //remove locations that don't have tiles or tiles of the wrong type
        for (int i = buildableLocations.Count - 1; i >= 0; i--)
        {
            HexTile tile = HexTileManager.GetHexTileAtLocation(buildableLocations[i]);
            if (tile == null)
            {
                buildableLocations.RemoveAt(i);
                continue;
            }

            if (tile.TileType != HexTileType.grass && tile.TileType != HexTileType.forest && tile.TileType != HexTileType.aspen)
            {
                buildableLocations.RemoveAt(i);
                continue;
            }

            if(Hex3.DistanceBetween(buildableLocations[i], closestLocation) > 3 )
            {
                buildableLocations.RemoveAt(i);
                continue;
            }

            //made it past the all the checks
            IndicatorInfo indicatorInfo = new IndicatorInfo();
            indicatorInfo.location = buildableLocations[i];

            if (UnitManager.PlayerUnitAtLocation(buildableLocations[i]) != null)
                indicatorInfo.material = redIndicator;
            else 
                indicatorInfo.material = blueIndicator;

            indicatorInfos.Add(indicatorInfo);
        }

        return indicatorInfos;
    }

    [Button]
    private void PlaceIndicatorsAllVisibleLocations()
    {
        List<Hex3> locations = GetVisibleLocations();
        PlaceIndicators(locations);
    }

    private List<Hex3> GetVisibleLocations()
    {
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        
        //get center point
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Vector3 center = Vector3.zero;
        if (plane.Raycast(ray, out float distance))
            center = ray.GetPoint(distance);

        //get corner point
        ray = playerCamera.ScreenPointToRay(new Vector2(0f, playerCamera.pixelHeight));
        Vector3 topLeftCorner = Vector3.zero;
        if (plane.Raycast(ray, out distance))
            topLeftCorner = ray.GetPoint(distance);

        distance = Vector3.Distance(center, topLeftCorner) / Hex3.SQRT3;

        return Hex3.GetNeighborsInRange(center.ToHex3(), Mathf.CeilToInt(distance));
    }

    public class IndicatorInfo
    {
        public Hex3 location;
        public Material material;
    }
}
