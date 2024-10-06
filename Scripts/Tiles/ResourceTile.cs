using HexGame.Resources;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ResourceTile : MonoBehaviour
{
    [SerializeField] private HexTile hexTile;
    public HexTileType TileType => hexTile.TileType;
    [SerializeField] private ResourceType resourceType;
    public ResourceType ResourceType => resourceType;

    private int startingResourceAmount;
    [SerializeField] private int resourceAmount = 1000;
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
        startingResourceAmount = resourceAmount;
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
            completeTile?.SetActive(true);
            partialTile?.SetActive(false);
            depletedTile?.SetActive(false);
        }
        else if((float)resourceAmount / (float)startingResourceAmount > 0.01f)
        {
            completeTile?.SetActive(false);
            partialTile?.SetActive(true);
            depletedTile?.SetActive(false);
        }
        else
        {
            completeTile?.SetActive(false);
            partialTile?.SetActive(false);
            depletedTile?.SetActive(true);
        }
    }

    private void TileRevealed()
    {
        resourceTileRevealed?.Invoke(resourceType, this);
    }
}
