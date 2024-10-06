using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.ObjectModel;
using HexGame.Resources;
using HexGame.Units;
using OWS.Nova;

public struct PopUpInfo
{
    public PopUpInfo(string info, float priority, PopUpInfoType infoType, int typeInfo = 0)
    {
        this.info = info;
        this.priority = priority;
        this.infoType = infoType;
        this.objectInfo = typeInfo;
    }

    public PopUpInfo(string info, PopUpInfoType infoType, float priority = 0f, int typeInfo = 0)
    {
        this.info = info;
        this.priority = priority;
        this.infoType = infoType;
        this.objectInfo = typeInfo;
    }

    public float priority;
    public string info;
    public PopUpInfoType infoType;
    public int objectInfo;
    public enum PopUpInfoType
    {
        name,
        stats,
        storage,
        icon,
        description
    }

    public override string ToString()
    {
        return info;
    }
}

public struct PopUpPriorityButton
{
    public PopUpPriorityButton(string displayName, Action action, float priority = 0f, bool closeWindowOnClick = false)
    {
        this.displayName = displayName;
        this.button = action;
        this.priority = priority;
        this.closeWindowOnClick = closeWindowOnClick;
    }

    public string displayName;
    public Action button;
    public float priority;
    public bool closeWindowOnClick;
}

public struct PopUpCanToggle
{
    public PopUpCanToggle(System.Action toggleAction, bool isActive)
    {
        this.isActive = isActive;
        this.toggleAction = toggleAction;
    }

    public bool isActive;
    public System.Action toggleAction;
}

public class RequestStorageInfo
{
    public RequestStorageInfo()
    {

    }

    public CargoManager.RequestPriority priority;
    public Action<CargoManager.RequestPriority> setPriority;
    public Func<CargoManager.RequestPriority> getPriority;

    public List<UnitStorageBehavior> connections;
    public Action startAddConnection;
}

public struct PopUpButtonInfo
{
    public ButtonType buttonType;
    public Action buttonAction;

    public PopUpButtonInfo(ButtonType buttonType, Action buttonAction)
    {
        this.buttonType = buttonType;
        this.buttonAction = buttonAction;
    }
}

public enum ButtonType
{
    move,
    addUnit,
    removeUnit,
    launch,
}

public struct PopUpValues
{
    public PopUpValues(string label, float value)
    {
        this.label = label;
        this.value = value;
    }

    public string label;
    public float value;
}

public struct PopUpStats : IEqualityComparer, IEquatable<PopUpStats>
{
    public Stat stat;
    public float value;
    public float priority;
    public Color color;

    public PopUpStats(Stat stat, float value, float priority, Color color)
    {
        this.stat = stat;
        this.value = value;
        this.priority = priority;
        this.color = color;
    }

    public bool Equals(PopUpStats other)
    {
        return this.stat == other.stat && this.value == other.value;
    }

    public new bool Equals(object x, object y)
    {
        PopUpStats first = (PopUpStats)x;
        PopUpStats second = (PopUpStats)y;

        return first.Equals(second);
    }

    public int GetHashCode(object obj)
    {
        PopUpStats stats = (PopUpStats)obj;
        return HashCode.Combine(stats.stat, stats.value);
    }
}

public struct PopUpResource : IEqualityComparer, IEquatable<PopUpResource>
{
    public ResourceAmount resource;
    public float maxStorage;
    public float priority;
    public Color color;

    public PopUpResource(ResourceAmount resource, float maxStorage, float priority, Color color)
    {
        this.resource = resource;
        this.maxStorage = maxStorage;
        this.priority = priority;
        this.color = color;
    }

    public new bool Equals(object x, object y)
    {
        PopUpResource first = (PopUpResource)x;
        PopUpResource second = (PopUpResource)y;
        return first.Equals(second);
    }

    public bool Equals(PopUpResource other)
    {
        return this.resource == other.resource && this.maxStorage == other.maxStorage;
    }

    public int GetHashCode(object obj)
    {
        return HashCode.Combine(resource, maxStorage);
    }
}

public class ReceipeInfo
{
    public ReadOnlyCollection<ResourceProduction> receipes;
    public IHaveReceipes receipeOwner;
    public int currentRecipe;
    public float efficiency;
    internal float timeToProduce;
}
