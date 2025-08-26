using HexGame.Resources;
using HexGame.Units;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ConnectionDisplayManager : MonoBehaviour, ISaveData
{
    [SerializeField] private DeliveryConnection connectionPrefab;
    private static ObjectPool<DeliveryConnection> connectionPool;
    [SerializeField] private List<DeliveryConnection> activeConnections = new List<DeliveryConnection>();
    private ConnectionInfo connectionInfo = new ConnectionInfo();

    [Header("Visuals")] 
    [SerializeField] private RangeIndication rangeIndication;
    [SerializeField] private Color borderColor;
    [SerializeField] private Color fillColor;
    private UnitStorageBehavior selectedStorage;
    private UIControlActions uiControls;
    private HexTileManager hexTileManager;
    private UnitManager unitManager;
    private static bool connectionsUnlocked;
    public static bool ConnectionsUnlocked => connectionsUnlocked;
    private Camera Cam
    {
        get
        {
            if(cam == null)
                cam = Camera.main;

            return cam;
        }
    }
    private Camera cam;


    private void Awake()
    {
        if(connectionPool == null)
        {
            connectionPool = new ObjectPool<DeliveryConnection>(connectionPrefab, 5);
        }

        uiControls = new UIControlActions();
        hexTileManager = FindObjectOfType<HexTileManager>();
        unitManager = FindObjectOfType<UnitManager>();
        connectionsUnlocked = false;

        RegisterDataSaving();
    }

    private void OnEnable()
    {
        UnitSelectionManager.unitSelected += OnUnitSelected;
        UnitSelectionManager.unitUnSelected += OnUnitUnselected;
        UnitStorageBehavior.startListeningAddConnection += ShowRange;
        UnitStorageBehavior.stopListeningAddConnection += HideRange;
        uiControls.UI.AltPressed.started += ShowIncomingConnections;
        uiControls.UI.AltPressed.canceled += HideIncomingConnections;
        uiControls.UI.AltPressed.Enable();
        //usb.preferredDeliveryChanged += OnPreferredDeliveryChanged;
        UnlockConnectionsTrigger.ConnectionsUnlocked += UnlockConnections;

        ResourceUI.resourceHovered += ShowConnectionByResource;
        ResourceUI.resourceUnHovered += ClearActiveConnections;
    }

    private void OnDisable()
    {
        UnitSelectionManager.unitSelected -= OnUnitSelected;
        UnitSelectionManager.unitUnSelected -= OnUnitUnselected;
        UnitStorageBehavior.startListeningAddConnection -= ShowRange;
        UnitStorageBehavior.stopListeningAddConnection -= HideRange;
        uiControls.UI.AltPressed.started -= ShowIncomingConnections;
        uiControls.UI.AltPressed.canceled -= HideIncomingConnections;
        uiControls.UI.AltPressed.Disable();
        //UnitStorageBehavior.preferredDeliveryChanged -= OnPreferredDeliveryChanged;
        UnlockConnectionsTrigger.ConnectionsUnlocked -= UnlockConnections;

        ResourceUI.resourceHovered -= ShowConnectionByResource;
        ResourceUI.resourceUnHovered -= ClearActiveConnections;
    }

    private void UnlockConnections()
    {
        connectionsUnlocked = true;
    }

    private void Update()
    {
        if(Keyboard.current.shiftKey.wasPressedThisFrame && UnitSelectionManager.selectedUnit != null && !hexTileManager.IsPlacingTile && !unitManager.IsPlacing)
        {
            rangeIndication.ShowRange(UnitSelectionManager.selectedUnit.transform.position, CargoManager.transportRange);
        }
        else if(Keyboard.current.shiftKey.wasReleasedThisFrame && UnitSelectionManager.selectedUnit != null)
            rangeIndication.HideRange();
    }

    private void HideRange(UnitStorageBehavior usb)
    {
        rangeIndication.HideRange();
    }

    private void ShowRange(UnitStorageBehavior usb)
    {
        rangeIndication.ShowRange(usb.transform.position, CargoManager.transportRange, 0);
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
            newConnection.SetResources(unit, connection.storage);
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
                    newConnection.SetResources(storage, selectedStorage);
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
                newConnection.SetResources(storage, connection.storage);
                connectionInfo.connectionDataList.Add(new ConnectionData { deliveryStorage = connection.storage, connectionDisplay = newConnection });
                return;
            }
        }

        //check for removed connections
        for (int i = 0; i < connectionInfo.connectionDataList.Count; i++)
        {
            ConnectionData connectionData = connectionInfo.connectionDataList[i];
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
                activeConnections.Remove(connectionData.connectionDisplay);
                connectionInfo.connectionDataList.Remove(connectionData);
                return;
            }
        }
    }

    [Button]
    private void ShowConnectionByResource(ResourceType resource = ResourceType.Energy)
    {
        ClearActiveConnections();

        foreach (var storage in UnitManager.playerStorage)
        {
            foreach (var connection in storage.GetConnectionInfoByResource(resource))
            {
                //attempting to cull non-visible connections
                if (!IsPositionVisible(Cam, storage.Position) && !IsPositionVisible(Cam, connection.storage.Position))
                    continue;

                DeliveryConnection newConnection = connectionPool.Pull();
                activeConnections.Add(newConnection);
                newConnection.transform.SetParent(this.transform);
                newConnection.transform.position = storage.Position;
                newConnection.SetPositions(storage.Position, connection.storage.Position);
                ConnectionStatus status = storage.GetConnectionStatus(connection.storage);
                newConnection.SetStatus(status);
                newConnection.SetResource(resource);
            }
        }
    }

    private bool IsPositionVisible(Camera cam, Vector3 position)
    {
        Vector3 viewportPoint = cam.WorldToViewportPoint(position);

        // Check if the point is in front of the camera
        if (viewportPoint.z < 0)
            return false;

        // Check if the point is within the camera's viewport rectangle (0 to 1 in x and y)
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1;
    }

    private void ClearActiveConnections(ResourceType resource) => ClearActiveConnections();
    private void ClearActiveConnections()
    {
        foreach (var connection in activeConnections)
        {
            connection.gameObject.SetActive(false);
        }
        activeConnections.Clear();
    }

    private const string CONNECTIONS_UNLOCKED = "connectionsUnlocked";

    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this);
    }

    public void Save(string savePath, ES3Writer writer)
    {
        writer.Write<bool>(CONNECTIONS_UNLOCKED, connectionsUnlocked);
    }

    public IEnumerator Load(string loadPath, System.Action<string> postUpdateMessage)
    {
        if(ES3.KeyExists(CONNECTIONS_UNLOCKED, loadPath))
            connectionsUnlocked = ES3.Load<bool>(CONNECTIONS_UNLOCKED, loadPath);

        yield return null;
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
