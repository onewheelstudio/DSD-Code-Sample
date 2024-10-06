using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using HexGame.Resources;
using System;
using HexGame.Grid;
using Sirenix.OdinInspector;
using OWS.ObjectPooling;
using HexGame.Units;
using System.Linq;
using DG.Tweening;
using Nova;
using System.Collections;

public class UnitManager : MonoBehaviour
{
    public static event Action<Unit> unitPlaced;
    public static List<PlayerUnit> playerUnits = new List<PlayerUnit>();
    public static List<UnitStorageBehavior> playerStorage = new List<UnitStorageBehavior>();
    public static List<EnemyUnit> enemyUnits = new List<EnemyUnit>();

    private BuildingSpotBehavior buildingSpot;
    private bool allowRepeatPlacement = false;
    private Hex3 lastPosition;

    [SerializeField]
    private List<BuildCost> buildingCosts = new List<BuildCost>();
    [Required]
    [SerializeField]
    private List<GameObject> buildPrefabs;
    [SerializeField]
    private List<GameObject> placeHolders;
    [SerializeField]
    private List<UnitIdentifier> buildUpUnits = new List<UnitIdentifier>();
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

    private BuildMenu buildMenu;
    private HexTileManager htm;

    [Header("Warning")]
    public static ObjectPool<WarningIcons> warningIcons;
    [SerializeField] private GameObject warningIcon;

    private void Awake()
    {
        buildingSpotPool = new ObjectPool<PoolObject>(buildingSpotPrefab);
        warningIcons = new ObjectPool<WarningIcons>(warningIcon);
        CreatePrefabByType();
        CreatePlaceHolderByType();
        CreateBuildUpByType();
        playerUnits.Clear();
        playerStorage.Clear();
        enemyUnits.Clear();
        htm = FindObjectOfType<HexTileManager>();
    }

    private void OnEnable()
    {
        Unit.unitRemoved += RemoveUnit;
        BuildingSpotBehavior.buildingComplete += CleanUpBuilding;
        PlaceHolderTileBehavior.tileComplete += UpdateBuildingSpotValidity;
    }

    private void OnDisable()
    {
        Unit.unitRemoved -= RemoveUnit;
        BuildingSpotBehavior.buildingComplete -= CleanUpBuilding;
        PlaceHolderTileBehavior.tileComplete -= UpdateBuildingSpotValidity;
        DOTween.Kill(this,true);
    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame && buildingSpot != null)
            RemoveBuilding(buildingSpot.gameObject);

        if (Mouse.current.leftButton.wasPressedThisFrame && buildingSpot != null && !PCInputManager.MouseOverVisibleUIObject() && buildingSpot.IsValidPlacement())
        {
            //buildingSpot.transform.position = HelperFunctions.GetMouseVector3OnPlane(true);
            CleanUpPlacement(buildingSpot.gameObject, buildingSpot.GetComponent<PlayerUnit>().unitType);
        }
        else if (buildingSpot != null && !UnitAtMouseLocation())
        {
            //unitToAdd.transform.position = HelperFunctions.GetMouseVector3OnPlane(true);
            buildingSpot.transform.DOMove(HelperFunctions.GetMouseVector3OnPlane(true), 0.1f);
            HexTile tile = HexTileManager.GetHexTileAtLocation(buildingSpot.transform.position);

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

    public GameObject SetUnitTypeToAdd(PlayerUnitType unitType, bool repeatPlacement = false)
    {
        if (buildingSpot != null)
            buildingSpot.gameObject.SetActive(false);

        if(!allowBuildingWithoutResources && !HasResourcesForUnit(unitType))
        {
            MessagePanel.ShowMessage($"Not enough resources to build {unitType.ToNiceString()}", null);
            return null;
        }

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
        GameObject newBuilding = Instantiate(buildingPrefab, buildingSpot.transform.position.ToHex3(), buildingSpot.transform.rotation);
        if(UnitSelectionManager.IsUnitSelected(buildingSpot.gameObject))
            FindObjectOfType<UnitSelectionManager>().SetUnitSelected(newBuilding);

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
                if (allowRepeatPlacement && Keyboard.current.shiftKey.isPressed)
                    StartCoroutine(AddUnitInOneFrame(bsb.unitTypeToBuild, allowRepeatPlacement));
            }
        }
        else
            RemoveBuilding(unitGO);
    }

    private IEnumerator AddUnitInOneFrame(PlayerUnitType unitType, bool allowRepeatPlacement)
    {
        yield return null;
        GameObject buildingSpot = SetUnitTypeToAdd(unitType, allowRepeatPlacement);
        List<Hex3> locations = htm.GetEmptyNeighborLocations(HelperFunctions.GetMouseVector3OnPlane(true).ToHex3());
        if (locations.Count > 0)
            buildingSpot.transform.position = locations[0];
    }


    private void RemoveBuilding(GameObject unitGO)
    {
        unitPlacementFinished?.Invoke();
        //MessagePanel.ShowMessage($"Can't place {unitGO.name} there.", unitGO);
        unitGO.gameObject.SetActive(false); //send back to pool
        buildingSpot = null;
    }

    /// <summary>
    /// Checks if object is on an edge and snaps to location.
    /// Calls Place on IPlaceable
    /// </summary>
    /// <param name="unitGO"></param>
    private void CleanUpEdgePlacement(GameObject unitGO)
    {
        Hex3 hexLocation = Hex3.Vector3ToHex3(unitGO.transform.position);
        if (HexTileManager.IsOnTileEdge(unitGO.transform.position))
        {
            unitPlacementFinished?.Invoke();
            Unit unit = unitGO.GetComponent<Unit>();
            unitPlaced?.Invoke(unit);
            unitGO.GetComponent<IPlaceable>().Place();
            AddUnitToList(unit);
            buildingSpot = null;
        }
        else
            RemoveBuilding(unitGO);
    }

    //Unit is added when placed. Not created!!!
    private void AddUnitToList(Unit unit)
    {
       if (unit is EnemyUnit enemyUnit && !enemyUnits.Contains(enemyUnit))
            enemyUnits.Add(enemyUnit);
        else if (unit is PlayerUnit playerUnit && !playerUnits.Contains(playerUnit))
        {
            playerUnits.Add(playerUnit);
            if(playerUnit.TryGetComponent(out UnitStorageBehavior storage) && !playerStorage.Contains(storage))
                playerStorage.Add(storage);
            UnitTypeAdded(playerUnit.unitType);
        }
    }

    private void UnitTypeAdded(PlayerUnitType playerUnitType)
    {
        buildMenu ??= FindObjectOfType<BuildMenu>();
        buildMenu.UnitTypeAdded(playerUnitType);
    }

    private void RemoveUnit(Unit unit)
    {
        if (unit is EnemyUnit enemyUnit)
            enemyUnits.Remove(enemyUnit);
        else if (unit is PlayerUnit playerUnit)
        {
            playerUnits.Remove(playerUnit);
            if (playerUnit.TryGetComponent(out UnitStorageBehavior storage) && !playerStorage.Contains(storage))
                playerStorage.Remove(storage);
        }
    }

    public static bool UnitAtMouseLocation()
    {
        Hex3 position = HelperFunctions.GetMouseVector3OnPlane(true);

        foreach (var playerUnit in playerUnits)
        {
            if (playerUnit.transform.position.ToHex3() == position)
                return true;
        }

        foreach (var enemyUnit in enemyUnits)
        {
            if (enemyUnit.transform.position.ToHex3() == position)
                return true;
        }

        return false;
    }

    private void CreatePrefabByType()
    {
        foreach (var building in buildPrefabs)
        {
            if (!prefabByType.ContainsKey(building.GetComponent<PlayerUnit>().unitType))    
                prefabByType.Add(building.GetComponent<PlayerUnit>().unitType, building);
            else
                Debug.Log($"Duplicate building type {building.GetComponent<PlayerUnit>().unitType} found.");
        }
    }

    private void CreatePlaceHolderByType()
    {
        foreach (var placeHolder in placeHolders)
        {
            if (!placeHolderByType.ContainsKey(placeHolder.GetComponent<UnitIdentifier>().unitType))
                placeHolderByType.Add(placeHolder.GetComponent<UnitIdentifier>().unitType, placeHolder);
            else
                Debug.Log($"Duplicate building type {placeHolder.GetComponent<UnitIdentifier>().unitType} found.");
        }
    }

    /// <summary>
    /// Instantiates a build over time prefab by type and returns if it exists.
    /// </summary>
    /// <param name="unitType"></param>
    /// <returns></returns>
    public BuildOverTime GetBuildOverTimeByType(PlayerUnitType unitType)
    {
        if(buildUpPrefabs.TryGetValue(unitType, out BuildOverTime buildOverTime))
            return Instantiate(buildOverTime);
        else return null;
    }
    
    private void CreateBuildUpByType()
    {
        foreach (var buildUp in buildUpUnits)
        {
            if (!buildUpPrefabs.ContainsKey(buildUp.unitType))
                buildUpPrefabs.Add(buildUp.unitType, buildUp.GetComponent<BuildOverTime>());
            else
                Debug.Log($"Duplicate building type {buildUp.unitType} found.");
        }
    }

    [Button]
    private void RefreshBuidlingPrefabs()
    {
        buildPrefabs = HelperFunctions.GetPrefabs("Assets/Prefabs/Units/Player/Complete Units/");
    }

    [Button]
    private void RefreshPlaceHolderPrefabs()
    {
        placeHolders = HelperFunctions.GetPrefabs("Assets/Prefabs/Units/Player/Building Placeholders/");
    }
    
    [Button]
    private void GetBuildCosts()
    {
        buildingCosts = HelperFunctions.GetScriptableObjects<BuildCost>("Assets/Prefabs/Units/Player/Build Costs/");
    }

    public GameObject InstantiateUnitByType(PlayerUnitType unitType, Vector3 position)
    {
        GameObject newUnit = Instantiate(GetPrefabByType(unitType), position, Quaternion.identity);
        Unit unit = newUnit.GetComponent<Unit>();
        unitPlaced?.Invoke(unit);
        newUnit.GetComponent<IPlaceable>().Place();
        AddUnitToList(unit);
        return newUnit;
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

    public static List<PlayerUnit> AllPlayerUnitsAtLocation(Hex3 location)
    {
        List<PlayerUnit> _playerUnits = new List<PlayerUnit>();
        foreach (var playerUnit in playerUnits)
        {
            if (playerUnit.transform.position.ToHex3() == location)
                _playerUnits.Add(playerUnit);
        }

        return _playerUnits;
    }
    public static bool TryGetAllPlayerUnitsAtLocation(Hex3 location, out List<PlayerUnit> playerUnits)
    {
        playerUnits = AllPlayerUnitsAtLocation(location);
        return playerUnits.Count > 0;
    }

    public static PlayerUnit PlayerUnitAtLocation(Hex3 location)
    {
        foreach (var playerUnit in playerUnits)
        {
            if (playerUnit == null)
                continue;

            if (playerUnit.Location == location)
                return playerUnit;
        }

        return null;
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
    public static bool TryGetPlayerUnitAtLocation(Hex3 location, out PlayerUnit playerUnit)
    {
        playerUnit = PlayerUnitAtLocation(location);
        return playerUnit != null;
    }

    public static List<PlayerUnit> GetPlayerUnitByType(PlayerUnitType unitType)
    {
        return playerUnits.Where(x => x.unitType == unitType).ToList();
    }

    public List<ResourceAmount> GetUnitCost(PlayerUnitType unitType)
    {
        return buildingCosts.Where(x => x.unitType == unitType).FirstOrDefault()?.costs;
    }

    public ResourceAmount GetUnitCost(PlayerUnitType unitType, ResourceType resourceType)
    {
        return GetUnitCost(unitType).FirstOrDefault(x => x.type == resourceType);
    }

    private bool HasResourcesForUnit(PlayerUnitType unitType)
    {
        foreach (var cost in GetUnitCost(unitType))
        {
            if (!PlayerResources.HasResource(cost))
            {
                MessagePanel.ShowMessage(GenerateNeedResourceMessage(unitType), null);
                return false;
            }
        }
        return true;
    }

    private string GenerateNeedResourceMessage(PlayerUnitType unitType)
    {
        string message = $"Need ";
        foreach (var cost in GetUnitCost(unitType))
        {
            message += $"{cost.amount} {cost.type}, ";
        }
        message += $"to build {unitType}.";

        return message;
    }

}

