using HexGame.Grid;
using HexGame.Units;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Directives/Move To Location Directive")]
public class MoveMarineUnitDirective : DirectiveBase
{
    private int numberOfMoves = 0;
    [SerializeField] private int movesRequired = 1;
    public override List<string> DisplayText()
    {
        return new List<string>() { $"Explore with infantry unit: {numberOfMoves}/{movesRequired}" };
    }

    public override void OnComplete()
    {
        MarineBehavior.marineMovedStarted -= MarineMoved;
        if(OnCompleteCommunication != null)
            CommunicationMenu.AddCommunication(OnCompleteCommunication);
    }

    public override void Initialize()
    {
        numberOfMoves = 0;
        MarineBehavior.marineMovedStarted += MarineMoved;

        if(OnStartCommunication != null)
            CommunicationMenu.AddCommunication(OnStartCommunication);   
    }

    private void MarineMoved(MarineBehavior marine)
    {
        numberOfMoves++;
        DirectiveUpdated();
    }


    public override List<bool> IsComplete()
    {
        return new List<bool>() { numberOfMoves >= movesRequired };
    }
}
