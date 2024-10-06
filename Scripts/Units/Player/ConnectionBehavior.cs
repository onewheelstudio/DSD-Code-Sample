using HexGame.Grid;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexGame.Units
{
    public class ConnectionBehavior : UnitBehavior
    {
        [SerializeField]
        private List<ConnectionBehavior> connections = new List<ConnectionBehavior>();
        [SerializeField]
        [Range(1, 10)]
        private int connectionRange = 2;
        [SerializeField]
        private GameObject lineRendererPrefab;
        private List<LineRenderer> connectionLines = new List<LineRenderer>();
        [SerializeField]
        private Transform connectionPoint;

        public override void StartBehavior()
        {
            GetNearbyConnections();
        }

        public override void StopBehavior()
        {
            RemoveAllConnections();
        }

        private void GetNearbyConnections()
        {
            connections.Clear();

            foreach (var playerUnit in UnitManager.playerUnits)
            {
                if (Hex3.DistanceBetween(this.transform.position, playerUnit.transform.position) <= connectionRange)
                {
                    AddConnection(playerUnit.GetComponent<ConnectionBehavior>());
                    playerUnit.GetComponent<ConnectionBehavior>().AddConnection(this);
                }
            }
        }

        public void AddConnection(ConnectionBehavior connection)
        {
            if (!connections.Contains(connection) && connection != this)
            {
                connections.Add(connection);
                DrawNewConnection(connection);
            }
        }

        private void DrawNewConnection(ConnectionBehavior connection)
        {
            LineRenderer lr;

            if (lineRendererPrefab != null)
                lr = Instantiate(lineRendererPrefab).GetComponent<LineRenderer>();
            else
                lr = new GameObject().AddComponent<LineRenderer>();

            lr.positionCount = 2;
            lr.SetPosition(0, GetConnectionPoint(this));
            lr.SetPosition(1, GetConnectionPoint(connection));
        }

        public void RemoveConnection(ConnectionBehavior connection)
        {
            connection.RemoveConnection(connection);
        }

        public void RemoveAllConnections()
        {
            foreach (var connection in connections)
            {
                connection.RemoveConnection(connection);
            }

            this.connections.Clear();
        }

        public Vector3 GetConnectionPoint(ConnectionBehavior connection)
        {
            if (connection.connectionPoint == null)
                return connection.transform.position;
            else
                return connection.connectionPoint.position;
        }


    }
}