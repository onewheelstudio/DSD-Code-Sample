using HexGame.Grid;
using Nova;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldMapGenerator : MonoBehaviour
{
    [Header("Noise Settings")]
    [SerializeField] private int seed = 1;
    private System.Random random;

    [Header("Map Size")]
    [SerializeField, Range(10, 40)] private int width = 10;
    [SerializeField, Range(10, 40)] private int height = 10;
    [SerializeField, Range(1, 6)] private int maxNeighbors = 3 ;
    [SerializeField, Range(0f, 1f)] private float percentToShow = 0.75f;

    [Header("Tile Settings")]
    [SerializeField] private Transform tileParent;
    [SerializeField] private UIMapTile hexTilePrefab;
    [SerializeField] private float spacingFactor = 0.95f;
    [SerializeField] private float tileSize = 45;
    [SerializeField] private bool clearDisconnected = true;
    [SerializeField] private bool randomize = true;
    private MapTiles mapTiles = new();
    public MapTiles MapTiles => mapTiles;
    public UIMapTile CenterTile => centerTile;
    private UIMapTile centerTile;

    private void Awake()
    {

    }

    private void Start()
    {
        DelayedBuild();
    }

    private async void DelayedBuild()
    {
        await Awaitable.NextFrameAsync(); //needed for nova to initialize properly??
        GenerateGrid();
        GenerateMap();
    }

    [ButtonGroup("Generate")]
    [Button(Icon = SdfIconType.Grid3x3, ButtonHeight = 40), GUIColor(0.5f,0.5f,1f)]
    private void GenerateGrid()
    {
        GenerateGrid(width, height);
    }

    private void GenerateGrid(int width = 15, int height = 10)
    {
        tileSize = GetTileSize(width, height);
        float spacing = tileSize * spacingFactor; // Calculate spacing based on tile size and spacing factor

        //create grid - ensures correct size
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                UIMapTile tile = Instantiate(hexTilePrefab, tileParent);
                UIBlock block = tile.GetComponent<UIBlock>();
                block.Size.Value = new Vector2(tileSize, tileSize);
                int q = i;
                int r = i % 2 == 0 ? j - ((i + 1) / 2) : j - (i / 2);
                int s = -q - r;
                Hex3 location = new Hex3(q, r, s);


                mapTiles.Add(location, tile); // Add the tile to the list of map tiles
                Vector3 position;
                if (i % 2 == 0) // Offset every other row for hex grid
                    position = new Vector3(i * spacing * Hex3.SQRT3 / 2f, j * spacing, 0); // Adjust for hex grid, even rows
                else
                    position = new Vector3(i * spacing * Hex3.SQRT3 / 2f, j * spacing + spacing / 2f, 0); // Adjust for hex grid

                block.Position.Value = position;

                //update hex3 value
                tile.SetHexPosition(location);
                tile.SetActive(false);
            }
        }
    }

    [ButtonGroup("Generate")]
    [Button(Icon = SdfIconType.Globe, ButtonHeight = 40), GUIColor(0.5f,1f,0.5f)]
    private void GenerateMap()
    {
        GetAllNeighbors();
        GenerateMap(width, height);
        //SetDisplayWidth();
    }

    private void GenerateMap(int width, int height)
    {
        random = new System.Random(seed);

        int xOffset = random.Next(6) - 3;
        int yOffset = random.Next(6) - 3;

        Vector2Int center = new Vector2Int(width / 2 + xOffset, height / 2 + yOffset);
        int numToShow = Mathf.CeilToInt((width * height) * percentToShow);
        int numActive = 0;

        Queue<UIMapTile> tiles = new Queue<UIMapTile>();
        int q = center.x;
        int r = center.x % 2 == 0 ? center.y - ((center.x + 1) / 2) : center.y - (center.x / 2);
        int s = -q - r;
        Hex3 centerLocation = new Hex3(q, r, s);

        if (mapTiles.TryGetValue(centerLocation, out centerTile))
        {
            centerTile.SetActive(true); //turn on
            centerTile.SetColor(Color.cyan);
            
            numActive++;
            tiles.Enqueue(centerTile);
            centerTile.GenerateCenterTileResources();
        }

        while (numActive < numToShow && tiles.Count > 0)
        {
            UIMapTile tile = tiles.Dequeue();
            var randomizedNeighbors = tile.levelData.neighbors.OrderBy(x => random.Next());
            int maxNeighbors = numActive == 1 ? 6 : this.maxNeighbors;

            int randomNum = random.Next(100);
            if (randomNum > 85)
                maxNeighbors = this.maxNeighbors + 2;
            else if (randomNum > 65)
                maxNeighbors = this.maxNeighbors + 1;

            //is the tile on the edge?
            if (tile.levelData.neighbors.Count < 6)
                maxNeighbors = this.maxNeighbors + 1;

            foreach (var neighbor in randomizedNeighbors)
            {
                if (neighbor.levelData.isActive)
                    continue;

                if (neighbor.levelData.ActiveNeighbors >= maxNeighbors)
                    continue;

                neighbor.SetActive(true);
                tiles.Enqueue(neighbor);
                neighbor.GenerateResources();
                numActive++;
            }
        }
    }

    private void GetAllActiveNeighbors()
    {
        for (int i = 0; i < mapTiles.Count; i++)
        {
            UIMapTile tile = mapTiles.tiles[i];
            if (!tile.levelData.isActive)
                continue;

            foreach (Hex3 neighbor in Hex3.GetNeighborLocations(tile.levelData.location))
            {
                if (mapTiles.TryGetValue(neighbor, out UIMapTile neighborTile) && neighborTile.levelData.isActive)
                    tile.levelData.neighbors.Add(neighborTile);
            }
        }
    }
    
    private void GetAllNeighbors()
    {
        for (int i = 0; i < mapTiles.Count; i++)
        {
            UIMapTile tile = mapTiles.tiles[i];

            foreach (Hex3 neighbor in Hex3.GetNeighborLocations(tile.levelData.location))
            {
                if (mapTiles.TryGetValue(neighbor, out UIMapTile neighborTile))
                    tile.levelData.neighbors.Add(neighborTile);
            }
        }
    }

    [Button(Icon = SdfIconType.XOctagon, ButtonHeight = 40), GUIColor(1f,0.5f,0.5f)]
    private void ClearTileGrid()
    {
        for (int i = mapTiles.Count - 1; i >= 0; i--)
        {
            if (mapTiles.tiles[i] == null) // Check for null references to avoid errors
                continue;

            if (Application.isEditor)
                DestroyImmediate(mapTiles.tiles[i].gameObject);
            else
                Destroy(mapTiles.tiles[i].gameObject);
        }

        mapTiles.Clear();
    }

    private void TurnOffDisconnectedTiles(UIMapTile startTile)
    {
        var connectedTiles = DFS(startTile);
        foreach (var tile in mapTiles.tiles)
        {
            if (connectedTiles.Contains(tile))
                continue;
            else
                tile.gameObject.SetActive(false);
        }
    }

    private HashSet<UIMapTile> DFS(UIMapTile start)
    {
        Stack<UIMapTile> stack = new Stack<UIMapTile>();
        HashSet<UIMapTile> visited = new();
        stack.Push(start);

        while (stack.Count > 0)
        {
            UIMapTile current = stack.Pop();
            if (visited.Contains(current)) continue;

            visited.Add(current);

            // Add neighboring hex tiles to the stack
            foreach (Hex3 neighbor in Hex3.GetNeighborLocations(current.levelData.location))
            {
                if (!mapTiles.TryGetValue(neighbor, out UIMapTile neighborTile))
                    continue;

                if (neighborTile.levelData.isActive && !visited.Contains(neighborTile))
                    stack.Push(neighborTile); // Push the neighbor tile to the stack for further exploration
            }
        }

        return visited;
    }

    //Calculate the tile size based on the map dimensions and spacing factor
    private float GetTileSize(int mapX, int mapY)
    {
        float spacing = tileSize * spacingFactor;
        Vector2 size = tileParent.GetComponent<UIBlock>().CalculatedSize.Value;
        int x = Mathf.FloorToInt(size.x / (mapX * spacingFactor * Hex3.SQRT3 / 2f));
        int y = Mathf.FloorToInt((size.y - spacing / 2f) / (mapY * spacingFactor));

        return Mathf.Min(x, y);
    }

    private Vector2 GetDisplayWidth()
    {
        float xMax = 0;
        float yMax = 0;
        float xMin = 0;
        float yMin = 0;

        foreach (var tile in mapTiles.tiles)
        {
            if (!tile.levelData.isActive)
                continue;

            if (tile.transform.localPosition.y > yMax)
                yMax = tile.transform.localPosition.y; // Find the max y coordinate, this will be used to calculate height of the map
            if (tile.transform.localPosition.x > xMax)
                xMax = tile.transform.localPosition.x; // Find the max x coordinate, this will be used to calculate width of the map
            if (tile.transform.localPosition.y < yMin)
                yMin = tile.transform.localPosition.y; // Find the min y coordinate, this will be used to calculate height of the map
            if (tile.transform.localPosition.x < xMin)
                xMin = tile.transform.localPosition.x; // Find the min x coordinate, this will be used to calculate width of the map
        }

        float spacing = tileSize * spacingFactor;
        return new Vector2(xMax - xMin + spacing, yMax - yMin + spacing);
    }

    [Button]
    private void SetDisplayWidth()
    {
        Vector2 size = GetDisplayWidth();
        tileParent.GetComponent<UIBlock>().Size.XY = GetDisplayWidth();
    }

}

[System.Serializable]
public class MapTiles
{
    public List<Hex3> locations = new();
    public List<UIMapTile> tiles = new();

    public void Add(Hex3 location, UIMapTile tile)
    {
        locations.Add(location);
        tiles.Add(tile);
    }

    public bool TryGetValue(Hex3 location, out UIMapTile tile)
    {
        for (int i = 0; i < locations.Count; i++)
        {
            if (locations[i] == location)
            {
                tile = tiles[i];
                return true;
            }
        }

        tile = null;
        return false;
    }

    public void Clear()
    {
        locations.Clear();
        tiles.Clear();
    }

    public int Count => locations.Count;

}
