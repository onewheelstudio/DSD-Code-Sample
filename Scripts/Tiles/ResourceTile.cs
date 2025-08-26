using HexGame.Grid;
using HexGame.Resources;
using Sirenix.OdinInspector;
using System;
using UnityEditor;
using UnityEngine;

public class ResourceTile : MonoBehaviour
{
    [SerializeField] private HexTile hexTile;
    public Hex3 Location => hexTile.hexPosition;
    public HexTileType TileType => hexTile.TileType;
    [SerializeField] private ResourceType resourceType;
    public ResourceType ResourceType => resourceType;

    private int startingResourceAmount;
    [SerializeField] private int resourceAmount = 1000;
    [Tooltip("Amount is in 100s, so 15 = 1500")]
    [MinMaxSlider(10,200, ShowFields = true), SerializeField] private Vector2Int resourceRange = new Vector2Int(10,30);
    public int ResourceAmount => resourceAmount;

    public static event Action<ResourceTile> resourceTileDepleted;
    public static event Action<ResourceType, ResourceTile> resourceTileRevealed;
    public event Action<ResourceType, ResourceTile> resourceExtractedLocal;
    public static event Action<ResourceType, ResourceTile> resourceExtractedGlobal;

    [SerializeField] private GameObject completeTile;
    [SerializeField] private GameObject partialTile;
    [SerializeField] private GameObject depletedTile;

    private void Awake()
    {
        hexTile = GetComponentInParent<HexTile>();
        resourceAmount = HexTileManager.GetNextInt(resourceRange.x, resourceRange.y) * 100;
        startingResourceAmount = resourceAmount;
    }

    private void Start()
    {
        if (!SaveLoadManager.Loading)
            SetAmountBasedOnLocation();
    }

    private void OnEnable()
    {
        this.gameObject.GetComponent<FogGroundTile>().JuicedTileRevealed += TileRevealed;
    }

    private void OnDisable()
    {
        this.gameObject.GetComponent<FogGroundTile>().JuicedTileRevealed -= TileRevealed;
    }

    public bool TryExtractResource(int amount)
    {
        if(resourceAmount >= amount)
        {
            resourceAmount -= amount;
            SetTileObject();
            resourceExtractedLocal?.Invoke(resourceType, this);
            resourceExtractedGlobal?.Invoke(resourceType, this);
            return true;
        }
        else
        {
            resourceTileDepleted?.Invoke(this);
            return false;
        }
    }

    private void SetTileObject()
    {
        if((float)resourceAmount / (float)startingResourceAmount > 0.5f)
        {
            if(completeTile != null)
                completeTile.SetActive(true);
            if(partialTile != null)
                partialTile.SetActive(false);
            if(depletedTile != null)
                depletedTile.SetActive(false);
        }
        else if((float)resourceAmount / (float)startingResourceAmount > 0.01f)
        {
            if(completeTile != null)
                completeTile.SetActive(false);
            if(partialTile != null)
                partialTile.SetActive(true);
            if(depletedTile != null)
                depletedTile.SetActive(false);
        }
        else
        {
            if(completeTile != null)
                completeTile.SetActive(false);
            if(partialTile != null)
                partialTile.SetActive(false);
            if(depletedTile != null)
                depletedTile.SetActive(true);
        }
    }

    private void TileRevealed()
    {
        resourceTileRevealed?.Invoke(resourceType, this);
    }

    public void SetResourceAmount(int amount)
    {
        resourceAmount = amount;
        SetTileObject();
    }

    private void SetAmountBasedOnLocation()
    {
        switch (hexTile.TileType)
        {
            case HexTileType.feOre:
                if(Hex3.DistanceBetween(Location, Hex3.Zero) < 7)
                {
                    resourceAmount *= 2;
                    startingResourceAmount = resourceAmount;
                }
                break;
            default:
                break;
        }
    }
}
