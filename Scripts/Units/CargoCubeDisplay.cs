using HexGame.Resources;
using HexGame.Units;
using System.Collections.Generic;
using UnityEngine;

public class CargoCubeDisplay : MonoBehaviour
{
    [SerializeField] private Transform cubeParent;
    private List<Vector3> cubePositions = new List<Vector3>();
    private List<Quaternion> cubeRotations = new List<Quaternion>();
    private Stack<int> positionIndices;
    private float cubeScale;
    private Dictionary<ResourceType, Stack<CargoCube>> resourceCubes = new Dictionary<ResourceType, Stack<CargoCube>>();
    private List<CargoCube> cubeList = new();

    private UnitStorageBehavior usb;
    private int currentStorage;
    private float amountPerCube;
    private int totalCubes;
    private static CargoManager cargoManager;
    private int allowedTypes;

    private void OnEnable()
    {
        usb = this.GetComponentInParent<UnitStorageBehavior>();
        cargoManager ??= FindFirstObjectByType<CargoManager>();

        usb.resourceDelivered += CargoAdded;
        usb.resourcePickedUp += CargoRemoved;
        usb.resourceUsed += CargoRemoved;

        GetCubePositions();

        allowedTypes = GetAllowedTypes();
        float maxStorage = usb.GetStat(Stat.maxStorage);
        amountPerCube = (maxStorage * allowedTypes) / cubePositions.Count;

        positionIndices = new Stack<int>(cubePositions.Count);
        for (int i = cubePositions.Count - 1; i >= 0; i--)
        {
            positionIndices.Push(i);
        }
    }

    private void OnDisable()
    {
        usb.resourceDelivered -= CargoAdded;
        usb.resourcePickedUp -= CargoRemoved;
        usb.resourceUsed -= CargoRemoved;
    }

    private int GetAllowedTypes()
    {
        return usb.GetAllowedResources().Count;
    }


    private void GetCubePositions()
    {
        cubePositions.Clear();
        cubePositions.Capacity = cubeParent.childCount;
        cubeScale = cubeParent.GetChild(0).localScale.x;
        for (int i = 0; i < cubeParent.childCount; i++)
        {
            cubePositions.Add(cubeParent.GetChild(i).localPosition);
            cubeRotations.Add(cubeParent.GetChild(i).localRotation);
            cubeParent.GetChild(i).gameObject.SetActive(false); //send to pool
        }
    }

    private async void CargoRemoved(UnitStorageBehavior behavior, ResourceAmount amount)
    {
        if (amount.type == ResourceType.Workers)
            return;

        while (doingCubeStuff)
            await Awaitable.NextFrameAsync();
        doingCubeStuff = true;

        if (behavior is ShipStorageBehavior)
        {
            RemoveAllCubes();
            return;
        }

        int amountStored = behavior.GetAmountStored(amount.type);
        float storagePercent = (float)amountStored / behavior.GetStorageCapacity();
        int currentCubes = 0;
        if (resourceCubes.TryGetValue(amount.type, out Stack<CargoCube> cubes))
            currentCubes = cubes.Count;
        int numCubes = currentCubes - Mathf.CeilToInt(cubePositions.Count * storagePercent);
        int cubesToTurnOff = totalCubes < numCubes ? totalCubes : numCubes;

        if (cubesToTurnOff <= 0)
        {
            doingCubeStuff = false;
            return;
        }

        RemoveCubes(amount.type, cubesToTurnOff);
     }

    private async void CargoAdded(UnitStorageBehavior behavior, ResourceAmount amount)
    {
        if (amount.type == ResourceType.Workers)
            return;

        while (doingCubeStuff)
            await Awaitable.NextFrameAsync();

        doingCubeStuff = true;
        currentStorage += amount.amount;

        int amountStored = behavior.GetAmountStored(amount.type);
        float storagePercent = (float)amountStored / behavior.GetStorageCapacity();
        int currentCubes = 0;
        if (resourceCubes.TryGetValue(amount.type, out Stack<CargoCube> cubes))
            currentCubes = cubes.Count;
        int numCubes = Mathf.CeilToInt(cubePositions.Count * storagePercent) - currentCubes;
        int remainingCubes = cubePositions.Count - totalCubes;
        int cubesToTurnOn = remainingCubes < numCubes ? remainingCubes : numCubes;

        if (cubesToTurnOn <= 0)
        {
            doingCubeStuff = false;
            return;
        }
        AddCubes(amount.type, cubesToTurnOn);
    }

    private bool doingCubeStuff = false;

    private async void AddCubes(ResourceType type, int amount)
    {
        if(totalCubes >= cubePositions.Count)
        {
            doingCubeStuff = false;
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            if(totalCubes >= cubePositions.Count)
            {
                doingCubeStuff = false;
                return;
            }

            CargoCube cube = GetCube(type);
            cubeList.Add(cube);
            cube.transform.SetParent(cubeParent);
            cube.transform.localScale = Vector3.one * cubeScale;
            int index = positionIndices.Pop();
            cube.positionIndex = index;
            cube.transform.localPosition = cubePositions[index];
            cube.transform.localRotation = cubeRotations[index];
            cube.gameObject.SetActive(true);
            totalCubes++;

            if (resourceCubes.TryGetValue(type, out Stack<CargoCube> cubes))
            {
                cubes.Push(cube);
            }
            else
            {
                Stack<CargoCube> newStack = new Stack<CargoCube>();
                newStack.Push(cube);
                resourceCubes.Add(type, newStack);
            }

            if (amount > 1)
                await Awaitable.WaitForSecondsAsync(0.1f);
        }
        doingCubeStuff = false;
    }

    private async void RemoveCubes(ResourceType type, int amount)
    {
        if (totalCubes == 0)
        {
            doingCubeStuff = false;
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            if(resourceCubes.TryGetValue(type, out Stack<CargoCube> cubes) && cubes.Count > 0)
            {
                CargoCube cube = cubes.Pop();
                positionIndices.Push(cube.positionIndex);
                cube.transform.SetParent(null);
                cube.ReturnToPool();
                totalCubes--;
            }

            if (amount > 1)
                await Awaitable.WaitForSecondsAsync(0.1f);
        }

        doingCubeStuff = false;
    }

    private async void RemoveAllCubes()
    {
        doingCubeStuff = true;
        foreach (var resouce in resourceCubes.Keys)
        {
            if (!resourceCubes.TryGetValue(resouce, out Stack<CargoCube> cubes))
                continue;

            int count = cubes.Count;
            for (int i = 0; i < count; i++)
            {
                CargoCube cube = cubes.Pop();
                positionIndices.Push(cube.positionIndex);
                if(!cube.gameObject.activeSelf)
                    continue; //already returned to pool

                cube.transform.SetParent(null);
                cube.gameObject.SetActive(false);
                await Awaitable.WaitForSecondsAsync(0.1f);
            }
        }
        doingCubeStuff = false;
        totalCubes = 0;
        currentStorage = 0;
    }

    private CargoCube GetCube(ResourceType type)
    {
        return cargoManager.GetCargoCube(type);
    }
}
