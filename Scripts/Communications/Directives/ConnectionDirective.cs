using HexGame.Units;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Connection Directive", menuName = "Hex/Directives/ConnectionDirective")]
public class ConnectionDirective : DirectiveBase
{
    [SerializeField] private List<ConnectionRequirement> connectionRequiremented = new ();
    [SerializeField] private bool removeConnections = false;
    public override List<string> DisplayText()
    {
        List<string> displayText = new List<string>();

        if (!removeConnections)
        {
            foreach (ConnectionRequirement connectionRequirement in connectionRequiremented)
            {
                displayText.Add($"Add connection - {connectionRequirement.startConnection.ToNiceString()} to {connectionRequirement.endConnection.ToNiceString()}: {connectionRequirement.connectionCount}/{connectionRequirement.connectionRequired}");
            }
        }
        else
        {
            foreach (ConnectionRequirement connectionRequirement in connectionRequiremented)
            {
                displayText.Add($"Remove connection - {connectionRequirement.startConnection.ToNiceString()} to {connectionRequirement.endConnection.ToNiceString()}: {connectionRequirement.connectionCount}/{connectionRequirement.connectionRequired}");
            }
        }

        return displayText;
    }

    public override void Initialize()
    {
        base.Initialize();
        if (removeConnections)
            UnitStorageBehavior.connectionRemoved += ConnectionChanged;
        else
            UnitStorageBehavior.connectionAdded += ConnectionChanged;

        CommunicationMenu.AddCommunication(OnStartCommunication);
        //foreach (ConnectionRequirement connectionRequirement in connectionRequiremented)
        //{
        //    connectionRequirement.connectionCount = 0;
        //}
    }

    private void ConnectionChanged(UnitStorageBehavior behavior1, UnitStorageBehavior behavior2)
    {
        if(behavior1.TryGetComponent(out PlayerUnit playerUnit1) && behavior2.TryGetComponent(out PlayerUnit playerUnit2))
        {
            PlayerUnitType start = GetUnitType(playerUnit1);
            PlayerUnitType end = GetUnitType(playerUnit2);
            foreach (ConnectionRequirement connectionRequirement in connectionRequiremented)
            {
                if (connectionRequirement.startConnection == start && connectionRequirement.endConnection == end)
                {
                    connectionRequirement.connectionCount++;
                    DirectiveUpdated();
                }
            }
        }
    }

    private PlayerUnitType GetUnitType(PlayerUnit playerUnit)
    {
        if (playerUnit.unitType != PlayerUnitType.buildingSpot)
            return playerUnit.unitType;
        else
            return playerUnit.GetComponent<BuildingSpotBehavior>().unitTypeToBuild;
    }

    public override List<bool> IsComplete()
    {
        List<bool> result = new List<bool>();
        foreach (ConnectionRequirement connectionRequirement in connectionRequiremented)
        {
            result.Add(connectionRequirement.connectionCount >= connectionRequirement.connectionRequired);
        }

        return result;
    }

    public override void OnComplete()
    {
        if (removeConnections)
            UnitStorageBehavior.connectionRemoved -= ConnectionChanged;
        else
            UnitStorageBehavior.connectionAdded -= ConnectionChanged;
        CommunicationMenu.AddCommunication(OnCompleteCommunication);
        OnCompleteTrigger.ForEach(t => t.DoTrigger());
    }

    [System.Serializable]
    public class ConnectionRequirement
    {
        public PlayerUnitType startConnection;
        public PlayerUnitType endConnection;
        [NonSerialized] public int connectionCount = 0;
        public int connectionRequired = 1;
    }
}
