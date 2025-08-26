using HexGame.Grid;
using HexGame.Units;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConnectionManager : MonoBehaviour, ISaveData
{
    private const string CONNECTION_SAVE_PATH = "ConnectionData";

    private void Awake()
    {
        RegisterDataSaving();
    }

    public IEnumerator Load(string loadPath, System.Action<string> postUpdateMessage)
    {
        if (ES3.KeyExists(CONNECTION_SAVE_PATH, loadPath))
        {
            List<ConnectionData> data = ES3.Load<List<ConnectionData>>(CONNECTION_SAVE_PATH, loadPath);

            foreach (var connectionData in data)
            {
                if(!UnitManager.TryGetPlayerUnitAtLocation(connectionData.unitLocation, out PlayerUnit playerUnit))
                {
                    Debug.LogError("Could not find player unit at location: " + connectionData.unitLocation);
                    continue;
                }

                if(!playerUnit.TryGetComponent(out UnitStorageBehavior unitStorage))
                {
                    Debug.LogError("Could not find unit storage behavior on unit at location: " + connectionData.unitLocation);
                    continue;
                }

                foreach (var location in connectionData.connectionLocations)
                {
                    if (!UnitManager.TryGetPlayerUnitAtLocation(location, out PlayerUnit connectionUnit))
                    {
                        Debug.LogError("Could not find player unit at location: " + location);
                        continue;
                    }

                    if (!connectionUnit.TryGetComponent(out UnitStorageBehavior connectionStorage))
                    {
                        Debug.LogError("Could not find unit storage behavior on unit at location: " + location);
                        continue;
                    }

                    unitStorage.AddDeliverConnection(connectionStorage);
                }

            }
        }
        yield return null;
    }

    public void RegisterDataSaving()
    {
        //must be after the unit manager is loaded
        //we can only load connections if all the units are loaded
        SaveLoadManager.RegisterData(this,2f);
    }

    public void Save(string savePath, ES3Writer writer)
    {
        List<ConnectionData> connectionData = new List<ConnectionData>();
        foreach (var playerUnit in UnitManager.playerUnits)
        {
            if (playerUnit.TryGetComponent(out UnitStorageBehavior storage))
            {
                ConnectionData data = new ConnectionData();
                data.unitLocation = playerUnit.Location;

                List<UnitStorageBehavior> connections = storage.GetConnections().ToList();
                if (connections.Count == 0)
                    continue;

                data.connectionLocations = new List<Hex3>();
                foreach (var connection in connections)
                {
                    data.connectionLocations.Add(connection.transform.position.ToHex3());
                }
                connectionData.Add(data);
            }
        }

        writer.Write<List<ConnectionData>>(CONNECTION_SAVE_PATH, connectionData);
    }

    public struct ConnectionData
    {
        public Hex3 unitLocation;
        public List<Hex3> connectionLocations;
    }
}
