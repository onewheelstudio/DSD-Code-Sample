using DG.Tweening;
using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using NovaSamples.UIControls;
using System.IO;

public class UnitManager : MonoBehaviour, ISaveData
{
    public static event Action<Unit> unitPlaced;
    public static List<PlayerUnit> playerUnits = new List<PlayerUnit>();
    /// <summary>
    /// This dictionary only contains units with a USB. No shuttles or other units are included.
    /// </summary>
    public static Dictionary<Hex3, PlayerUnit> playerUnitLocations = new Dictionary<Hex3, PlayerUnit>();
    public static List<UnitStorageBehavior> playerStorage = new List<UnitStorageBehavior>();
    public static List<EnemyUnit> enemyUnits = new List<EnemyUnit>();

    private BuildingSpotBehavior buildingSpot;
    public bool IsPlacing => buildingSpot != null;
    private Func<bool> allowRepeatPlacement;
    private Hex3 lastPosition;

    [SerializeField]
    private List<BuildCost> buildingCosts = new List<BuildCost>();
    private AssetBundle buildCostBundle;
    private AssetBundle completeUnitBundle;
    private AssetBundle unitPlaceholderBundle;
    private AssetBundle unitBuildUpBundle;

    [SerializeField]
    [Required]
    private GameObject buildingSpotPrefab;
    private ObjectPool<PoolObject> buildingSpotPool;
    public static event Action<PlayerUnitType> unitPlacementStarted;
    public static event Action unitPlacementFinished;

    [ShowInInspector]
    private Dictionary<PlayerUnitType, GameObject> prefabByType = new Dictionary<PlayerUnitType, GameObject>();
    [ShowInInspector]
    private Dictionary<PlayerUnitType, GameObject> placeHolderByType = new Dictionary<PlayerUnitType, GameObject>();
    [ShowInInspector]
    private Dictionary<PlayerUnitType, BuildOverTime> buildUpPrefabs = new Dictionary<PlayerUnitType, BuildOverTime>();
    private List<AddUnitButton> unitbuttons = new List<AddUnitButton>();

    [Header("Settings")]
    [SerializeField] private bool allowBuildingWithoutResources = true;
    [SerializeField] private GameSettings gameSettings;

    private BuildMenu buildMenu;
    private HexTileManager htm;

    [Header("Warning")]
    public static ObjectPool<WarningIcons> warningIcons;
    [SerializeField] private GameObject warningIcon;
    private static float hillOffset = 0.325f;
    public static float HillOffset => hillOffset;

    private void Awake()
    {
        buildingSpotPool = new ObjectPool<PoolObject>(buildingSpotPrefab);
        warningIcons = new ObjectPool<WarningIcons>(warningIcon);

        buildingCosts = LoadBuildCosts().ToList(); //loads asset bundle and filters for demo
        //LoadPrefabByType();
        //LoadPlaceHolderByType();
        //LoadBuildUpByType();
        LoadPrefabByType();
        LoadPlaceHolderByType();
        LoadBuildUpByType();


        playerUnits.Clear();
        playerStorage.Clear();
        playerUnitLocations.Clear();
        enemyUnits.Clear();
        htm = FindFirstObjectByType<HexTileManager>();
        RegisterDataSaving();

        CheatCodes.AddButton(ToggleLowCost, "Toggle Low Cost");
    }

    private void OnEnable()
    {
        Unit.unitRemoved += RemoveUnit;
        BuildingSpotBehavior.buildingComplete += CleanUpBuilding;
        PlaceHolderTileBehavior.tileComplete += UpdateBuildingSpotValidity;
        WindowPopup.SomeWindowOpened += ForceOff;
        UnitSelectionManager.UnitMoved += UnitMoved;
        UnitSelectionManager.unitSelected += RemoveBuilding;
    }

    private void OnDisable()
    {
        Unit.unitRemoved -= RemoveUnit;
        BuildingSpotBehavior.buildingComplete -= CleanUpBuilding;
        PlaceHolderTileBehavior.tileComplete -= UpdateBuildingSpotValidity;
        WindowPopup.SomeWindowOpened -= ForceOff;
        UnitSelectionManager.UnitMoved -= UnitMoved;
        UnitSelectionManager.unitSelected -= RemoveBuilding;
        DOTween.Kill(this,true);
    }

    private void OnDestroy()
    {
        if (buildCostBundle != null)
            buildCostBundle.UnloadAsync(true);
    }


    // Update is called once per frame
    void Update()
    {
        if ((Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame) && buildingSpot != null)
            RemoveBuilding(buildingSpot.gameObject);

        if (Mouse.current.leftButton.wasPressedThisFrame && buildingSpot != null && !PCInputManager.MouseOverVisibleUIObject() && buildingSpot.IsValidPlacement())
        {
            //buildingSpot.transform.position = HelperFunctions.GetMouseVector3OnPlane(true);
            CleanUpPlacement(buildingSpot.gameObject, buildingSpot.GetComponent<PlayerUnit>().unitType);
        }
        else if (buildingSpot != null && !UnitAtMouseLocation())
        {
            //unitToAdd.transform.position = HelperFunctions.GetMouseVector3OnPlane(true);
            HexTile tile = HexTileManager.GetHexTileAtLocation(buildingSpot.transform.position);
            if(tile != null && tile.TileType == HexTileType.hill)
                buildingSpot.transform.DOMove(HelperFunctions.GetMouseVector3OnPlane(true) + Vector3.up * hillOffset, 0.1f).SetUpdate(true);
            else
                buildingSpot.transform.DOMove(HelperFunctions.GetMouseVector3OnPlane(true), 0.1f).SetUpdate(true);

            //don't rotate solar panels
            if (tile != null && buildingSpot != null && buildingSpot.unitTypeToBuild == PlayerUnitType.solarPanel)
                buildingSpot.transform.localRotation = Quaternion.identity;
            else if (tile != null && buildingSpot != null)
                buildingSpot.transform.localRotation = tile.transform.localRotation;
        }

        if(buildingSpot != null && lastPosition != (Hex3)buildingSpot.transform.position)
        {
            buildingSpot.CheckIfLocationValid();
            lastPosition = buildingSpot.transform.position;
        }
    }

    private void ForceOff(WindowPopup popup)
    {
        if (popup is InfoToolTipWindow || buildingSpot == null)
            return;

        RemoveBuilding(buildingSpot.gameObject);
    }

    private void UpdateBuildingSpotValidity(PlaceHolderTileBehavior placeHolder, HexTileType tileType)
    {
        StartCoroutine(WaitOneFrameUpdateVisibility());
    }

    private IEnumerator WaitOneFrameUpdateVisibility()
    {
        yield return null;

        if (buildingSpot != null)
            buildingSpot.CheckIfLocationValid();
    }

    public void RegisterUnitButton(AddUnitButton button)
    {
        unitbuttons.Add(button);
    }
    
    public GameObject SetUnitTypeToAdd(PlayerUnitType unitType, Func<bool> repeatPlacement)
    {
        if (buildingSpot != null)
            buildingSpot.gameObject.SetActive(false);

        this.allowRepeatPlacement = repeatPlacement;
        buildingSpot = buildingSpotPool.Pull().GetComponent<BuildingSpotBehavior>();
        buildingSpot.SetTypeToBuild(unitType, GetPrefabByType(unitType), GetPlaceHolderByType(unitType), GetUnitCost(unitType));
        buildingSpot.gameObject.name = $"{unitType} building spot";
        lastPosition = buildingSpot.transform.position;
        unitPlacementStarted?.Invoke(unitType);

        return buildingSpot.gameObject;
    }

    private void CleanUpBuilding(BuildingSpotBehavior buildingSpot, GameObject buildingPrefab)
    {
        GameObject newBuilding = Instantiate(buildingPrefab, buildingSpot.transform.position, buildingSpot.transform.rotation);
        if(UnitSelectionManager.IsUnitSelected(buildingSpot.gameObject))
            FindFirstObjectByType<UnitSelectionManager>().SetUnitSelected(newBuilding);

        CleanUpPlacement(newBuilding, buildingSpot.unitTypeToBuild);
    }



    private void CleanUpPlacement(GameObject unitGO, PlayerUnitType unitType)
    {
        if(HexTileManager.IsTileAtHexLocation(unitGO.transform.position) || HexTileManager.IsOnTileEdge(unitGO.transform.position))
        {
            unitPlacementFinished?.Invoke();
            unitGO.transform.position = unitGO.transform.position;
            Unit unit = unitGO.GetComponent<Unit>();
            unitPlaced?.Invoke(unit);
            unitGO.GetComponent<IPlaceable>().Place();
            AddUnitToList(unit);
            if (unitGO.TryGetComponent(out BuildingSpotBehavior bsb))
            {
                buildingSpot = null;
                bool repeatPlacement = allowRepeatPlacement?.Invoke() ?? true;
                if (repeatPlacement && !Keyboard.current.shiftKey.isPressed)
                    StartCoroutine(AddUnitInOneFrame(bsb.unitTypeToBuild, allowRepeatPlacement));
            }
        }
        else
            RemoveBuilding(unitGO);
    }

    [Button]
    private bool? CanRepeat()
    {
        return allowRepeatPlacement?.Invoke();
    }

    private IEnumerator AddUnitInOneFrame(PlayerUnitType unitType, Func<bool> allowRepeatPlacement)
    {
        yield return null;
        GameObject buildingSpot = SetUnitTypeToAdd(unitType, allowRepeatPlacement);
        List<Hex3> locations = htm.GetEmptyNeighborLocations(HelperFunctions.GetMouseVector3OnPlane(true).ToHex3());
        if (locations.Count > 0)
            buildingSpot.transform.position = locations[0];
    }

    private void RemoveBuilding(PlayerUnit playerUnit)
    {
        if (buildingSpot == null)
            return;

        RemoveBuilding(buildingSpot.gameObject);//unit type doesn't matter.
    }
    private void RemoveBuilding(GameObject unitGO)
    {
        unitGO.gameObject.SetActive(false); //send back to pool
        buildingSpot = null;
        unitPlacementFinished?.Invoke();
    }

    private IEnumerator DelayRemoveBuilding(GameObject unitGO)
    {
        yield return null;
        unitGO.gameObject.SetActive(false); //send back to pool
        buildingSpot = null;
        unitPlacementFinished?.Invoke();
    }

    public void AddResourceDrop(PlayerUnit playerUnit)
    {
        AddUnitToList(playerUnit);
    }

    public void RemoveResourceDrop(PlayerUnit playerUnit)
    {
        RemoveUnit(playerUnit);
    }

    //Unit is added when placed. Not created!!!
    private void AddUnitToList(Unit unit)
    {
       if (unit is EnemyUnit enemyUnit && !enemyUnits.Contains(enemyUnit))
            enemyUnits.Add(enemyUnit);
        else if (unit is PlayerUnit playerUnit && !playerUnits.Contains(playerUnit))
        {
            playerUnits.Add(playerUnit);
            AddPlayerLocation(playerUnit);
            if(playerUnit.TryGetComponent(out UnitStorageBehavior storage) && !playerStorage.Contains(storage))
                playerStorage.Add(storage);
            UnitTypeAdded(playerUnit.unitType);
        }
    }

    private void AddPlayerLocation(PlayerUnit playerUnit)
    {
        if (playerUnit.GetComponent<UnitStorageBehavior>() == null)
            return;

        if(playerUnitLocations.ContainsKey(playerUnit.Location))
            playerUnitLocations[playerUnit.Location] = playerUnit;
        else
            playerUnitLocations.Add(playerUnit.Location, playerUnit);

        if(playerUnit.unitType == PlayerUnitType.orbitalBarge)
        {
            List<Hex3> locations = Hex3.GetNeighborsInRange(playerUnit.Location, 1);
            foreach (var location in locations)
            {
                playerUnitLocations.TryAdd(location, playerUnit);
            }
        }
        else if (playerUnit.unitType == PlayerUnitType.buildingSpot)
        {
            BuildingSpotBehavior buildingSpot = playerUnit.GetComponent<BuildingSpotBehavior>();
            if (buildingSpot.unitTypeToBuild != PlayerUnitType.orbitalBarge)
                return;
            
            List<Hex3> locations = Hex3.GetNeighborsInRange(playerUnit.Location, 1);
            foreach (var location in locations)
            {
                playerUnitLocations.TryAdd(location, playerUnit);
            }
        }

    }

    private void RemovePlayerLocation(PlayerUnit playerUnit)
    {
        if (playerUnitLocations.TryGetValue(playerUnit.Location, out PlayerUnit unit) && unit == playerUnit)
            playerUnitLocations.Remove(playerUnit.Location);
        else
            return;

        if (playerUnit.unitType == PlayerUnitType.orbitalBarge)
        {
            List<Hex3> locations = Hex3.GetNeighborsInRange(playerUnit.Location, 1);
            foreach (var location in locations)
            {
                if(playerUnitLocations.ContainsKey(location))
                    playerUnitLocations.Remove(location);
            }
        }
        else if (playerUnit.unitType == PlayerUnitType.buildingSpot)
        {
            BuildingSpotBehavior buildingSpot = playerUnit.GetComponent<BuildingSpotBehavior>();
            if (buildingSpot.unitTypeToBuild != PlayerUnitType.orbitalBarge)
                return;

            List<Hex3> locations = Hex3.GetNeighborsInRange(playerUnit.Location, 1);
            foreach (var location in locations)
            {
                if(playerUnitLocations.ContainsKey(location))
                    playerUnitLocations.Remove(location);
            }
        }
    }

    public void UnitMoved(Hex3 start, Hex3 end, PlayerUnit playerUnit)
    {
        if (playerUnitLocations.TryGetValue(start, out PlayerUnit unit) && unit == playerUnit)
            playerUnitLocations.Remove(start);

        playerUnitLocations.TryAdd(end, playerUnit);
    }

    private void UnitTypeAdded(PlayerUnitType playerUnitType)
    {
        buildMenu ??= FindObjectOfType<BuildMenu>();
        buildMenu.UnlockUnitType(playerUnitType);
    }

    private void RemoveUnit(Unit unit)
    {
        if (unit is EnemyUnit enemyUnit)
            enemyUnits.Remove(enemyUnit);
        else if (unit is PlayerUnit playerUnit)
        {
            playerUnits.Remove(playerUnit);
            RemovePlayerLocation(playerUnit);
            if (playerUnit.TryGetComponent(out UnitStorageBehavior storage) && playerStorage.Contains(storage))
                playerStorage.Remove(storage);
        }
    }

    public static bool UnitAtMouseLocation()
    {
        Hex3 position = HelperFunctions.GetMouseVector3OnPlane(true);

        foreach (var playerUnit in playerUnits)
        {
            if (TryGetPlayerUnitAtLocation(position, out PlayerUnit playerUnitAtLocation))
                return true;
        }

        foreach (var enemyUnit in enemyUnits)
        {
            if (enemyUnit.transform.position.ToHex3() == position)
                return true;
        }

        return false;
    }

   
    private void LoadPrefabByType()
    {
        //var buildPrefabs = LoadUnitPrefabs().ToList();
        var buildPrefabs = Resources.LoadAll("Complete Units", typeof(GameObject)).ToList();

        if (gameSettings.IsDemo)
        {
            for (int i = buildPrefabs.Count - 1; i >= 0; i--)
            {
                GameObject prefab = buildPrefabs[i] as GameObject;
                if (!gameSettings.DemoTypes.Contains(prefab.GetComponent<PlayerUnit>().unitType))
                    buildPrefabs.RemoveAt(i);
            }
        }

        foreach (var building in buildPrefabs)
        {
            GameObject prefab = building as GameObject;

            if (!prefabByType.ContainsKey(prefab.GetComponent<PlayerUnit>().unitType))    
                prefabByType.Add(prefab.GetComponent<PlayerUnit>().unitType, prefab);
            else
            {
                Debug.Log($"Duplicate building type {prefab.GetComponent<PlayerUnit>().unitType} found. {building.name}", building);
                GameObject duplicate = prefabByType[prefab.GetComponent<PlayerUnit>().unitType];
                Debug.Log($"Duplicate : {duplicate.name}", duplicate);
            }
        }
    }
    
    private void LoadPlaceHolderByType()
    {
        var placeHolders = Resources.LoadAll("Building Placeholders", typeof(GameObject)).ToList();

        if (gameSettings.IsDemo)
        {
            for (int i = placeHolders.Count - 1; i >= 0; i--)
            {
                GameObject prefab = placeHolders[i] as GameObject;
                if (!gameSettings.DemoTypes.Contains(prefab.GetComponent<UnitIdentifier>().unitType))
                    placeHolders.RemoveAt(i);
            }
        }

        foreach (var placeHolder in placeHolders)
        {
            GameObject prefab = placeHolder as GameObject;

            if (!placeHolderByType.ContainsKey(prefab.GetComponent<UnitIdentifier>().unitType))
                placeHolderByType.Add(prefab.GetComponent<UnitIdentifier>().unitType, prefab);
            else
                Debug.Log($"Duplicate placeholder building type {prefab.GetComponent<UnitIdentifier>().unitType} found.");
        }
    }

    /// <summary>
    /// Instantiates a build over time prefab by type and returns if it exists.
    /// </summary>
    /// <param name="unitType"></param>
    /// <returns></returns>
    public BuildOverTime GetBuildUpByType(PlayerUnitType unitType)
    {
        if(buildUpPrefabs.TryGetValue(unitType, out BuildOverTime buildOverTime))
            return Instantiate(buildOverTime);
        else return null;
    }

    
    private void LoadBuildUpByType()
    {
        var buildUpUnits = Resources.LoadAll("Build Up Units", typeof(GameObject)).ToList();

        if (gameSettings.IsDemo)
        {
            for (int i = buildUpUnits.Count - 1; i >= 0; i--)
            {
                GameObject prefab = buildUpUnits[i] as GameObject;
                if (!gameSettings.DemoTypes.Contains(prefab.GetComponent<UnitIdentifier>().unitType))
                    buildUpUnits.RemoveAt(i);
            }
        }

        foreach (var buildUp in buildUpUnits)
        {
            GameObject prefab = buildUp as GameObject;
            if (!buildUpPrefabs.ContainsKey(prefab.GetComponent<UnitIdentifier>().unitType))
                buildUpPrefabs.Add(prefab.GetComponent<UnitIdentifier>().unitType, prefab.GetComponent<BuildOverTime>());
            else
                Debug.Log($"Duplicate build up type {prefab.GetComponent<UnitIdentifier>().unitType} found.");
        }
    }

    [Button]
    public void GetAll()
    {
        //not currently used..
    }

    public GameObject InstantiateUnitByType(PlayerUnitType unitType, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = GetPrefabByType(unitType);
        if(prefab == null)
        {
            Debug.LogError($"No prefab found for {unitType}");
            return null;
        }
        GameObject newUnit = Instantiate(GetPrefabByType(unitType), position, rotation);
        Unit unit = newUnit.GetComponent<Unit>();
        unitPlaced?.Invoke(unit);
        newUnit.GetComponent<IPlaceable>().Place();
        AddUnitToList(unit);
        return newUnit;
    }
    
    public GameObject InstantiateUnitByType(PlayerUnitType unitType, Vector3 position)
    {
        return InstantiateUnitByType(unitType, position, Quaternion.identity);
    }

    private GameObject GetPrefabByType(PlayerUnitType unitType)
    {
        if(prefabByType.TryGetValue(unitType, out GameObject prefab))
            return prefab;
        return null;
    }

    private GameObject GetPlaceHolderByType(PlayerUnitType unitType)
    {
        if (placeHolderByType.TryGetValue(unitType, out GameObject prefab))
            return prefab;
        return null;
    }

    public static bool TryGetPlayerUnitAtLocation(Hex3 location, out PlayerUnit playerUnit)
    {
        playerUnitLocations.TryGetValue(location, out playerUnit);
        return playerUnit != null;
    }

    public static PlayerUnit PlayerUnitAtLocation(Hex3 location)
    {
        playerUnitLocations.TryGetValue(location, out PlayerUnit playerUnit);
        
        return playerUnit;
    }

    public static List<PlayerUnit> PlayerUnitsInRange(Hex3 center, int range)
    {
        List<PlayerUnit> playerUnits = new List<PlayerUnit>();
        List<Hex3> locations = Hex3.GetNeighborsInRange(center, range);
        foreach(Hex3 location in locations)
        {
            PlayerUnit playerUnit = PlayerUnitAtLocation(location);
            if(playerUnit != null)
                playerUnits.Add(playerUnit);
        }

        return playerUnits;
    }

    public static bool PlayerUnitAtMouseLocation()
    {
        return PlayerUnitAtLocation(HelperFunctions.GetMouseVector3OnPlane(true)) != null;
    }

    public static List<PlayerUnit> GetPlayerUnitByType(PlayerUnitType unitType)
    {
        return playerUnits.Where(x => x.unitType == unitType).ToList();
    }

    public List<ResourceAmount> GetUnitCost(PlayerUnitType unitType)
    {
        return buildingCosts.Where(x => x.unitType == unitType).FirstOrDefault()?.GetCosts();
    }

    public ResourceAmount GetUnitCost(PlayerUnitType unitType, ResourceType resourceType)
    {
        return GetUnitCost(unitType).FirstOrDefault(x => x.type == resourceType);
    }

    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this, 1);
    }


    private const string UNIT_SAVE_PATH = "UnitData";
    public void Save(string savePath, ES3Writer writer)
    {
        List<UnitData> data = new List<UnitData>();
        foreach (var playerUnit in playerUnits)
        {
            UnitData unitData = new UnitData();
            unitData.IsBuildingSpot = playerUnit.unitType == PlayerUnitType.buildingSpot;
            if(unitData.IsBuildingSpot)
                unitData.unitType = playerUnit.GetComponent<BuildingSpotBehavior>().unitTypeToBuild;
            else
                unitData.unitType = playerUnit.unitType;
            unitData.location = playerUnit.Location;
            unitData.rotation = playerUnit.transform.rotation;
            unitData.localStats = new Dictionary<Stat, float>(playerUnit.LocalStats);
            UnitStorageBehavior usb = playerUnit.GetComponent<UnitStorageBehavior>();
            unitData.storedResources = usb.GetResourcesToSave();

            if (playerUnit.unitType == PlayerUnitType.orbitalBarge)
            {

            }

            if(usb is TransportStorageBehavior tsb)
            {
                unitData.allowedTypes = tsb.AllowedTypes;
            }
            else if(usb is ShipStorageBehavior ssb)
            {
                unitData.allowedTypes = ssb.AllowedTypes;
            }

            unitData.connectionLocations = usb.GetConnectionLocations();

            if(playerUnit.TryGetComponent(out ResourceProductionBehavior production))
            {
                unitData.recipeStatus = production.GetRecipeStatus();
                unitData.recipeIndex = production.GetRecipeIndex();
            }

            data.Add(unitData);
        }

        writer.Write<List<UnitData>>(UNIT_SAVE_PATH, data);
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if (ES3.KeyExists(UNIT_SAVE_PATH, loadPath))
        {
            bool buildingLoadingComplete = false;
            PlayerResources playerResources = FindFirstObjectByType<PlayerResources>();
            List<UnitData> data = ES3.Load<List<UnitData>>(UNIT_SAVE_PATH, loadPath);
            for (int i = 0; i < data.Count; i++)
            {
                if (!prefabByType.ContainsKey(data[i].unitType))
                    continue;

                GameObject unitGO = null;
                if (data[i].IsBuildingSpot)
                {
                    BuildingSpotBehavior buildingSpot = buildingSpotPool.Pull().GetComponent<BuildingSpotBehavior>();
                    buildingSpot.SetTypeToBuild(data[i].unitType, GetPrefabByType(data[i].unitType), GetPlaceHolderByType(data[i].unitType), GetUnitCost(data[i].unitType));
                    buildingSpot.gameObject.name = $"{data[i].unitType} building spot";
                    unitGO = buildingSpot.gameObject;
                    unitGO.transform.position = data[i].location.ToVector3();
                    PlayerUnit unit = unitGO.GetComponent<PlayerUnit>();
                    unit.Place();
                    AddUnitToList(unit);
                }
                else
                    unitGO = InstantiateUnitByType(data[i].unitType, data[i].location.ToVector3(), data[i].rotation);

                if (unitGO == null)
                    continue;

                PlayerUnit playerUnit = unitGO.GetComponent<PlayerUnit>();
                playerUnit.LoadLocalStats(new Dictionary<Stat, float>(data[i].localStats));

                postUpdateMessage?.Invoke($"Building {playerUnit.unitType.ToNiceString()}");

                UnitStorageBehavior usb = unitGO.GetComponent<UnitStorageBehavior>();
                if (usb is TransportStorageBehavior tsb && data[i].allowedTypes != null)
                {
                    foreach (var type in data[i].allowedTypes)
                    {
                        tsb.AddAllowedResource(type);
                    }
                }

                if (playerUnit.unitType == PlayerUnitType.orbitalBarge)
                {
                    //we can't set inventory until the project has been set
                    //otherwise inventory doesn't load correctly due to "allowed types"
                    SpecialProjectManager spm = FindFirstObjectByType<SpecialProjectManager>();
                    List<ResourceAmount> inventory = new List<ResourceAmount>(data[i].storedResources);
                    spm.SetLiftInventory(() => usb.LoadStoredResources(inventory));
                }
                else if(playerUnit.unitType != PlayerUnitType.supplyShip)
                {
                    usb.LoadStoredResources(data[i].storedResources);
                    usb.CheckResourceLevels();
                    playerResources.AddResource(data[i].storedResources);
                }
                else
                {
                    if(usb is not ShipStorageBehavior ssb)
                        continue;

                    if (data[i].allowedTypes != null)
                    {
                        foreach (var type in data[i].allowedTypes)
                        {
                            ssb.AddAllowedResource(type);
                        }
                    }


                    ssb.LoadStoredResources(data[i].storedResources);
                    for (int j = 0; j < data[i].storedResources.Count; j++)
                    {
                        if (data[i].storedResources[j].type != ResourceType.Workers)
                        {
                            playerResources.AddResource(data[i].storedResources[j]);
                        }
                    }
                }



                if (playerUnit.TryGetComponent(out ResourceProductionBehavior production))
                {
                    if(data[i].recipeStatus != null && data[i].recipeStatus.Count > 0)
                        production.SetRecipeStatus(data[i].recipeStatus);
                    production.SetRecipeIndex(data[i].recipeIndex);
                }

                //load connection waits one frame to ensure all units are loaded
                usb.LoadConnections(data[i].connectionLocations, () => buildingLoadingComplete == true);

                yield return null;
            }
            buildingLoadingComplete = true;
        }
    }
    public static List<string> GetDataNames()
    {
        return new List<string>() { UNIT_SAVE_PATH };
    }

    [ShowInInspector]
    private static bool useLowCost = false;
    public static bool UseLowCost => useLowCost;


    [Button]
    private void ToggleLowCost()
    {
        useLowCost = !useLowCost;
    }

    public struct UnitData
    {
        public bool IsBuildingSpot;
        public PlayerUnitType unitType;
        public Hex3 location;
        public Quaternion rotation;
        public Dictionary<Stat, float> localStats;
        public List<ResourceAmount> storedResources;
        public List<Hex3> connectionLocations;
        public int recipeIndex;
        public List<bool> recipeStatus;
        public HashSet<ResourceType> allowedTypes;
    }

    private BuildCost[] LoadBuildCosts()
    {
        buildCostBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "unit build costs"));
        if(buildCostBundle == null)
        {
            Debug.LogError("No build cost asset bundle found.");
            return null;
        }

        return buildCostBundle.LoadAllAssets<BuildCost>();
    }
    
    private GameObject[] LoadUnitPrefabs()
    {
        completeUnitBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "complete units"));
        if(completeUnitBundle == null)
        {
            Debug.LogError("No unit prefab bundle found.");
            return null;
        }

        return completeUnitBundle.LoadAllAssets<GameObject>();
    }
    
    private GameObject[] LoadUnitPlaceholders()
    {
        unitPlaceholderBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "unit placeholders"));
        if(unitPlaceholderBundle == null)
        {
            Debug.LogError("No unit placeholder asset bundle found.");
            return null;
        }

        return unitPlaceholderBundle.LoadAllAssets<GameObject>();
    }
    
    private GameObject[] LoadUnitBuildups()
    {
        unitBuildUpBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "unit build ups"));
        if(unitBuildUpBundle == null)
        {
            Debug.LogError("No unit build up asset bundle found.");
            return null;
        }

        return unitBuildUpBundle.LoadAllAssets<GameObject>();
    }

    [Button]
    private void UnloadAllAssetBundles()
    {
        AssetBundle.UnloadAllAssetBundles(true);
    }
}

