using HexGame.Grid;
using Nova;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class UIMapTile : MonoBehaviour, IHavePopupInfo, IHavePopUpValues
{
    private UIBlock2D hex;
    public LevelData levelData;

    public float Difficulty { get => levelData.difficulty; }

    private void OnEnable()
    {
        this.GetComponent<Button>().OnClicked.AddListener(() => LoadLevel());
    }

    //BuildingMenu.buildMenuHexSize = 38f;
    //[Button]
    //private void AdjustPosition()
    //{
    //    this.transform.localPosition = BuildingMenu.buildMenuHexSize * SwapYZ(Hex3.FlatHex3ToVector3(Hex3.Vector3ToFlatHex3(SwapYZ(this.transform.localPosition / BuildingMenu.buildMenuHexSize))));
    //    levelData.location = Hex3.Vector3ToFlatHex3(SwapYZ(this.transform.localPosition / BuildingMenu.buildMenuHexSize));
    //}
    public Vector3 SwapYZ(Vector3 input)
    {
        return new Vector3(input.x, input.z, input.y);
    }

    public Hex3 GetHexPosition()
    {
        return levelData.location;
    }

    public void SetNoise(float value, float offThreshold)
    {
        if (hex == null)
            hex = this.GetComponent<UIBlock2D>();

        this.levelData.difficulty = value + 0.1f;
        Color color = hex.Color;

        //if (value > threshold)
        //    color.r += value;
        //else 
        if (value < offThreshold)
            hex.gameObject.SetActive(false);
        else
            color.r = 0.5f + DiscreteValues(value);

        hex.Color = color;
    }

    public void SetStats(float stat2, float stat3, float stat4, float stat5)
    {
        this.levelData.stat2 = stat2 + 0.1f;
        this.levelData.stat3 = stat3 + 0.1f;
        this.levelData.stat4 = stat4 + 0.1f;
        this.levelData.stat5 = stat5 + 0.1f;
    }

    public void ResetTileColor()
    {
        if (hex == null)
            hex = this.GetComponent<UIBlock2D>();
        this.gameObject.SetActive(true);
        hex.Color = new Color(0.5f, 0.5f, 0.5f, 1f);
    }

    private float DiscreteValues(float value)
    {
        return Mathf.RoundToInt(value * 10f) /20f;
    }

    private void LoadLevel()
    {
        //BuildingMenu.buildMenuHexSize = 38f;
        //levelData.location = Hex3.Vector3ToFlatHex3(SwapYZ(this.transform.localPosition / BuildingMenu.buildMenuHexSize));
        FindObjectOfType<SessionManager>().LevelData = levelData;
        FindObjectOfType<WorldLevelManager>().SetLevel(this);
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
        values.Add(new PopUpValues("stat1", levelData.difficulty));
        values.Add(new PopUpValues("stat2", levelData.stat2));
        values.Add(new PopUpValues("stat3", levelData.stat3));
        values.Add(new PopUpValues("stat4", levelData.stat4));
        values.Add(new PopUpValues("stat5", levelData.stat5));
        return values;
    }
}
