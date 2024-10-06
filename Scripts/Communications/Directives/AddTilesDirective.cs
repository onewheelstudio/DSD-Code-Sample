using HexGame.Resources;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Add Tiles Directive", menuName = "Hex/Directives/AddTilesDirective")]
public class AddTilesDirective : DirectiveBase
{
    [SerializeField] private int numberOfTilesToAdd = 1;
    private int numberAdded = 0;
    [SerializeField] bool requireSpecificTileType = false;
    [SerializeField,ShowIf("requireSpecificTileType")] private HexTileType tileType = HexTileType.grass;

    public override List<string> DisplayText()
    {
        if(requireSpecificTileType)
            return new List<string>() { $"Build {tileType.ToNiceString()}: {numberAdded}/{numberOfTilesToAdd}" };
        else
            return new List<string>() { $"Build Terrain: {numberAdded}/{numberOfTilesToAdd}" };
    }

    public override void Initialize()
    {
        numberAdded = 0;
        if(OnStartCommunication != null)
            CommunicationMenu.AddCommunication(OnStartCommunication);

        PlaceHolderTileBehavior.tileComplete += TileComplete;
    }

    public override List<bool> IsComplete()
    {
        return new List<bool>() { numberAdded >= numberOfTilesToAdd };
    }

    public override void OnComplete()
    {
        if(OnCompleteCommunication != null)
            CommunicationMenu.AddCommunication(OnCompleteCommunication);

        OnCompleteTrigger.ForEach(t => t.DoTrigger());
        PlaceHolderTileBehavior.tileComplete -= TileComplete;
    }

    private void TileComplete(PlaceHolderTileBehavior behavior, HexTileType type)
    {
        if(!requireSpecificTileType || type == this.tileType)
        {
            numberAdded++;
        }
        DirectiveUpdated();
    }
}
