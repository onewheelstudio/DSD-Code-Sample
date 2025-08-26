using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Directives/Explore Tiles")]
public class ExploreTilesDirective : DirectiveQuest
{
    [SerializeField] private int tilesToExplore = 10;
    [NonSerialized] private int tilesExplored = 0;

    public override void Initialize()
    {
        base.Initialize();
        FogGroundTile.TileRevealed += OnTileRevealed;
    }

    public override List<string> DisplayText()
    {
        return new List<string> { $"Explore newly created land mass: {tilesExplored}/{tilesToExplore}" };
    }

    public override List<bool> IsComplete()
    {
        return new List<bool> { tilesExplored >= tilesToExplore };
    }

    private void OnTileRevealed(FogGroundTile tile)
    {
        tilesExplored++;
        DirectiveUpdated();

        if(tilesExplored >= tilesToExplore)
        {
            FogGroundTile.TileRevealed -= OnTileRevealed;
        }
    }
}
