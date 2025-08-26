using HexGame.Grid;
using HexGame.Resources;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Runtime.Versioning;
using UnityEngine;

[System.Serializable]
public class LevelData
{
    public Hex3 location;
    public bool isActive;
    public List<UIMapTile> neighbors = new();
    public SectorControl SectorControl => sectorControl;
    private SectorControl sectorControl = SectorControl.NONE;

    public float enemyForces;
    public float playerForces;
    public float stat3;
    public float stat4;
    public float stat5;
    private int simTicks = 0;
    public List<ResourceAmount> resources = new();

    public int UnControlledNeighbors => GetNeighborsControlledBy(SectorControl.NONE).Count;
    public int PlayerControlledNeighbors => GetNeighborsControlledBy(SectorControl.Player).Count;
    public int EnemyControlledNeighbors => GetNeighborsControlledBy(SectorControl.Enemy).Count;

    public int ActiveNeighbors
    {
        get
        {
            int count = 0;
            foreach (var neighbor in neighbors)
            {
                if (neighbor.levelData.isActive)
                    count++;
            }

            return count;
        }
    }

    [Header("Tile Needs")]
    [SerializeField] private List<ResourceAmount> requests = new();
    [SerializeField] private int foodNeeded;
    [SerializeField] private int waterNeeded;
    [SerializeField] private float foodPerDay;
    [SerializeField] private float waterPerDay;
    private int maxRequests = 5;

    public List<UIMapTile> GetNeighborsControlledBy(SectorControl control)
    {
        List<UIMapTile> controlledNeighbors = new List<UIMapTile>();
        foreach (var neighbor in neighbors)
        {
            if (neighbor.levelData.sectorControl == control && neighbor.levelData.isActive)
                controlledNeighbors.Add(neighbor);
        }

        return controlledNeighbors;
    }

    internal void SetControl(SectorControl control)
    {
        this.sectorControl = control;
    }

    public void DoResourceTick()
    {
        simTicks++;
        if (foodNeeded < playerForces)
        {
            foodNeeded += Mathf.RoundToInt(foodPerDay * playerForces);
        }

        if (waterNeeded < playerForces)
        {
            waterNeeded += Mathf.RoundToInt(waterPerDay * playerForces);
        }

        if(requests.Count > maxRequests)
        {
            RemoveRequest();
        }

        if(simTicks < 2 || simTicks % 5 != 0)
            requests.Add(GenerateResourceRequest());
    }



    [ButtonGroup("Deliveries")]
    private void DeliverFood()
    {
        DeliverResource(new ResourceAmount(ResourceType.Food, 50));
    }

    [ButtonGroup("Deliveries")]
    private void DeliverWater()
    {
        DeliverResource(new ResourceAmount(ResourceType.Water, 50));
    }

    public void DeliverResource(ResourceAmount resource)
    {
        playerForces += resource.amount * GetResourceScore(resource.type);
    }

    private float GetResourceScore(ResourceType resource)
    {
        switch (resource)
        {
            case ResourceType.Food:
            case ResourceType.Water:
            case ResourceType.Energy:
                return 1f;
            case ResourceType.FeOre:
                break;
            case ResourceType.FeIngot:
                break;
            case ResourceType.AlOre:
                break;
            case ResourceType.AlIngot:
                break;
            case ResourceType.TiOre:
                break;
            case ResourceType.TiIngot:
                break;
            case ResourceType.UOre:
                break;
            case ResourceType.UIngot:
                break;
            case ResourceType.Oil:
                break;
            case ResourceType.Gas:
                break;
            case ResourceType.Carbon:
                break;
            case ResourceType.BioWaste:
                break;
            case ResourceType.IndustrialWaste:
                break;
            case ResourceType.Terrene:
                break;
            case ResourceType.Thermite:
                break;
            case ResourceType.SteelPlate:
                break;
            case ResourceType.IronCog:
                break;
            case ResourceType.AlPlate:
                break;
            case ResourceType.AlCog:
                break;
            case ResourceType.Hydrogen:
                break;
            case ResourceType.Nitrogen:
                break;
            case ResourceType.Oxygen:
                break;
            case ResourceType.AmmoniumNitrate:
                break;
            case ResourceType.CuOre:
                break;
            case ResourceType.CuIngot:
                break;
            case ResourceType.CannedFood:
                break;
            case ResourceType.FuelRod:
                break;
            case ResourceType.WeaponsGradeUranium:
                break;
            case ResourceType.ExplosiveShell:
                break;
            case ResourceType.Sulfer:
                break;
            case ResourceType.Plastic:
                break;
            case ResourceType.CarbonFiber:
                break;
            case ResourceType.Sand:
                break;
            case ResourceType.Electronics:
                break;
            case ResourceType.UraniumShells:
                break;
            case ResourceType.SulfuricAcid:
                break;
            case ResourceType.Biomass:
                break;
            case ResourceType.TerraFluxCell:
                break;
        }

        return 0;
    }

    private ResourceAmount GenerateResourceRequest()
    {
        List<ResourceType> resources = new();
        foreach (ResourceType resource in System.Enum.GetValues(typeof(ResourceType)))
        {
            if (resource == ResourceType.Workers || resource == ResourceType.Food || resource == ResourceType.Water)
                continue;

            //prevent duplicates
            for (int i = 0; i < requests.Count; i++)
            {
                if(requests[i].type == resource)
                    continue;
            }

            resources.Add(resource);
        }

        ResourceType randomResource = resources[Random.Range(0, resources.Count)];
        int maxAmount = Mathf.RoundToInt(Mathf.Clamp(playerForces / 2000, 1, 5));
        int amount = Random.Range(1, maxAmount);
        
        return new ResourceAmount(randomResource, amount * 50);
    }

    private void RemoveRequest()
    {
        if (requests.Count == 0)
            return;

        int index = Random.Range(0, requests.Count);
        requests.RemoveAt(index);
    }

    public List<SectorRequest> GetRequests()
    {
        List<SectorRequest> sectorRequests = new();

        if (foodNeeded >= 50)
        {
            int amountToRequest = Mathf.FloorToInt(foodNeeded / 50) * 50;
            int resourceScore = (int)GetResourceScore(ResourceType.Food) * amountToRequest;
            sectorRequests.Add(new SectorRequest(this, new ResourceAmount(ResourceType.Food, amountToRequest), resourceScore));
        }

        if (waterNeeded >= 50)
        {
            int amountToRequest = Mathf.FloorToInt(foodNeeded / 50) * 50;
            int resourceScore = (int)GetResourceScore(ResourceType.Water) * amountToRequest;
            sectorRequests.Add(new SectorRequest(this, new ResourceAmount(ResourceType.Water, amountToRequest), resourceScore));
        }

        for (int i = 0; i < requests.Count; i++)
        {
            sectorRequests.Add(new SectorRequest(this, requests[i], (int)GetResourceScore(ResourceType.Water) * requests[i].amount));
        }

        return sectorRequests;
    }

}

public struct SectorRequest
{
    public ResourceAmount resource;
    public int reinforcements;
    public LevelData levelData;

    public SectorRequest(LevelData levelData, ResourceAmount resource, int reinforcements)
    {
        this.levelData = levelData;
        this.resource = resource;
        this.reinforcements = reinforcements;
    }
}
public enum SectorControl
{
    NONE,
    Player,
    Enemy,
    Contested,
}