using HexGame.Units;
using System;
using UnityEngine;


[System.Serializable]
public class BuildingRequirement
{
    public PlayerUnitType unitType;
    public int totalToBuild;
    [HideInInspector, NonSerialized]
    public int numberBuilt;
    public string DisplayText => $"Build {unitType.ToNiceString()}: {numberBuilt}/{totalToBuild}";
}
