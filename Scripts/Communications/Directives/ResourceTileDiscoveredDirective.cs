using HexGame.Grid;
using HexGame.Resources;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Directives/Resource Tile Discovered Directive")]
public class ResourceTileDiscoveredDirective : DirectiveQuest
{
    [SerializeField] private bool allowPreviousReveals = false;
    [SerializeField] private ResourceType resourceType;
    [SerializeField] private int numberToReveal = 1;
    [NonSerialized] private int numberRevealed;

    [SerializeField] private int numberToExtract = 50;
    [NonSerialized] private int numberExtracted;
    [NonSerialized] private List<ResourceTile> resourceTiles = new();

    public override List<string> DisplayText()
    {
        List<string> text = new List<string>();
        if(numberToReveal > 1)
            text.Add(new string($"Discover {resourceType.ToNiceString()} Tiles: {numberRevealed}/{numberToReveal}"));
        else if(numberToReveal == 1)
            text.Add(new string($"Discover {resourceType.ToNiceString()} Tile: {numberRevealed}/{numberToReveal}"));

        if (numberToExtract > 1 && numberToReveal > 0)
            text.Add(new string($"Extract {resourceType.ToNiceString()} From Discovered Tiles: {numberExtracted}/{numberToExtract}"));
        else if (numberToExtract > 0 && numberToReveal > 0)
            text.Add(new string($"Extract {resourceType.ToNiceString()} From Discovered Tile: {numberExtracted}/{numberToExtract}"));
        else if (numberToExtract > 1)
            text.Add(new string($"Extract {resourceType.ToNiceString()}: {numberExtracted}/{numberToExtract}"));
        else if (numberToExtract == 1)
            text.Add(new string($"Extract {resourceType.ToNiceString()}: {numberExtracted}/{numberToExtract}"));

        return text;
    }

    public override void Initialize()
    {
        numberExtracted = 0;
        numberRevealed = 0;

        if(allowPreviousReveals)
        {
            resourceTiles.Clear();
            HexTileType tileType = GetTileTypeForResource(resourceType);
            if(tileType != HexTileType.grass) //grass used as a default value
            {
                List<HexTile> revealedTiles = HexTileManager.GetAllRevealedTilesOfTYpe(tileType);
                for (int i = revealedTiles.Count - 1; i >= 0; i--)
                {
                    if (revealedTiles[i].hexPosition.Max() < 5)
                        revealedTiles.RemoveAt(i);
                    else if(revealedTiles[i].TryGetComponent(out ResourceTile resourceTile))
                    {
                        resourceTiles.Add(resourceTile);
                        resourceTile.resourceExtractedLocal += ResourceExtracted;
                    }
                }
                numberRevealed = Mathf.Max(numberToReveal, revealedTiles.Count);
            }
        }

        if(numberToReveal > 0)
            ResourceTile.resourceTileRevealed += ResourceTileRevealed;
        if(numberToReveal == 0)
            ResourceTile.resourceExtractedGlobal += ResourceExtracted;
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
            case ResourceType.cuOre:
                return HexTileType.cuOre;
            default:
                return HexTileType.grass;
        }
    }

    public override void OnComplete()
    {
        if(numberToReveal > 0)
            ResourceTile.resourceTileRevealed -= ResourceTileRevealed;
        if(numberToReveal == 0)
            ResourceTile.resourceExtractedGlobal -= ResourceExtracted;
        resourceTiles.ForEach(rt => rt.resourceExtractedLocal -= ResourceExtracted);
    }

    public override List<bool> IsComplete()
    {
        List<bool> result = new List<bool>();
        result.Add(numberRevealed >= numberToReveal);
        result.Add(numberExtracted >= numberToExtract);
        return result;
    }

    private void ResourceTileRevealed(ResourceType type, ResourceTile resourceTile)
    {
        if (type != resourceType)
            return;

        if(!resourceTiles.Contains(resourceTile))
            resourceTiles.Add(resourceTile);

        resourceTile.resourceExtractedLocal += ResourceExtracted;

        numberRevealed++;
        if(numberRevealed > numberToReveal)
        {
            numberRevealed = numberToReveal;
        }
        else
            DirectiveUpdated();
    }

    private void ResourceExtracted(ResourceType type, ResourceTile resourceTile)
    {
        if (type != resourceType)
            return;

        if (!resourceTiles.Contains(resourceTile) && numberToReveal > 0)
            return;

        numberExtracted++;
        if(numberExtracted > numberToExtract)
            numberExtracted = numberToExtract;
        else
            DirectiveUpdated();
    }
}
