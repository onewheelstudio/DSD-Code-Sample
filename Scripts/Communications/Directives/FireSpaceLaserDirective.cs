using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Directives/Fire Space Laser Directive")]
public class FireSpaceLaserDirective : DirectiveBase
{
    private int numberOfShots = 0;
    [SerializeField] private int shotsRequired = 3;
    public override List<string> DisplayText()
    {
        return new List<string>() { $"Fire Space Laser Test Shots: {numberOfShots}/{shotsRequired}" };
    }

    public override void Initialize()
    {
        base.Initialize();
        //numberOfShots = 0;
        if(OnStartCommunication != null)
            CommunicationMenu.AddCommunication(OnStartCommunication);
        SpaceLaser.SpaceLaserFired += SpaceLaserFired;
    }

    public override List<bool> IsComplete()
    {
        return new List<bool>() { numberOfShots >= shotsRequired };
    }

    public override void OnComplete()
    {
        if (OnCompleteCommunication != null)
            CommunicationMenu.AddCommunication(OnCompleteCommunication);
        SpaceLaser.SpaceLaserFired -= SpaceLaserFired;
    }

    private void SpaceLaserFired()
    {
        numberOfShots++;
        DirectiveUpdated();
    }
}
