using HexGame.Grid;
using HexGame.Resources;
using Nova;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class UIMapTile : MonoBehaviour, IHavePopupInfo, IHavePopUpValues
{
    private UIBlock2D hex;
    public LevelData levelData;
    [SerializeField] private TextBlock tileLabel;
    [SerializeField] private Button tileButton;
    public static event Action<UIMapTile> OnTileClicked;

    public float Difficulty { get => levelData.enemyForces; }


    private void OnEnable()
    {
        tileButton.Clicked += TileClicked;
    }

    private void OnDisable()
    {
        tileButton.Clicked -= TileClicked;
    }

    public void SetHexPosition(Hex3 location)
    {
        //swap yz to get the correct hex position
        this.levelData.location = location;
        tileLabel.Text = location.StringCoordinates();
    }

    [Button]
    public Hex3 GetHexPosition()
    {
        this.levelData.location = (this.transform.position / 4f).ToHex3();
        return levelData.location;
    }

    public void SetNoise(float value, float offThreshold)
    {
        if (hex == null)
            hex = this.GetComponent<UIBlock2D>();

        levelData.isActive = value <= offThreshold;
        hex.gameObject.SetActive(levelData.isActive);
    }

    public void SetActive(bool active)
    {
        levelData.isActive = active;
        this.gameObject.SetActive(active);
    }

    public List<PopUpInfo> GetPopupInfo()
    {
        List<PopUpInfo> infoList = new List<PopUpInfo>();
        infoList.Add(new PopUpInfo($"Location: {levelData.location}", 10, PopUpInfo.PopUpInfoType.name));
        return infoList;
    }

    List<PopUpValues> IHavePopUpValues.GetPopUpValues()
    {
        List<PopUpValues> values = new List<PopUpValues>();
        values.Add(new PopUpValues("stat1", levelData.enemyForces));
        values.Add(new PopUpValues("stat2", levelData.playerForces));
        values.Add(new PopUpValues("stat3", levelData.stat3));
        values.Add(new PopUpValues("stat4", levelData.stat4));
        values.Add(new PopUpValues("stat5", levelData.stat5));
        return values;
    }

    internal void SetColor(Color color)
    {
        if(this.hex == null)
            hex = this.GetComponent<UIBlock2D>();
        this.hex.Color = color;
    }

    private void TileClicked()
    {
        OnTileClicked?.Invoke(this);
        Debug.Log("Clicked on tile: " + levelData.location);
    }

    public void GenerateCenterTileResources()
    {
        List<ResourceAmount> resources = new List<ResourceAmount>();
        resources.Add(new ResourceAmount(ResourceType.FeOre, 75));
        resources.Add(new ResourceAmount(ResourceType.AlOre, 25));
        resources.Add(new ResourceAmount(ResourceType.CuOre, 25));

        this.levelData.resources = resources;
    }

    public void GenerateResources()
    {
        List<ResourceAmount> resources = new List<ResourceAmount>();
        foreach (var resource in SectorResourceTypes)
        {
            int randomValue = UnityEngine.Random.Range(0, 100);

            if(randomValue > 90)
                resources.Add(new ResourceAmount(resource, UnityEngine.Random.Range(75, 100)));
            else if(randomValue > 60)
                resources.Add(new ResourceAmount(resource, UnityEngine.Random.Range(25, 75)));
            else if(randomValue > 30)
                resources.Add(new ResourceAmount(resource, UnityEngine.Random.Range(10, 25)));
        }

        this.levelData.resources = resources;
    }

    //possible raw resources for a tile
    private List<ResourceType> SectorResourceTypes = new List<ResourceType>
    {
        ResourceType.FeOre,
        ResourceType.AlOre,
        ResourceType.CuOre,
        ResourceType.Oil,
        ResourceType.Gas,
        ResourceType.TiOre,
        ResourceType.UOre,
    };
    
}
