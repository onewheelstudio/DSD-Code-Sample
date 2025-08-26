using HexGame.Grid;
using HexGame.Resources;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[Manageable]
public class LandmassGenerator : MonoBehaviour
{
    [SerializeField]
    private bool autoGenerate = true;
    [SerializeField, Required, AssetsOnly, InlineEditor(Expanded = true)] 
    private LandGenerationData landGenerationData;

    #region Members
    public static event Action generationStarted;
    public static event Action generationComplete;
    private static bool _generating = false;
    public static bool generating => _generating;

    private HexTileManager htm;
    private int chunks => landGenerationData.chunks;
    private Hex3 nextLocation;

    //settings
    private int minClusters => landGenerationData.minClusters;
    private int maxClusters => landGenerationData.maxClusters;
    private int minDistance => landGenerationData.minDistance;
    private int maxDistance => landGenerationData.maxDistance;
    private int minSize => landGenerationData.minSize;
    private int maxSize => landGenerationData.maxSize;
    public static int globalSize => LandGenerationData.globalSize;

    [Header("Other Bits")]
    [SerializeField] private GameObject enemyCrystalPrefab;
    private int enemyCrystalCount => landGenerationData.enemyCrystalCount;
    private Vector2Int crystalRange => landGenerationData.crystalRange;
    [SerializeField] private bool useFog = true;
    private Vector2Int gapRange => landGenerationData.gapRange;

    //progress
    private float maxSteps;
    private float currentStep;
    public static event Action<float, string> generationProgress;
    #endregion

    private void Awake()
    {
        htm = FindObjectOfType<HexTileManager>();
        _generating = false;
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += MakeLand;
    }


    private void MakeLand(Scene arg0, LoadSceneMode arg1)
    {
        if (arg0.buildIndex != 2)
            return;

        //if (autoGenerate)
        //    Generate();
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= MakeLand;
        StopAllCoroutines();
    }

    private void OnValidate()
    {
        if (maxClusters < minClusters)
            landGenerationData.maxClusters = minClusters + 1;
        if (maxDistance < minDistance)
            landGenerationData.maxDistance = minDistance + 1;
        if (maxSize < minSize)
            landGenerationData.maxSize = minSize + 1;
    }

    private void Start()
    {
        if(autoGenerate && !SaveLoadManager.Loading)
            Generate();
    }

    [Button]
    public void Generate()
    {
        if (!Application.isPlaying)
            return;
        else
            StartCoroutine(GenerateLandMass());
    }

    IEnumerator GenerateLandMass()
    {
        //global size should or could be implemented as a setting
        landGenerationData.maxDistance = maxDistance > globalSize ? globalSize : maxDistance;

        generationStarted?.Invoke();
        //delay added for scene loading... :shrug:
        yield return new WaitForSeconds(0.5f);
        _generating = true;
        yield return null;

        generationProgress?.Invoke(0f, "Generating the World");
        yield return new WaitForSeconds(0.25f);
        int clusters = HexTileManager.GetNextInt(minClusters, maxClusters);
        maxSteps = chunks + clusters + 4 + 2; //last 2 are for fog and borders

        yield return htm.PlaceBorderTiles();
        generationProgress?.Invoke(currentStep / maxSteps, "Placing Border Tiles");

        //generate first tile at origin
        foreach (var tile in htm.GetTilesToFill(Hex3.Zero, HexTileType.grass, false))
        {
            if (tile != null)
                tile.gameObject.SetActive(true);
            yield return null;
        }

        currentStep = 1; 

        //generate remaining start location tiles
        for (int i = 0; i < chunks; i++)
        {
            nextLocation = GetRandomLocation(GetAllEmptyNeighbors(),5);

            //ensure that there is building spots to start and basic resources
            HexTileType tileType;
            if (i == 0)
            {
                tileType = HexTileType.feOre;
                nextLocation = HexTileManager.GetEmptyLocationNextTo(HexTileType.grass, Hex3.Zero, 4);
            }
            else if (i == 1)
            {
                tileType = HexTileType.water;
                nextLocation = HexTileManager.GetEmptyLocationNextTo(HexTileType.grass, Hex3.Zero, 4);
            }
            else if (i == 2)
            {
                tileType = HexTileType.grass;
                nextLocation = HexTileManager.GetEmptyLocationNextTo(HexTileType.grass, Hex3.Zero, 4);
            }
            else if (i == 3)
                tileType = HexTileType.aspen;
            else if (i == 4)
                tileType = HexTileType.mountain;
            else if (i == 5)
                tileType = HexTileType.funkyTree;
            else if (i == 6)
                tileType = HexTileType.forest;
            else
                tileType = GetTileType();


            //this occurs if empty neighbors can't be found...
            if (nextLocation == Hex3.Zero)
            {
                Debug.LogError($"Unable to place {tileType} at for i = {i}");
                yield return null;
                currentStep++;
                continue;
            }

            List<HexTile> newTiles = htm.GetTilesToFill(nextLocation, tileType, false);

            foreach (var tile in newTiles)
            {
                tile.gameObject.SetActive(true);
                yield return null;
            }
            yield return null;

            generationProgress?.Invoke(currentStep / maxSteps, $"Placing {tileType.ToNiceString()} {i + 1} of {chunks}");
            yield return new WaitForSeconds(0.25f);
            currentStep++;

            if (i == 1) //first water and fe ore
            {
                yield return SurroundTiles(newTiles);
                List<HexTile> ironTiles = HexTileManager.GetAllTilesOfType(HexTileType.feOre);
                yield return SurroundTiles(ironTiles);
            }

        }

        yield return GenerateEnemyCrystals();
        yield return new WaitForSeconds(0.25f);
        currentStep++;

        yield return GenerateSpecialTiles();
        currentStep++;
        yield return new WaitForSeconds(0.25f);

        yield return null;
        yield return GenerateClusters(clusters);

        //clear gap
        List<Hex3> gapTiles = HexTileManager.GetHex3WithInRange(Hex3.Zero, gapRange.x, gapRange.y);
        foreach (var gap in gapTiles)
        {
            HexTile hexTile = HexTileManager.GetHexTileAtLocation(gap);
            if (hexTile == null)
                continue;

            hexTile.gameObject.SetActive(false);
            HexTileManager.RemoveTileAtLocation(gap);
            yield return null;
        }

        if(useFog)
            yield return htm.PlaceFogOverTime();
        generationProgress?.Invoke(currentStep / maxSteps, "");

        generationComplete?.Invoke();
        _generating = false;
    }

    private IEnumerator SurroundTiles(List<HexTile> newTiles)
    {
        List<Hex3> emptyNeighbors = HexTileManager.GetEmptyNeighborLocations(newTiles);
        yield return null;
        for(int i = 0; i < emptyNeighbors.Count; i++)
        {
            if(i % 3 == 0)
                htm.PlaceAtLocation(emptyNeighbors[i], HexTileType.aspen);
            else if(i % 4 == 0)
                htm.PlaceAtLocation(emptyNeighbors[i], HexTileType.forest);
            else
                htm.PlaceAtLocation(emptyNeighbors[i], HexTileType.grass);
        }
    }

    private IEnumerator GenerateClusters(int numClusters)
    {
        for (int i = 0; i < numClusters; i++)
        {
            int distance;
            if (i == 0)
                distance = minDistance;
            else
                distance = HexTileManager.GetNextInt(minDistance, maxDistance);

            Hex3 startLocation = HexTileManager.GetRandomLocationAtDistance(Hex3.Zero, distance);
            htm.BuildCluster(startLocation, HexTileManager.GetNextInt(minSize, maxSize));
            yield return null;
            currentStep++;
            generationProgress?.Invoke(currentStep / maxSteps, $"Building Tile Cluster {i} of {numClusters}");
            yield return new WaitForSeconds(0.25f);
        }

    }

    private void GenerateRing(int size)
    {
        List<Hex3> locations = Hex3.GetNeighborsAtDistance(Hex3.Zero, size);
        foreach (var location in locations)
        {
            htm.TrySeedTile(location, HexTileType.grass);
        }
    }

    private IEnumerator GenerateSpecialTiles()
    {
        int count = 0;
        int maxCount = landGenerationData.specialTiles.Count;
        foreach (var special in landGenerationData.specialTiles)
        {
            count++;
            generationProgress?.Invoke(currentStep / maxSteps, $"Adding Resources {count} of {maxCount}");

            for (int i = 0; i < special.number; i++)
            {
                Hex3 location;
                if (HexTileManager.GetNextInt(0,100) > 20)
                    location = HexTileManager.GetRandomLocationAtDistance(Hex3.Zero, special.Distance());
                else
                {
                    List<Hex3> locations = HexTileManager.GetAllEmptyTiles(special.MinDistance, special.MaxDistance);
                    if(locations.Count == 0)
                        location = HexTileManager.GetRandomLocationAtDistance(Hex3.Zero, special.Distance());
                    else
                        location = GetRandomLocation(locations);
                }
                int num = HexTileManager.GetNextInt(special.minClumpSize, special.maxClumpSize);

                htm.TrySeedTile(location, special.type);
                yield return null;
                List<Hex3> neighbors = location.GetNeighborLocations();

                if (num > 1)
                {
                    for (int j = 1; j < num; j++)
                    {
                        if(neighbors.Count == 0)
                            break;

                        Hex3 hex3 = neighbors[HexTileManager.GetNextInt(0, neighbors.Count)];
                        neighbors.Remove(hex3);

                        //get neigbhor tiles and compare types - ensure only 2 neighbors of the same type
                        if(!CanAddSpecialTile(hex3, special))
                            continue;

                        if (!htm.TrySeedTile(hex3, special.type))
                            continue;
                        //grow possible locations
                        List<Hex3> hexNeighbors = hex3.GetNeighborLocations();
                        for (int k = 0; k < hexNeighbors.Count; k++)
                        {
                            if (!neighbors.Contains(hexNeighbors[k]))
                                neighbors.Add(hexNeighbors[k]);
                        }
                    }
                }

                neighbors.Clear();
                foreach (var tile in htm.GetTilesToFill(location, HexTileType.grass, false, 3))
                {
                    neighbors.Add(tile.hexPosition);
                    tile.gameObject.SetActive(true);
                    yield return null;
                }

                if (neighbors.Count > 0)
                {
                    location = htm.GetRandomEmptyNeighbor(GetRandomLocation(neighbors));
                }
                neighbors.Clear();
                foreach (var tile in htm.GetTilesToFill(location, special.GetRandomNearByType(), false, 3))
                {
                    neighbors.Add(tile.hexPosition);
                    tile.gameObject.SetActive(true);
                    yield return null;
                }

                if (HexTileManager.GetNextInt(0, 100) > 50)
                    continue;

                //add additional tiles
                if (neighbors.Count > 0)
                {
                    location = htm.GetRandomEmptyNeighbor(GetRandomLocation(neighbors));
                }
                foreach (var tile in htm.GetTilesToFill(location, special.GetRandomNearByType(), false, 3))
                {
                    tile.gameObject.SetActive(true);
                    yield return null;
                }
            }
        }
    }

    /// <summary>
    /// Checks the number of similar neighbor tiles and compares to max allowed neighbors.
    /// </summary>
    /// <param name="location"></param>
    /// <param name="specialTile"></param>
    /// <returns></returns>
    private bool CanAddSpecialTile(Hex3 location, ResourceToGenerate specialTile)
    {
        List<Hex3> neighbors = HexTileManager.GetFilledNeighborLocations(location);
        if (neighbors.Count <= specialTile.maxNeighbors)
            return true;

        int countSimilar = 0;
        foreach (var neighbor in neighbors)
        {
            HexTileType type = HexTileManager.GetHexTileAtLocation(neighbor).TileType;
            if (type == specialTile.type)
                countSimilar++;
        }

        return countSimilar <= specialTile.maxNeighbors;
    }

    private IEnumerator GenerateEnemyCrystals()
    {
        for (int i = 0; i < enemyCrystalCount; i++)
        {
            generationProgress?.Invoke(currentStep / maxSteps, $"Generating Enemy Spawns {i + 1} of {enemyCrystalCount}");
            yield return null;
            //int range = HexTileManager.GetNextInt(crystalRange.x, crystalRange.y);
            //Hex3 location = HexTileManager.GetRandomLocationAtDistance(Hex3.Zero, range);
            //Hex3 location = GetNewCrystalLocation(range);
            Hex3 location = RandomPointInCircle(Vector2.zero, crystalRange.x, crystalRange.y);



            if(location == Hex3.Zero)
                continue;

            if (htm.TrySeedTile(location, HexTileType.grass))
            {
                GameObject newCrystal = Instantiate(enemyCrystalPrefab);
                newCrystal.transform.position = location;
                newCrystal.GetComponent<HexGame.Units.EnemyUnit>().Place();
            }

            List<Hex3> neighbors = location.GetNeighborLocations();
            int num = HexTileManager.GetNextInt(4, 8);

            for (int j = 0; j < num; j++)
            {
                if(j < 3)
                {
                    htm.TrySeedTile(neighbors[HexTileManager.GetNextInt(0, neighbors.Count)], HexTileType.grass);
                    continue;
                }

                int typeToPick = HexTileManager.GetNextInt(0, 4);
                HexTileType type;
                switch (typeToPick)
                {
                    case 0:
                        type = HexTileType.mountain;
                        break;
                    case 1:
                        type = HexTileType.forest;
                        break;
                    case 2:
                        type = HexTileType.aspen;
                        break;
                    case 3:
                        type = HexTileType.water;
                        break;
                    default:
                        type = HexTileType.grass;
                        break;
                }
                htm.TrySeedTile(neighbors[HexTileManager.GetNextInt(0, neighbors.Count)], type);
            }
        }
    }

    private Hex3 RandomPointInCircle(Vector2 origin, float minRadius, float maxRadius)
    {
        float angle = HexTileManager.GetNextFloat(0, 2 * Mathf.PI);
        Vector2 randomDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        float randomDistance = HexTileManager.GetNextFloat() * (maxRadius - minRadius) + minRadius;
        Vector2 randomPosition = (origin + randomDirection * randomDistance * Mathf.Sqrt(3));
        return new Vector3(randomPosition.x, 0f, randomPosition.y).ToHex3();
    }

    private Hex3 GetNewCrystalLocation(int range)
    {
        int maxAttempts = 5;
        Hex3 location = Hex3.Zero;
        EnemyCrystalManager ecm = FindObjectOfType<EnemyCrystalManager>();

        for (int i = 0; i < maxAttempts; i++)
        {
            location = HexTileManager.GetRandomLocationAtDistance(Hex3.Zero, range);
            foreach (var crystal in ecm.GetCrystals())
            {
                if (HelperFunctions.HexRangeFloat(crystal.transform.position, location) < 15)
                    continue;
            }

            return location;
        }

        return location;
    }

    private HexTileType GetRandomType()
    {
        return (HexTileType)HexTileManager.GetNextInt(0, System.Enum.GetValues(typeof(HexTileType)).Length);
    }

    private HexTileType GetTileType()
    {
        int roll = HexTileManager.GetNextInt(0, 100);
        if (roll > 96)
            return HexTileType.feOre;
        else if (roll > 92)
            return HexTileType.alOre;
        else if (roll > 88)
            return HexTileType.gas;
        else if (roll > 80)
            return HexTileType.water;
        else if (roll > 64)
            return HexTileType.mountain;
        else if (roll > 52)
            return HexTileType.forest;
        else if (roll > 45)
            return HexTileType.aspen;
        else if (roll > 38)
            return HexTileType.funkyTree;
        else
            return HexTileType.grass;
    }

    private Hex3 GetRandomLocation(List<Hex3> neighbors, int rangeFromOrigin = -1)
    {
        if(rangeFromOrigin > 0  && neighbors.Where(n => n.Max() <= rangeFromOrigin).Count() > 0)
        {
            neighbors = neighbors.Where(n => n.Max() <= rangeFromOrigin).ToList();
        }

        return neighbors[HexTileManager.GetNextInt(0, neighbors.Count)];
    }

    private List<Hex3> GetAllEmptyNeighbors()
    {
        List<Hex3> emptyNeighbors = new List<Hex3>();

        foreach (var hex in HexTileManager.GetHexTiles())
        {
            foreach (var neighbor in Hex3.GetNeighborLocations(hex.Key))
            {
                if (!HexTileManager.GetHexTiles().ContainsKey(neighbor) && !emptyNeighbors.Contains(neighbor))
                    emptyNeighbors.Add(neighbor);
            }
        }

        return emptyNeighbors;
    }

    private Hex3 GetEmptyLocation()
    {
        if (HexTileManager.GetHexTiles().Count == 0)
            return Hex3.Zero;

        foreach (var hex in HexTileManager.GetHexTiles())
        {
            foreach (var neighbor in Hex3.GetNeighborLocations(hex.Key))
            {
                if (!HexTileManager.GetHexTiles().ContainsKey(neighbor))
                    return neighbor;
            }
        }

        return Hex3.Zero;
    }

    [System.Serializable]
    public struct ResourceToGenerate
    {
        public HexTileType type;
        public int number;
        [MinValue(1)]
        public int minClumpSize;
        [MinValue(2)]
        public int maxClumpSize;
        public Hex3 center;
        [SerializeField]
        private int minDistance;
        public int MinDistance => minDistance;
        [SerializeField]
        private int maxDistance;
        public int MaxDistance => maxDistance;
        [SerializeField]
        private List<HexTileType> nearByTypes;
        [SerializeField, Range(2,6)] public int maxNeighbors;

        public HexTileType GetRandomNearByType()
        {
            return nearByTypes[HexTileManager.GetNextInt(0, nearByTypes.Count)];
        }

        public int Distance()
        {
            return HexTileManager.GetNextInt(minDistance, maxDistance + 1);
        }


    }
}
