using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Directives/Develop Resource Directive")]
public class DevelopResourceDirective : DirectiveQuest
{
    [Header("Develop Resource Bits")]
    [SerializeField] private ResourceType resourceType;
    [SerializeField] private int amountToCollect;
    [NonSerialized] private int amountCollected;

    [SerializeField] private BuildingRequirement requiredBuilding;
    [SerializeField] private FogRevealer fogRevealerPrefab;
    [SerializeField] private GameObject markerPrefab;
    [NonSerialized] private List<GameObject> markerInstances = new();
    [NonSerialized] private List<ResourceTile> resourceTiles = new();

    public static event Action<Vector3> MoveToLocationClicked;
    public override void Initialize()
    {
        base.Initialize();

        HexTile tileToReveal = FindFirstResourceTile(resourceType);
        if(tileToReveal == null)
            return;

        List<Hex3> resourcesPositions = new();
        GetAdjacentResources(tileToReveal.hexPosition, resourcesPositions);

        if(!resourcesPositions.Contains(tileToReveal.hexPosition))
            resourcesPositions.Add(tileToReveal.hexPosition);

        //clearing in the case of reinitialization
        resourceTiles.Clear();
        markerInstances.Clear();
        foreach (Hex3 hex3 in resourcesPositions)
        {
            PlaceFogRevealer(hex3);
            if(HexTileManager.GetHexTileAtLocation(hex3).TryGetComponent(out ResourceTile resourceTile))
            {
                resourceTiles.Add(resourceTile);
                resourceTile.resourceExtractedLocal += ResourceExtracted;
            }
        }

        if(resourceTiles.Count == 0)
        {
            Debug.Log($"No {resourceType.ToNiceString()} resource tiles - removing directive");
            FindFirstObjectByType<DirectiveMenu>().RemoveDirective(this);
            return;
        }

        PlayerUnit.unitCreated += UnitCreated;

        MessageData messageData = new MessageData();
        messageData.message = $"{resourceType.ToNiceString()} has been revealed.";
        messageData.messageObject = tileToReveal.gameObject;
        messageData.messageColor = ColorManager.GetColor(ColorCode.techCredit);
        MessagePanel.ShowMessage(messageData).SetDisplayTime(20);

        lastUsedTime = Time.realtimeSinceStartup;
    }

    private void UnitCreated(Unit unit)
    {
        if (unit == null || requiredBuilding == null)
            return;

        if(unit is PlayerUnit playerUnit && playerUnit.unitType == requiredBuilding.unitType)
        {
            if(!UnitAtOrNearResource(playerUnit))
                return;

            requiredBuilding.numberBuilt++;
            if(requiredBuilding.numberBuilt >= requiredBuilding.totalToBuild)
                requiredBuilding.numberBuilt = requiredBuilding.totalToBuild;
            else
                DirectiveUpdated();

            foreach (var marker in markerInstances)
            {
                if (marker == null)
                    continue;
                marker.SetActive(false);
            }
        }
    }

    private bool UnitAtOrNearResource(PlayerUnit playerUnit)
    {
        Hex3 unitLocation = playerUnit.transform.position.ToHex3();
        foreach (var resourceTile in resourceTiles)
        {
            if (resourceTile.Location == unitLocation)
                return true;

            if(resourceTile.Location.DistanceTo(unitLocation) < 2)
                return true;
        }

        return false;
    }

    private void ResourceExtracted(ResourceType type, ResourceTile tile)
    {
        amountCollected++;
        if(amountCollected > amountToCollect)
            amountCollected = amountToCollect;
        else
            DirectiveUpdated();
    }

    public override bool CanBeAssigned()
    {
        HexTileType HexTileType = GetTileTypeForResource(resourceType);
        List<HexTile> tiles = HexTileManager.GetAllRevealedTilesOfTYpe(HexTileType);

        foreach (HexTile tile in tiles)
        {
            if (tile.hexPosition.Max() < 5)
                continue;

            List<Hex3> neighbors = Hex3.GetNeighborLocations(tile.hexPosition);
            neighbors.Add(tile.hexPosition);
            List<PlayerUnit> units = new List<PlayerUnit>();
            foreach (Hex3 neighbor in neighbors)
            {
                units.Add(UnitManager.PlayerUnitAtLocation(neighbor));
            }

            //if a required type of building is present then we should NOT assign this directive
            foreach (var unit in units)
            {
                if (unit == null)
                    continue;

                if (requiredBuilding.unitType == unit.unitType)
                    return false;
            }
        }
        return true;
    }

    private HexTile FindFirstResourceTile(ResourceType resourceType)
    {
        HexTileType HexTileType = GetTileTypeForResource(resourceType);
        List<HexTile> tiles = HexTileManager.GetAllTilesOfType(HexTileType);
        tiles = tiles.OrderBy(tiles => tiles.hexPosition.Max()).ToList();

        foreach (HexTile tile in tiles)
        {
            if (tile.hexPosition.Max() < 5)
                continue;
            return tile;
        }

        return null;
    }
    private void PlaceFogRevealer(Hex3 hexPosition)
    {
        WorldController worldController = FindFirstObjectByType<WorldController>();
        FogRevealer fogRevealer = Instantiate(fogRevealerPrefab, hexPosition, Quaternion.identity, worldController.transform);
        markerInstances.Add(Instantiate(markerPrefab, hexPosition, Quaternion.identity, worldController.transform));
    }

    private List<Hex3> GetAdjacentResources(Hex3 hexPosition, List<Hex3> resourcePositions)
    {
        List<Hex3> neighbors = Hex3.GetNeighborLocations(hexPosition);
        foreach (Hex3 neighbor in neighbors)
        {
            HexTile tile = HexTileManager.GetHexTileAtLocation(neighbor);
            if (tile == null)
                continue;

            if (tile.TileType != GetTileTypeForResource(resourceType))
                continue;

            if(!resourcePositions.Contains(neighbor))
            {
                resourcePositions.Add(neighbor);
                GetAdjacentResources(neighbor, resourcePositions);
            }
        }

        return resourcePositions;
    }

    private HexTileType GetTileTypeForResource(ResourceType resourceType)
    {
        switch (resourceType)
        {
            case ResourceType.FeOre:
                return HexTileType.feOre;
            case ResourceType.AlOre:
                return HexTileType.alOre;
            case ResourceType.TiOre:
                return HexTileType.tiOre;
            case ResourceType.UOre:
                return HexTileType.uOre;
            case ResourceType.Oil:
                return HexTileType.oil;
            case ResourceType.Gas:
                return HexTileType.gas;
            case ResourceType.CuOre:
                return HexTileType.cuOre;
            default:
                return HexTileType.grass;
        }
    }

    public override List<string> DisplayText()
    {
        List<string> strings = new();
        strings.Add(requiredBuilding.DisplayText);
        strings.Add($"Collect {resourceType.ToNiceString()}: {amountCollected}/{amountToCollect}");
        return strings;
    }

    public override List<bool> IsComplete()
    {
        List<bool> results = new();
        results.Add(requiredBuilding.numberBuilt >= requiredBuilding.totalToBuild);
        results.Add(amountCollected >= amountToCollect);
        return results;
    }

    public override void OnComplete()
    {
        foreach (ResourceTile resourceTile in resourceTiles)
        {
            resourceTile.resourceExtractedLocal -= ResourceExtracted;
        }

        PlayerUnit.unitCreated -= UnitCreated;

        base.OnComplete();
    }

    public override void Failed()
    {
        MessagePanel.ShowMessage($"Deadline for {resourceType.ToNiceString()} extraction missed.", null);

        if (tempQuestReward == null)
            tempQuestReward = new QuestReward();
        tempQuestReward.techCreditsReward = Mathf.RoundToInt(questReward.techCreditsReward * 0.5f);
        tempQuestReward.repReward = Mathf.RoundToInt(questReward.repReward * 0.5f);
        this.isFailed = true;
        FindFirstObjectByType<DirectiveMenu>().QuestTimeExpired(this);
        DirectiveUpdated();
    }

    public void MoveToLocation()
    {
        foreach(var tile in resourceTiles)
        {
            if (tile.transform != null)
            {
                MoveToLocationClicked?.Invoke(tile.transform.position);
                return;
            }
        }

        Debug.LogError($"No resource tiles found for: {this.name}");
    }
}
