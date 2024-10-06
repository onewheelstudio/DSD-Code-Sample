using HexGame.Resources;
using HexGame.Units;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ConnectionDisplayManager : MonoBehaviour
{
    [SerializeField] private DeliveryConnection connectionPrefab;
    private static ObjectPool<DeliveryConnection> connectionPool;
    [SerializeField] private List<DeliveryConnection> activeConnections = new List<DeliveryConnection>();
    private ConnectionInfo connectionInfo = new ConnectionInfo();

    [Header("Visuals")] 
    [SerializeField] private HexRange hexRange;
    [SerializeField] private Color borderColor;
    [SerializeField] private Color fillColor;
    private UnitStorageBehavior selectedStorage;
    private UIControlActions uiControls;

    private void Awake()
    {
        if(connectionPool == null)
        {
            connectionPool = new ObjectPool<DeliveryConnection>(connectionPrefab, 5);
        }

        uiControls = new UIControlActions();
    }

    private void OnEnable()
    {
        UnitSelectionManager.unitSelected += OnUnitSelected;
        UnitSelectionManager.unitUnSelected += OnUnitUnselected;
        UnitStorageBehavior.startListeningForConnection += ShowRange;
        UnitStorageBehavior.stopListeningForConnection += HideRange;
        uiControls.UI.AltPressed.started += ShowIncomingConnections;
        uiControls.UI.AltPressed.canceled += HideIncomingConnections;
        uiControls.UI.AltPressed.Enable();
        //usb.preferredDeliveryChanged += OnPreferredDeliveryChanged;
    }

    private void OnDisable()
    {
        UnitSelectionManager.unitSelected -= OnUnitSelected;
        UnitSelectionManager.unitUnSelected -= OnUnitUnselected;
        UnitStorageBehavior.startListeningForConnection -= ShowRange;
        UnitStorageBehavior.stopListeningForConnection -= HideRange;
        uiControls.UI.AltPressed.started -= ShowIncomingConnections;
        uiControls.UI.AltPressed.canceled -= HideIncomingConnections;
        uiControls.UI.AltPressed.Disable();
        //UnitStorageBehavior.preferredDeliveryChanged -= OnPreferredDeliveryChanged;
    }

    private void Update()
    {
        if(Keyboard.current.shiftKey.wasPressedThisFrame && UnitSelectionManager.selectedUnit != null)
        {
            hexRange.transform.position = UnitSelectionManager.selectedUnit.transform.position + Vector3.up * 0.1f;
            hexRange.ShowRange(CargoManager.transportRange, 0, borderColor, fillColor);
        }
        else if(Keyboard.current.shiftKey.wasReleasedThisFrame && UnitSelectionManager.selectedUnit != null)
            hexRange.HideRange();
    }

    private void HideRange(UnitStorageBehavior usb)
    {
        hexRange.HideRange();
    }

    private void ShowRange(UnitStorageBehavior usb)
    {
        hexRange.transform.position = usb.transform.position + Vector3.up * 0.1f;
        hexRange.ShowRange(CargoManager.transportRange, 0, borderColor, fillColor);
    }

    private void OnUnitUnselected(PlayerUnit unit)
    {
        foreach (var connection in activeConnections)
        {
            connection.gameObject.SetActive(false);
        }
        activeConnections.Clear();
        selectedStorage = null;
    }

    private void OnUnitSelected(PlayerUnit unit)
    {
        selectedStorage = unit.GetComponent<UnitStorageBehavior>();

        OnUnitSelected(selectedStorage);
    }
    
    private void OnUnitSelected(UnitStorageBehavior unit)
    {
        if (selectedStorage == null)
            return;

        if(connectionInfo.pickupStorage != selectedStorage)
        {
            if(connectionInfo.pickupStorage != null)
                connectionInfo.pickupStorage.connectionChanged -= OnPreferredDeliveryChanged;
            selectedStorage.connectionChanged += OnPreferredDeliveryChanged;
        }

        connectionInfo = new ConnectionInfo();
        connectionInfo.pickupStorage = selectedStorage;
        connectionInfo.connections = selectedStorage.GetConnectionInfo();

        if (selectedStorage.GetConnectionInfo().Count == 0)
            return;

        foreach (var connection in selectedStorage.GetConnectionInfo())
        {
            DeliveryConnection newConnection = connectionPool.Pull();
            activeConnections.Add(newConnection);
            newConnection.transform.SetParent(this.transform);
            newConnection.transform.position = unit.transform.position;
            newConnection.SetPositions(unit.transform.position, connection.storage.transform.position);
            ConnectionStatus status = connectionInfo.pickupStorage.GetConnectionStatus(connection.storage);
            newConnection.SetStatus(status);
            connectionInfo.connectionDataList.Add(new ConnectionData { deliveryStorage = connection.storage, connectionDisplay = newConnection });
        }
    }

    private void ShowIncomingConnections(InputAction.CallbackContext context)
    {
        ShowIncomingConnections();
    }

    private void HideIncomingConnections(InputAction.CallbackContext context)
    {
        foreach (var connection in activeConnections)
        {
            connection.gameObject.SetActive(false);
        }
        activeConnections.Clear();

        OnUnitSelected(selectedStorage);
    }

    [Button]
    private void ShowIncomingConnections()
    {
        if (selectedStorage == null)
            return;

        foreach (var connection in activeConnections)
        {
            connection.gameObject.SetActive(false);
        }
        activeConnections.Clear();

        UnitManager.playerUnits.ForEach(u =>
        {
            if (u != selectedStorage)
            {
                UnitStorageBehavior storage = u.GetComponent<UnitStorageBehavior>();
                if (storage != null && storage.GetConnections().Contains(selectedStorage))
                {
                    DeliveryConnection newConnection = connectionPool.Pull();
                    activeConnections.Add(newConnection);
                    newConnection.transform.SetParent(this.transform);
                    newConnection.transform.position = u.transform.position;
                    newConnection.SetPositions(u.transform.position, selectedStorage.transform.position);
                    ConnectionStatus status = storage.GetConnectionStatus(selectedStorage);
                    newConnection.SetStatus(status);
                    connectionInfo.connectionDataList.Add(new ConnectionData { deliveryStorage = selectedStorage, connectionDisplay = newConnection });
                }
            }
        });
    }

    private void OnPreferredDeliveryChanged(UnitStorageBehavior storage)
    {
        if (storage != connectionInfo.pickupStorage)
            return;

        //check for added connections
        foreach (var connection in storage.GetConnectionInfo())
        {
            bool foundConnection = false;
            foreach (var connectionData in connectionInfo.connectionDataList)
            {
                if (connectionData.deliveryStorage == connection.storage)
                {
                    foundConnection = true;
                    break;
                }
            }

            if (!foundConnection)
            {
                DeliveryConnection newConnection = connectionPool.Pull();
                activeConnections.Add(newConnection);
                newConnection.transform.SetParent(this.transform);
                newConnection.transform.position = connectionInfo.pickupStorage.transform.position;
                newConnection.SetPositions(connectionInfo.pickupStorage.transform.position, connection.storage.transform.position);
                ConnectionStatus status = connectionInfo.pickupStorage.GetConnectionStatus(connection.storage);
                newConnection.SetStatus(status);
                connectionInfo.connectionDataList.Add(new ConnectionData { deliveryStorage = connection.storage, connectionDisplay = newConnection });
                return;
            }
        }

        //check for removed connections
        foreach (var connectionData in connectionInfo.connectionDataList)
        {
            bool foundConnection = false;
            foreach (var connection in storage.GetConnectionInfo())
            {
                if (connectionData.deliveryStorage == connection.storage)
                {
                    foundConnection = true;
                    break;
                }
            }

            if (!foundConnection)
            {
                connectionData.connectionDisplay.gameObject.SetActive(false);
                connectionInfo.connectionDataList.Remove(connectionData);
                return;
            }
        }
    }

    public class ConnectionInfo
    {
        public UnitStorageBehavior pickupStorage;
        public List<ConnectionStatusInfo> connections;
        public List<ConnectionData> connectionDataList = new List<ConnectionData>();
    }

    public class ConnectionData
    {
        public UnitStorageBehavior deliveryStorage;
        public DeliveryConnection connectionDisplay;
    }
}
