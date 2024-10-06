using HexGame.Resources;
using HexGame.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoCubeDisplay : MonoBehaviour
{
    [SerializeField] private Transform cubeParent;
    private List<Vector3> cubePositions = new List<Vector3>();
    private List<Quaternion> cubeRotations = new List<Quaternion>();
    private float cubeScale;
    private Dictionary<ResourceType, Stack<GameObject>> resourceCubes = new Dictionary<ResourceType, Stack<GameObject>>();

    private UnitStorageBehavior usb;
    private int currentStorage;
    private float amountPerCube;
    private int totalCubes;
    private CargoManager cargoManager;
    private int allowedTypes;

    private void OnEnable()
    {
        usb = this.GetComponentInParent<UnitStorageBehavior>();
        cargoManager = FindObjectOfType<CargoManager>();

        usb.resourceDelivered += CargoAdded;
        usb.resourcePickedUp += CargoRemoved;
        usb.resourceUsed += CargoRemoved;

        GetCubePositions();

        allowedTypes = GetAllowedTypes();
        float maxStorage = usb.GetStat(Stat.maxStorage);
        amountPerCube = (maxStorage * allowedTypes) / cubePositions.Count;
    }

    private void OnDisable()
    {
        usb.resourceDelivered -= CargoAdded;
        usb.resourcePickedUp -= CargoRemoved;
        usb.resourceUsed -= CargoRemoved;
    }

    private int GetAllowedTypes()
    {
        if (usb.GetComponent<SupplyShipBehavior>())
            return 1;

        return usb.GetAllowedTypes().Count;
    }

    private void GetCubePositions()
    {
        cubePositions.Clear();
        cubeScale = cubeParent.GetChild(0).localScale.x;
        for (int i = 0; i < cubeParent.childCount; i++)
        {
            cubePositions.Add(cubeParent.GetChild(i).localPosition);
            cubeRotations.Add(cubeParent.GetChild(i).localRotation);
            cubeParent.GetChild(i).gameObject.SetActive(false); //send to pool
        }
    }

    private void CargoRemoved(UnitStorageBehavior behavior, ResourceAmount amount)
    {
        if (amount.type == ResourceType.Workers)
            return;

        //this calculation added to account storage size changes
        float maxStorage = usb.GetStat(Stat.maxStorage);
        amountPerCube = (maxStorage * allowedTypes) / cubePositions.Count; 
        
        currentStorage -= amount.amount;
        int numCubes = Mathf.RoundToInt(currentStorage / amountPerCube);
        int cubesToTurnOff = totalCubes - numCubes;

        StartCoroutine(RemoveCubes(amount.type, cubesToTurnOff));
    }

    private void CargoAdded(UnitStorageBehavior behavior, ResourceAmount amount)
    {
        if (amount.type == ResourceType.Workers)
            return;

        float maxStorage = usb.GetStat(Stat.maxStorage);
        amountPerCube = (maxStorage * allowedTypes) / cubePositions.Count;

        currentStorage += amount.amount;
        int numCubes = Mathf.Min(Mathf.RoundToInt(currentStorage / amountPerCube), cubePositions.Count);
        int cubesToTurnOn = numCubes - totalCubes;
        StartCoroutine(AddCubes(amount.type, cubesToTurnOn));
    }

    private IEnumerator AddCubes(ResourceType type, int amount)
    {
        if(totalCubes >= cubePositions.Count)
        {
            yield break;
        }

        for (int i = 0; i < amount; i++)
        {
            if(totalCubes >= cubePositions.Count)
            {
                Debug.Log("Cargo cube display is full");
                yield break;
            }

            GameObject cube = GetCube(type);
            cube.SetActive(true);
            cube.transform.SetParent(cubeParent);
            cube.transform.localScale = Vector3.one * cubeScale;

            //needs to be checked again because of delay animation :)
            if (totalCubes >= cubePositions.Count)
            {
                Debug.Log("Cargo cube display is full");
                yield break;
            }

            cube.transform.localPosition = cubePositions[totalCubes];
            cube.transform.localRotation = cubeRotations[totalCubes];
            totalCubes++;
            if (resourceCubes.TryGetValue(type, out Stack<GameObject> cubes))
            {
                cubes.Push(cube);
            }
            else
            {
                Stack<GameObject> newStack = new Stack<GameObject>();
                newStack.Push(cube);
                resourceCubes.Add(type, newStack);
            }

            if(amount > 1)
                yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator RemoveCubes(ResourceType type, int amount)
    {
        if(totalCubes == 0)
        {
            Debug.Log("Cargo cube display is empty");
            yield break;
        }

        for (int i = 0; i < amount; i++)
        {
            if(resourceCubes.TryGetValue(type, out Stack<GameObject> cubes) && cubes.Count > 0)
            {
                cubes.Pop().SetActive(false);
                totalCubes--;
            }

            if (amount > 1 && amount != Mathf.RoundToInt(currentStorage / amountPerCube))
                yield return new WaitForSeconds(0.1f);
        }   
    }

    public void RemoveAllCubes()
    {

    }

    private GameObject GetCube(ResourceType type)
    {
        return cargoManager.GetCargoCube(type);
    }
}
