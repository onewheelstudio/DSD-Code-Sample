using HexGame.Resources;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System;
using HexGame.Grid;
using NovaSamples.UIControls;
using UnityEngine.PlayerLoop;
using static UnityEngine.Rendering.DebugUI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Manageable]
public class HexTileManager : SerializedMonoBehaviour, ISaveData
{
    public static event System.Action fillComplete;
    public static event System.Action nextTilePlaced;

    [SerializeField]
    private Dictionary<HexTileType, GameObject> tilePrefabs = new Dictionary<HexTileType, GameObject>();
    [SerializeField]
    private Dictionary<HexTileType, List<GameObject>> tileVariants = new Dictionary<HexTileType, List<GameObject>>();
    [SerializeField]
    private Dictionary<HexTileType, GameObject> placeHolderTilePrefabs = new Dictionary<HexTileType, GameObject>();

    private GameObject nextTileToAdd;
    public static event Action tilePlacementStarted;
    public static event Action tilePlacementCompleted;
    public static event Action<HexTileType> tileAdded;
    private static Dictionary<Hex3, HexTile> hexTiles = new Dictionary<Hex3, HexTile>();
    private Dictionary<Hex3, FogTile> fogTiles = new Dictionary<Hex3, FogTile>();
    private Dictionary<Hex3, GameObject> borderTiles = new Dictionary<Hex3, GameObject>();
    private static Dictionary<Hex3, List<FogRevealer>> revealedLocations = new Dictionary<Hex3, List<FogRevealer>>();

    private HexTileType nextTileType;
    private HexTile nextTile;
    public bool IsPlacingTile => nextTile != null;
    [BoxGroup("Tile Placement Settings")]
    [SerializeField]
    private bool requireNeighbors = true;
    [SerializeField]
    [BoxGroup("Tile Placement Settings")]
    private bool fillNeighbors = true;

    private CameraControlActions cameraControls;

    [Range(10f, 50f)]
    [SerializeField]
    [BoxGroup("Tile Placement Settings")]
    private float smoothSpeed = 20f;

    private int numToFill;
    [SerializeField]
    [Range(0f, 0.2f)]
    [BoxGroup("Tile Placement Settings")]
    private float fillDelay = 0.05f;

    [SerializeField]
    [BoxGroup("Tile Placement Settings")]
    private int randomizeSeed;
    public int RandomizeSeed
    {
        get
        {
            if (randomizeSeed == 0)
                randomizeSeed = UnityEngine.Random.Range(0, int.MaxValue);
            return randomizeSeed;
        }
    }
    private static System.Random externalRandom;
    private static System.Random random;
    private Transform tileParent;

    [Title("Fog")]
    public FogTile fogTilePrefab;
    public List<GameObject> borderTilePrefabs = new List<GameObject>();
    public GameObject borderTilePrefab => borderTilePrefabs[random.Next(0, borderTilePrefabs.Count)];

    [Header("Debug")]
    [SerializeField] private bool debug = false;

    private void Awake()
    {
        if (random == null)
            random = new System.Random(RandomizeSeed);

        cameraControls = new CameraControlActions();
        numToFill = random.Next(2, 12);
        tileParent = new GameObject("TileParent").transform;
        RegisterDataSaving();
    }

    private void OnEnable()
    {
        hexTiles.Clear();
        revealedLocations.Clear();
        HexTile.NewHexTile += AddHexToDictionary;
        //HexTile.HexTileRemoved += RemoveHexToDictionary;
        PlaceHolderTileBehavior.tileComplete += CompletePlaceHolder;
        cameraControls.PointerInput.Enable();
        LandmassGenerator.generationComplete += CheckTiles;
        WindowPopup.SomeWindowOpened += ForceOff;
    }

    private void OnDisable()
    {
        HexTile.NewHexTile -= AddHexToDictionary;
        //HexTile.HexTileRemoved -= RemoveHexToDictionary;
        PlaceHolderTileBehavior.tileComplete -= CompletePlaceHolder;
        cameraControls.PointerInput.Disable();
        LandmassGenerator.generationComplete -= CheckTiles;
        WindowPopup.SomeWindowOpened -= ForceOff;
    }


    private void CheckTiles()
    {
        List<Hex3> locationsToRemove = new List<Hex3>();
        List<HexTile> tilesToMove = new List<HexTile>();

        foreach (Hex3 location in hexTiles.Keys)
        {
            if (hexTiles[location].hexPosition != location)
            {
                tilesToMove.Add(hexTiles[location]);
                locationsToRemove.Add(location);
                continue;
            }
        }

        foreach (var location in locationsToRemove)
            hexTiles.Remove(location);

        foreach (var tile in tilesToMove)
        {
            if (hexTiles[tile.hexPosition] == tile)
                continue;

            if (hexTiles.ContainsKey(tile.hexPosition))
            {
                tile.gameObject.SetActive(false);
                continue;
            }

            if (fogTiles.TryGetValue(tile.hexPosition, out FogTile fogTile))
            {
                fogTile.gameObject.SetActive(false);
                fogTiles.Remove(tile.hexPosition);
                //Destroy(fogTile.gameObject);
            }

            hexTiles.Add(tile.hexPosition, tile);
        }
    }

    private void Update()
    {

#if UNITY_EDITOR
        if (debug)
        {
            Hex3 location = HelperFunctions.GetMouseHex3OnPlane();
            Debug.Log(location);

            if (hexTiles.TryGetValue(location, out HexTile hextile) && hextile != null)
            {
                Selection.activeGameObject = hextile.gameObject;
            }
        }
#endif

        if (nextTile != null)
            nextTile.transform.position = Vector3.Slerp(nextTile.transform.position,
                                            HelperFunctions.GetMouseVector3OnPlane(true),
                                            smoothSpeed * Time.deltaTime);

        if (nextTile != null
            && Mouse.current.leftButton.wasPressedThisFrame
            && !PCInputManager.MouseOverVisibleUIObject())
        {
            if (CanAddTileAtLocation(HelperFunctions.GetMouseHex3OnPlane(), true))
                nextTilePlaced?.Invoke();
            else
                return;


            if (fillNeighbors)
                FillTiles(HelperFunctions.GetMouseHex3OnPlane(), nextTileType, nextTile.isPlaceHolder);
            else
                TryPlaceTileAtMouse(nextTile);

        }
        else if(nextTile != null)
        {
            Hex3 location = HelperFunctions.GetMouseHex3OnPlane();
            //checking for location reveled in CanAddTileAtLocation breaks landmass generation
            bool canPlace = CanAddTileAtLocation(location, true) && IsLocationRevealed(location);
            nextTile.PlaceHolderBehavior.UpdateLocationValidity(canPlace);
        }

        if (nextTile != null && (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            DestroyTile();
        }
    }

    private async Awaitable DestroyTile()
    {
        await Awaitable.NextFrameAsync();
        Destroy(nextTile.gameObject);
        tilePlacementCompleted?.Invoke();
    }

    private void ForceOff(WindowPopup popup)
    {
        if (popup is InfoToolTipWindow || nextTile == null)
            return;

        Destroy(nextTile.gameObject);
        tilePlacementCompleted?.Invoke();
    }


    public void SetNextTile(HexTileType tileType, bool isPlaceHolder = false)
    {
        if (nextTile != null)
            return;

        nextTileType = tileType;
        nextTileToAdd = GetNextHexTile(tileType, isPlaceHolder).gameObject;
        nextTile = nextTileToAdd.GetComponent<HexTile>();
        tilePlacementStarted?.Invoke();
    }

    private HexTile GetNextHexTile(HexTileType tileType, bool isPlaceHolder)
    {
        if (isPlaceHolder && placeHolderTilePrefabs.TryGetValue(tileType, out GameObject placeHolderPrefab))
            return Instantiate(placeHolderPrefab).GetComponent<HexTile>();
        else if (tileVariants.TryGetValue(tileType, out List<GameObject> tileVariantList))
        {
            GameObject tile = tileVariantList[random.Next(0, tileVariantList.Count)];
            return Instantiate(tile).GetComponent<HexTile>();
        }
        else if (tilePrefabs.TryGetValue(tileType, out GameObject tilePrefab))
            return Instantiate(tilePrefab).GetComponent<HexTile>();

        Debug.LogError($"Could not find tile of type {tileType} - this should not happen!!");
        return null;
    }

    /// <summary>
    /// Places tile regardless of if there is another tile at that location.
    /// </summary>
    /// <param name="location"></param>
    /// <param name="tileType"></param>
    public bool PlaceAtLocation(Hex3 location, HexTileType tileType)
    {
        if (hexTiles.TryGetValue(location, out HexTile hexTile) && !hexTile.isPlaceHolder)
            return false;

        hexTile = GetNextHexTile(tileType, false);
        if (!TryReplaceHex(location, hexTile))
        {
            Debug.LogError($"Couldn't place {hexTile.TileType} at {location}.");
            Destroy(hexTile.gameObject); // this shouldn't happen
            return true;
        }

        return true;
    }


    [Button] ///ODIN!!!
    private void UpdateTiles()
    {
        tilePrefabs.Clear();
        placeHolderTilePrefabs.Clear();

        foreach (var tile in HelperFunctions.GetTiles("Assets/Prefabs/Tiles/", false))
        {
            tilePrefabs.Add(tile.GetComponent<HexTile>().TileType, tile);
        }
        foreach (var tile in HelperFunctions.GetTiles("Assets/Prefabs/Tiles/", true))
        {
            placeHolderTilePrefabs.Add(tile.GetComponent<HexTile>().TileType, tile);
        }
    }

    private bool TryPlaceTileAtMouse(HexTile hexTile)
    {
        Hex3 tileLocation = HelperFunctions.GetMouseHex3OnPlane();

        return TryPlaceTile(tileLocation, hexTile);
    }

    /// <summary>
    /// Used to place tiles connected to other tiles.
    /// </summary>
    /// <param name="location"></param>
    /// <param name="hexTile"></param>
    /// <param name="rotate"></param>
    /// <returns></returns>
    private bool TryPlaceTile(Hex3 location, HexTile hexTile, bool requireNeighbor = true, bool rotate = true)
    {
        if (!CanAddTileAtLocation(location, requireNeighbor))
            return false;

        if (!TryAddHexToDictionary(location, hexTile))
            return false;
        hexTile.transform.position = Hex3.Hex3ToVector3(location);
        if (rotate)
            hexTile.Rotate(random.Next(0, 6));
        hexTile.PlaceHex();
        hexTile.transform.SetParent(tileParent);
        if (!Keyboard.current.shiftKey.isPressed && nextTile && nextTile.isPlaceHolder) //allows for placing multiple tiles in a row
        {
            nextTileToAdd = Instantiate(nextTileToAdd);
            nextTile = nextTileToAdd.GetComponent<HexTile>();
        }
        else
        {
            nextTile = null;
            tilePlacementCompleted?.Invoke();
        }
        tileAdded?.Invoke(hexTile.TileType);

        return true;
    }

    private void CompletePlaceHolder(PlaceHolderTileBehavior behavior, HexTileType tileType)
    {
        HexTile tile = GetNextHexTile(tileType, false);
        TryReplaceHex(behavior.transform.position, tile);
        tile.transform.position = behavior.transform.position;
        tile.transform.rotation = behavior.transform.rotation;
        tile.PlaceHex();
        if (NumberOfRevealersAtLocation(behavior.transform.position) == 0)
            tile.FogTile.MoveToPlacedConfiguration();
        //tile.FogTile.MoveToRevealed(); //reveal since we just built it!!

        if (revealedLocations.TryGetValue(behavior.transform.position.ToHex3(), out List<FogRevealer> fogRevealers))
            tile.FogTile?.AddAgents(fogRevealers);
    }

    /// <summary>
    /// Used to Seed different islands of hex tiles. No Neighbors required.
    /// </summary>
    /// <param name="location"></param>
    /// <param name="type"></param>
    /// <param name="rotate"></param>
    /// <returns></returns>
    public bool TrySeedTile(Hex3 location, HexTileType type, bool rotate = true)
    {
        if (hexTiles.ContainsKey(location)) //double checks availablity to avoid instantiating extra tile
            return false;
        else
            return TryPlaceTile(location, GetNextHexTile(type, false), false);
    }

    private void FillTiles(Hex3 location, HexTileType tileType, bool isPlaceHolder)
    {
        StartCoroutine(FillOverTime(location, tileType, isPlaceHolder));
    }

    IEnumerator FillOverTime(Hex3 location, HexTileType tileType, bool isPlaceHolder)
    {
        SFXManager.PlaySFX(SFXType.tilePlace);

        foreach (var tile in GetTilesToFill(location, tileType, isPlaceHolder))
        {
            if (tile != null)
            {
                tile.gameObject.SetActive(true);
                tile.PlaceHex();
            }
            SFXManager.PlaySFX(SFXType.tilePlace);
            yield return new WaitForSeconds(fillDelay);
        }
        fillComplete?.Invoke();
    }

    public List<HexTile> GetTilesToFill(Hex3 location, HexTileType tileType, bool isPlaceHolder, int maxValue = 0)
    {
        //control how many tiles are placed by the player
        if (isPlaceHolder)
            return GetTilesToFill(location, tileType, 0, isPlaceHolder);

        if (tileType == HexTileType.feOre || tileType == HexTileType.alOre || tileType == HexTileType.gas)
            numToFill = random.Next(1, 3);
        else if (tileType == HexTileType.water)
            numToFill = random.Next(3, 8);
        else if (tileType == HexTileType.grass)
            numToFill = random.Next(5, 10);
        else if (tileType == HexTileType.mountain)
            numToFill = random.Next(2, 5);
        else
            numToFill = random.Next(2, 8);

        if (maxValue > 0)
            numToFill = Mathf.Min(numToFill, maxValue);

        return GetTilesToFill(location, tileType, numToFill, isPlaceHolder);
    }

    private List<HexTile> GetTilesToFill(Hex3 location, HexTileType tileType, int numToFill, bool isPlaceHolder)
    {
        List<Hex3> emptyNeighbors = GetEmptyNeighborLocations(location);
        List<HexTile> placedTiles = new List<HexTile>();
        if (nextTile == null)
            nextTile = GetNextHexTile(tileType, isPlaceHolder);

        if (!TryPlaceTile(location, nextTile))
            Destroy(nextTile.gameObject);

        int neighbor;
        Hex3 neighborToFill;
        HexTile tileToPlace;

        for (int i = 0; i < numToFill; i++)
        {
            if (emptyNeighbors.Count == 0)
            {
                emptyNeighbors = GetEmptyNeighborLocations(placedTiles);
                if (emptyNeighbors.Count == 0)
                    break;
            }

            neighbor = random.Next(0, emptyNeighbors.Count);
            neighborToFill = emptyNeighbors[neighbor];
            emptyNeighbors.RemoveAt(neighbor);

            if (CanAddTileAtLocation(neighborToFill))
            {
                tileToPlace = GetNextHexTile(tileType, isPlaceHolder);

                if (TryPlaceTile(neighborToFill, tileToPlace))
                {
                    placedTiles.Add(tileToPlace);
                    tileToPlace.gameObject.SetActive(false);
                }
                else
                    Destroy(tileToPlace.gameObject);
            }
        }

        return placedTiles;
    }

    public void BuildCluster(Hex3 startLocation, int numberOfTiles)
    {
        //avoid areas that are already built up
        //this was causing an error if not checked
        if (GetEmptyNeighborLocations(startLocation).Count == 0)
            return;

        TrySeedTile(startLocation, HexTileType.grass);
        Hex3 nextLocation = GetRandomEmptyNeighbor(startLocation);

        List<HexTile> placedTiles = GetTilesToFill(nextLocation, HexTileType.grass, GetNextInt_Internal(1, numberOfTiles + 1), false);

        List<Hex3> nextLocationList = GetEmptyNeighborLocations(placedTiles);
        if (nextLocationList.Count == 0)
        {
            Debug.LogError($"No empty neighbors for {nextLocation} with start at {startLocation}");
            return;
        }

        placedTiles.AddRange(GetTilesToFill(nextLocationList[GetNextInt_Internal(0,
                                            nextLocationList.Count)],
                                            HexTileType.forest,
                                            GetNextInt_Internal(0, numberOfTiles),
                                            false));

        nextLocationList = GetEmptyNeighborLocations(placedTiles);
        if (nextLocationList.Count > 0)
        {
            List<HexTile> tilesToAdd = GetTilesToFill(nextLocationList[GetNextInt_Internal(0,
                                                        nextLocationList.Count)],
                                                        HexTileType.mountain,
                                                        GetNextInt_Internal(0, numberOfTiles),
                                                        false);
            if (tilesToAdd.Count > 0)
                placedTiles.AddRange(tilesToAdd);
        }

        foreach (var tile in placedTiles)
        {
            tile.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Can also use GetEmptyNeightLocations and check the count of the returned list
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public bool HasEmptyNeighborLocations(Hex3 location)
    {
        return GetEmptyNeighborLocations(location).Count > 0;
    }

    /// <summary>
    /// returns locations of neighbors where tiles have been placed
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public static List<Hex3> GetFilledNeighborLocations(Hex3 location)
    {
        List<Hex3> filledNeighbors = new List<Hex3>();
        foreach (var neighbor in Hex3.GetNeighborLocations(location))
        {
            if (hexTiles.ContainsKey(neighbor))
                filledNeighbors.Add(neighbor);
        }

        return filledNeighbors;
    }
    
    public static List<Hex3> GetFilledNeighborLocations(Hex3 location, int range)
    {
        List<Hex3> filledNeighbors = new List<Hex3>();
        foreach (var neighbor in Hex3.GetNeighborsInRange(location, range))
        {
            if (hexTiles.ContainsKey(neighbor))
                filledNeighbors.Add(neighbor);
        }

        return filledNeighbors;
    }
    
    public static List<HexTile> GetTilesAtLocations(List<Hex3> locations)
    {
        List<HexTile> tiles = new List<HexTile>();

        foreach (var neighbor in locations)
        {
            if (hexTiles.TryGetValue(neighbor, out HexTile hexTile))
                tiles.Add(hexTile);
        }

        return tiles;
    }

    /// <summary>
    /// returns locations of neighbors where tiles have NOT been placed.
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public List<Hex3> GetEmptyNeighborLocations(Hex3 location)
    {
        List<Hex3> emptyNeighbors = new List<Hex3>();
        foreach (var neighbor in Hex3.GetNeighborLocations(location))
        {
            if (!hexTiles.ContainsKey(neighbor))
                emptyNeighbors.Add(neighbor);
        }

        return emptyNeighbors;
    }

    public Hex3 GetRandomEmptyNeighbor(Hex3 location)
    {
        List<Hex3> locations = GetEmptyNeighborLocations(location);

        if (locations.Count == 0)
            return location;

        return locations[random.Next(0, locations.Count)];
    }

    public static List<Hex3> GetEmptyNeighborLocations(List<HexTile> locations)
    {
        List<Hex3> emptyNeighbors = new List<Hex3>();
        foreach (var location in locations)
        {
            foreach (var neighbor in Hex3.GetNeighborLocations(location.hexPosition))
            {
                if (!hexTiles.ContainsKey(neighbor) && !emptyNeighbors.Contains(neighbor))
                    emptyNeighbors.Add(neighbor);
            }
        }

        return emptyNeighbors;
    }

    public static Hex3 GetEmptyLocationNextTo(HexTileType tileType)
    {
        List<HexTile> tilesOfType = GetAllTilesOfType(tileType);

        return GetEmptyNeighborLocations(tilesOfType)[random.Next(0, tilesOfType.Count)];
    }

    public static Hex3 GetEmptyLocationNextTo(HexTileType tileType, Hex3 center, int radius)
    {
        List<HexTile> tilesOfType = GetAllTilesOfType(tileType);
        tilesOfType = tilesOfType.Where(x => Hex3.DistanceBetween(center, x.hexPosition) <= radius).ToList();

        if (tilesOfType.Count == 0)
        {
            Debug.Log($"Failed to find {tileType} within {radius} of {center}");
            return GetEmptyLocationNextTo(tileType);
        }

        List<Hex3> locations = GetEmptyNeighborLocations(tilesOfType);

        if (locations != null && locations.Count > 0)
            return locations[random.Next(0, locations.Count)];
        else
            return Hex3.Zero;
    }

    public static List<Hex3> GetAllEmptyTiles(int minRange, int maxRange)
    {
        List<HexTile> tiles = new List<HexTile>();
        foreach (var tile in hexTiles.Values)
        {
            if(tile.hexPosition.Max() >= minRange && tile.hexPosition.Max() <= maxRange)
                tiles.Add(tile);
        }

        return GetEmptyNeighborLocations(tiles);
    }

    public static List<HexTile> GetAllTilesOfType(HexTileType tileType)
    {
        List<HexTile> tilesOfType = new List<HexTile>();
        foreach (var tile in hexTiles.Values)
        {
            if (tile.TileType == tileType)
                tilesOfType.Add(tile);
        }

        return tilesOfType;
    }

    public static List<HexTile> GetAllTilesOfType(HexTileType tileType, List<Hex3> locations)
    {
        List<HexTile> tilesOfType = new List<HexTile>();
        foreach (var location in locations)
        {
            if (hexTiles.TryGetValue(location, out HexTile hexTile) && hexTile.TileType == tileType)
                tilesOfType.Add(hexTile);
        }
        return tilesOfType;
    }

    public static List<HexTile> GetAllRevealedTilesOfTYpe(HexTileType tileType)
    {
        List<Hex3> locations = revealedLocations.Keys.ToList();
        return GetAllTilesOfType(tileType, locations);
    }

    public bool CanAddTileAtLocation(Hex3 hex, bool requireNeighbor = true)
    {
        if (hexTiles.Count == 0)
            return true;
        if (hex.Max() > LandmassGenerator.globalSize)
            return false;
        else if (hexTiles.ContainsKey(hex))
            return false;
        else if (requireNeighbor && !Hex3.HasNeighbor(hex, hexTiles))
            return false;
        else
            return true;
    }

    public static bool IsTileAtHexLocation(Hex3 location)
    {
        return hexTiles.ContainsKey(location);
    }

    public static bool IsTileAtHexLocation(Hex3 location, out HexTile hexTile)
    {
        return hexTiles.TryGetValue(location, out hexTile);
    }

    public static bool IsOnTileEdge(Vector3 position)
    {
        if (IsTileAtHexLocation(position))
            return true;

        List<Hex3> neighbors = Hex3.GetNeighborLocations(position);

        foreach (var neighbor in neighbors)
        {
            foreach (var edge in Hex3.GetEdgeMidPoints(neighbor))
            {
                if ((edge - position).sqrMagnitude < 0.1f)
                    return true;
            }
        }

        return false;
    }

    public static HexTile GetHexTileAtLocation(Hex3 location)
    {
        hexTiles.TryGetValue(location, out HexTile hexTile);
        return hexTile;
    }

    private void AddHexToDictionary(HexTile hexTile)
    {
        TryAddHexToDictionary(hexTile.hexPosition, hexTile);
    }

    private void RemoveHexToDictionary(HexTile hexTile)
    {
        hexTiles.Remove(hexTile.hexPosition);
    }

    public void NukeTile(HexTile hexTile)
    {
        if ((hexTile.TileType == HexTileType.mountain
            || hexTile.TileType == HexTileType.forest
            || hexTile.TileType == HexTileType.water)
            && GetNextInt_Internal(0, 100) > 40)
        {
            ReplaceHexTile(hexTile, HexTileType.grass);
        }
        else if (hexTile.TileType == HexTileType.grass && GetNextInt_Internal(0, 100) > 40)
        {
            RemoveHexToDictionary(hexTile);
            hexTile.DestroyTile();
        }
        else if (hexTile.TileType == HexTileType.feOre)
        {
            ReplaceHexTile(hexTile, HexTileType.grass);
            Debug.Log("Boom!");
        }
    }

    private void ReplaceHexTile(HexTile tileToReplace, HexTileType typeToAdd)
    {


        HexTile replacement;
        replacement = GetNextHexTile(typeToAdd, false);
        replacement.transform.position = tileToReplace.transform.position;
        replacement.AdjustPosition();
        RemoveHexToDictionary(tileToReplace);
        tileToReplace.DestroyTile();
        AddHexToDictionary(replacement);
    }

    private bool TryAddHexToDictionary(Hex3 location, HexTile hexTile)
    {
        hexTile.transform.position = location;
        hexTile.AdjustPosition();

        if (!hexTiles.ContainsKey(location))
        {
            if (fogTiles.TryGetValue(location, out FogTile fogTile) && fogTile != null)
            {
                fogTiles.Remove(location);
                fogTile.gameObject.SetActive(false);

                //Destroy(fogTile.gameObject);

            }

            hexTiles.Add(location, hexTile);

            if (revealedLocations.TryGetValue(location, out List<FogRevealer> fogRevealers))
                hexTile.FogTile?.AddAgents(fogRevealers);

            return true;
        }
        else
            return false;
    }

    private bool TryReplaceHex(Hex3 location, HexTile hexTile)
    {
        hexTile.transform.position = location;
        hexTile.AdjustPosition();
        HelperFunctions.UpdatePathfindingGrid(hexTile.gameObject, hexTile.Walkable, hexTile.Penalty, (int)hexTile.TileType);

        if (hexTiles.ContainsKey(location))
        {
            HexTile oldTile = hexTiles[location];
            oldTile.gameObject.SetActive(false);
            hexTiles[location] = hexTile;
            hexTile.PlaceHex();

            if (revealedLocations.TryGetValue(location, out List<FogRevealer> fogRevealers))
                hexTile.FogTile?.AddAgents(fogRevealers);

            return true;
        }
        else
            return TryAddHexToDictionary(location, hexTile);
    }

    public static Dictionary<Hex3, HexTile> GetHexTiles()
    {
        return hexTiles;
    }

    public static List<Hex3> GetHex3WithInRange(Hex3 center, int min, int max)
    {
        int neighborCount = 0;
        for (int i = min; i < max + 1; i++)
        {
            if (i == 0)
                neighborCount += 1;
            else
                neighborCount += i * 6;
        }
        List<Hex3> neighbors = new List<Hex3>(neighborCount);
        for (int i = min; i < max + 1; i++)
        {
            Hex3.GetNeighborsAtDistance(center, i, ref neighbors);
        }
        return neighbors;
    }
    
    public static List<Hex3> GetHex3WithInRange(Hex3 center, int min, int max, ref List<Hex3> neighbors)
    {
        int neighborCount = 0;
        for (int i = min; i < max + 1; i++)
        {
            if (i == 0)
                neighborCount += 1;
            else
                neighborCount += i * 6;
        }
        for (int i = min; i < max + 1; i++)
        {
            Hex3.GetNeighborsAtDistance(center, i, ref neighbors);
        }
        return neighbors;
    }

    public static Hex3 GetRandomLocationAtDistance(Hex3 center, int distance, int maxFilledNeighbors = 3)
    {
        List<Hex3> neighbors = Hex3.GetNeighborsAtDistance(center, distance).OrderBy(h => System.Guid.NewGuid()).ToList();
        if (neighbors == null || neighbors.Count == 0)
        {
            Debug.Log($"This shouldn't happen. No neighbors for {center} at distance {distance}");
            return new Hex3();
        }

        for (int i = 0; i < neighbors.Count; i++)
        {
            if (hexTiles.ContainsKey(neighbors[i]))
                continue;

            if(GetFilledNeighborLocations(neighbors[i]).Count > maxFilledNeighbors)
                continue;

            return neighbors[i];
        }

        return neighbors[random.Next(0, neighbors.Count)];
    }

    private int GetNextInt_Internal(int min, int max)
    {
        if (random == null)
        {
            random = new System.Random(RandomizeSeed);
        }
        return random.Next(min, max);
    }

    public static int GetNextInt(int min, int max)
    {
        if (externalRandom == null)
        {
            HexTileManager htm = FindObjectOfType<HexTileManager>();
            externalRandom = new System.Random(htm.RandomizeSeed);
        }
        return externalRandom.Next(min, max);
    }

    public static float GetNextFloat(float min = 0f, float max = 1f)
    {
        if (externalRandom == null)
        {
            HexTileManager htm = FindObjectOfType<HexTileManager>();
            externalRandom = new System.Random(htm.RandomizeSeed);
        }
        return (float)(externalRandom.NextDouble() * (max - min) + min);
    }

    private struct TilePrefabContainer
    {
        public HexTileType tileType;
        public GameObject prefab;
    }

    public static int GetTilePenality(HexTileType tileType)
    {
        switch (tileType)
        {
            case HexTileType.grass:
                return 1;
            case HexTileType.mountain:
                return 1000;
            case HexTileType.forest:
                return 500;
            case HexTileType.water:
                return 1000;
            case HexTileType.feOre:
                return 1;
            default:
                return 1;
        }
    }

    #region fog

    public IEnumerator PlaceFogOverTime()
    {
        List<Hex3> hexes = Hex3.GetNeighborsInRange(Hex3.Zero, LandmassGenerator.globalSize);
        for (int i = 0; i < hexes.Count; i++)
        {
            if (revealedLocations.Keys.Contains(hexes[i]))
                continue;

            if (!HexTileManager.GetHexTiles().TryGetValue(hexes[i], out HexTile hexTile))
            {
                FogTile blankTile = Instantiate(fogTilePrefab, hexes[i], Quaternion.identity);
                blankTile.transform.SetParent(tileParent);
                fogTiles.TryAdd(hexes[i], blankTile);
                blankTile.transform.Rotate(Vector3.up, GetNextInt_Internal(0, 6) * 60f);
            }

            if (i % 50 == 0)
                yield return null;
        }
    }
    public IEnumerator PlaceBorderTiles()
    {
        int count = 0;
        foreach (Hex3 hex in Hex3.GetNeighborsAtDistance(Hex3.Zero, LandmassGenerator.globalSize + 1))
        {
            if (!HexTileManager.GetHexTiles().TryGetValue(hex, out HexTile hexTile))
            {
                count++;
                GameObject blankTile = Instantiate(borderTilePrefab, hex, Quaternion.identity);
                blankTile.transform.SetParent(tileParent);
                blankTile.transform.Rotate(Vector3.up, GetNextInt_Internal(0, 6) * 60f);
                borderTiles.TryAdd(hex, blankTile);
            }

            if (count % 50 == 0)
                yield return null;
        }
    }

    public void AddFogAgent(List<Hex3> locations, FogRevealer fogRevealer, bool addOverTime = true)
    {
        if (this == null)
            return;
        StartCoroutine(AddAgentsOverTime(locations, fogRevealer, addOverTime));
    }

    private IEnumerator AddAgentsOverTime(IEnumerable<Hex3> locations, FogRevealer fogRevealer, bool addOverTime = true)
    {
        locations = locations.OrderBy(l => Hex3.DistanceBetween(l, fogRevealer.transform.position.ToHex3()));

        foreach (var location in locations)
        {
            if (fogRevealer == null)
                continue;

            if (revealedLocations.TryGetValue(location, out List<FogRevealer> fogRevealers))
            {
                if (!fogRevealers.Contains(fogRevealer))
                    fogRevealers.Add(fogRevealer);
            }
            else
                revealedLocations.Add(location, new List<FogRevealer> { fogRevealer });

            if (hexTiles.TryGetValue(location, out HexTile hexTile) && hexTile != null && hexTile.FogTile != null)
            {
                hexTile.FogTile.AddAgent(fogRevealer);
            }
            else if (fogTiles.TryGetValue(location, out FogTile fogTile) && fogTile != null && fogTile.gameObject.activeInHierarchy)
            {
                fogTile.AddAgent(fogRevealer);
            }

            if(addOverTime)
                yield return null;

        }
        yield return null;
    }

    [Button]
    private void GetRevealersAtLocation(Hex3 location)
    {
        foreach (var revealer in revealedLocations[location])
        {
            Debug.Log(revealer.name, revealer.gameObject);
        }
    }

    public static int NumberOfRevealersAtLocation(Hex3 location)
    {
        if (revealedLocations.TryGetValue(location, out List<FogRevealer> fogRevealers))
            return fogRevealers.Count;
        else
            return 0;
    }

    public void RemoveFogAgent(List<Hex3> locations, FogRevealer fogRevealer, bool removeOverTime = true)
    {
        if (this == null)
            return;
        StartCoroutine(RemoveAgentsOverTime(locations, fogRevealer, removeOverTime));
    }

    private IEnumerator RemoveAgentsOverTime(List<Hex3> locations, FogRevealer fogRevealer, bool removeOverTime = true)
    {
        locations = locations.OrderByDescending(l => Hex3.DistanceBetween(l, fogRevealer.transform.position.ToHex3())).ToList();

        foreach (var location in locations)
        {
            if (revealedLocations.TryGetValue(location, out List<FogRevealer> fogRevealers))
                fogRevealers.Remove(fogRevealer);

            if (hexTiles.TryGetValue(location, out HexTile hexTile) && hexTile != null && !hexTile.isPlaceHolder)
            {
                if (hexTile.FogTile == null)
                {
                    Debug.LogError($"No fog tile for {hexTile.TileType}", hexTile.gameObject);
                    yield break;
                }
                hexTile.FogTile.RemoveAgent(fogRevealer);
            }
            else if (fogTiles.TryGetValue(location, out FogTile fogTile) && fogTile != null)
            {
                fogTile.RemoveAgent(fogRevealer);
            }

            if(removeOverTime)
                yield return null;
        }
        yield return null;
    }


    [Button]
    private bool IsLocationRevealed(Hex3 location)
    {
        return revealedLocations.ContainsKey(location);
    }

    public bool IsLocationRevealed(Hex3 location, out List<FogRevealer> fogRevealers)
    {
        if (revealedLocations.TryGetValue(location, out List<FogRevealer> _fogRevealers))
        {
            fogRevealers = _fogRevealers;
            return fogRevealers.Count > 0;
        }

        fogRevealers = null;
        return false;
    }
    #endregion

    public static void RemoveTileAtLocation(Hex3 location)
    {
        if (hexTiles.ContainsKey(location))
            hexTiles.Remove(location);
    }


    #region Save and Load
    private const string TILE_SAVE_PATH = "HexTileData";
    private const string FOG_SAVE_PATH = "FogTileData";
    private const string BORDER_SAVE_PATH = "BorderTileData";
    private const string RANDOM_SEED_PATH = "RandomSeed";
    private const string RANDOM_PATH = "Random";
    private const string REVEALED_TILE_PATH = "RevealedTileData";
    public static string RandomSeedPath => RANDOM_SEED_PATH;
    private int spawnInterval = 50;
    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this, 0);
    }

    public void Save(string savePath, ES3Writer writer)
    {
        List<HexTileData> data = new List<HexTileData>();
        foreach (var hex in hexTiles.Values)
        {
            if (hex == null)
                continue;

            HexTileData hexTileData = new HexTileData
            {
                location = hex.hexPosition,
                tileType = hex.TileType,
            };

            if (hex.TryGetComponent(out ResourceTile resourceTile))
            {
                hexTileData.resourceAmount = resourceTile.ResourceAmount;
            }

            if (hex.TryGetComponent(out FogGroundTile fogTile))
                hexTileData.revealed = fogTile.HasBeenRevealed;
            data.Add(hexTileData);
        }


        List<Hex3> remainingFogTiles = new List<Hex3>();
        foreach (var keyValuePair in fogTiles)
        {
            if(!keyValuePair.Value.isDown)
                remainingFogTiles.Add(keyValuePair.Key);
        }

        writer.Write<List<HexTileData>>(TILE_SAVE_PATH, data);
        writer.Write<List<Hex3>>(BORDER_SAVE_PATH, borderTiles.Keys.ToList());
        writer.Write<int>(RANDOM_SEED_PATH, RandomizeSeed);
        writer.Write<System.Random>(RANDOM_PATH, random);
        writer.Write<List<Hex3>>(FOG_SAVE_PATH, remainingFogTiles);
        writer.Write<List<Hex3>>(REVEALED_TILE_PATH, revealedLocations.Keys.ToList());
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if(!ES3.FileExists(loadPath))
        {
            Debug.LogError($"No file found at {loadPath}");
            yield break;
        }

        yield return LoadOverTime(loadPath, postUpdateMessage);
    }

    private IEnumerator LoadOverTime(string loadPath, Action<string> postUpdateMessage)
    {
        if(ES3.KeyExists(RANDOM_PATH, loadPath))
        {
            random = ES3.Load<System.Random>(RANDOM_PATH, loadPath);
        }
        if(ES3.KeyExists(RANDOM_SEED_PATH, loadPath))
        {
            randomizeSeed = ES3.Load<int>(RANDOM_SEED_PATH, loadPath);
        }
        //Load regular land tiles
        if (ES3.KeyExists(TILE_SAVE_PATH, loadPath))
        {
            List<HexTileData> data = ES3.Load<List<HexTileData>>(TILE_SAVE_PATH, loadPath);
            for (int i = 0; i < data.Count; i++)
            {
                if(i % spawnInterval == 0)
                    yield return null;
                HexTile tile = GetNextHexTile(data[i].tileType, false);
                TryReplaceHex(data[i].location, tile);
                postUpdateMessage?.Invoke($"Loading {tile.TileType.ToNiceString()} Tile {i + 1} of {data.Count}");

                if(tile.TryGetComponent(out ResourceTile resourceTile))
                {
                    resourceTile.SetResourceAmount(data[i].resourceAmount);
                }

                if (data[i].revealed)
                    tile.GetComponent<FogGroundTile>().RevealTile();
            }
        }
        else
            Debug.LogError($"No {TILE_SAVE_PATH} found in {loadPath}");

        //load fog tiles
        if (ES3.KeyExists(FOG_SAVE_PATH, loadPath))
        {
            List<Hex3> fogTileData = ES3.Load<List<Hex3>>(FOG_SAVE_PATH, loadPath);
            for(int i = 0; i < fogTileData.Count; i++)
            {
                if (i % spawnInterval == 0)
                    yield return null;
                FogTile fogTile = Instantiate(fogTilePrefab, fogTileData[i], Quaternion.identity);
                fogTile.transform.SetParent(tileParent);
                fogTile.transform.Rotate(Vector3.up, GetNextInt_Internal(0, 6) * 60f);
                fogTiles.TryAdd(fogTileData[i], fogTile);
                postUpdateMessage?.Invoke($"Fog of War {i + 1} of {fogTileData.Count}");
            }
        }
        else
            Debug.LogError($"No {FOG_SAVE_PATH} found in {loadPath}");

        //load border tiles
        if (ES3.KeyExists(BORDER_SAVE_PATH, loadPath))
        {
            List<Hex3> borderTileData = ES3.Load<List<Hex3>>(BORDER_SAVE_PATH, loadPath);
            for (int i = 0; i < borderTileData.Count; i++)
            {
                if (i % spawnInterval == 0)
                    yield return null;
                GameObject borderTile = Instantiate(borderTilePrefab, borderTileData[i], Quaternion.identity);
                borderTile.transform.SetParent(tileParent);
                borderTile.transform.Rotate(Vector3.up, GetNextInt_Internal(0, 6) * 60f);
                borderTiles.TryAdd(borderTileData[i], borderTile);
                postUpdateMessage?.Invoke($"Border Tile {i + 1} of {borderTileData.Count}");
            }
        }
        else
            Debug.LogError($"No {BORDER_SAVE_PATH} found in {loadPath}");

        if (ES3.KeyExists(REVEALED_TILE_PATH, loadPath))
        {
            List<Hex3> revealedLocations = ES3.Load<List<Hex3>>(REVEALED_TILE_PATH, loadPath);
            for (int i = 0; i < revealedLocations.Count; i++)
            {
                HexTileManager.revealedLocations.TryAdd(revealedLocations[i], new List<FogRevealer>());
            }
        }
    }

    public static List<string> GetDataNames()
    {
        return new List<string>() { TILE_SAVE_PATH, FOG_SAVE_PATH, BORDER_SAVE_PATH, RANDOM_SEED_PATH, RANDOM_PATH };
    }

    public class HexTileData
    {
        public Hex3 location;
        public HexTileType tileType;
        public int resourceAmount;
        public bool revealed;
    }
    #endregion
}

