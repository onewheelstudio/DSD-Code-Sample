using HexGame.Resources;
using HexGame.Units;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuildingCargoDisplay : MonoBehaviour
{
    private Queue<CargoCube> cargoCubes = new();
    [SerializeField] private List<Vector3> cubePositions = new List<Vector3>();
    private UnitStorageBehavior usb;
    private CargoManager cargoManager;
    private GlobalStorageBehavior gsb;
    private bool isGlobal => gsb != null;
    private int maxCubesPerResource = 10;

    private void Awake()
    {
        usb = this.gameObject.GetComponentInParent<UnitStorageBehavior>();

        if (usb is GlobalStorageBehavior)
            gsb = usb as GlobalStorageBehavior;

        cargoCubes.Clear();
        cargoManager = FindObjectOfType<CargoManager>();
    }

    private void OnEnable()
    {
        if(gsb != null)
        {
            GlobalStorageBehavior.resourceAdded += DisplayCubes;
            GlobalStorageBehavior.resourceRemoved += DisplayCubes;
        }
        //else
        //{
        //    usb.resourceDelivered += DisplayCubes;
        //    usb.resourcePickedUp += DisplayCubes;
        //    usb.resourceUsed += DisplayCubes;
        //}
    }

    private void OnDisable()
    {
        if (gsb != null)
        {
            GlobalStorageBehavior.resourceAdded -= DisplayCubes;
            GlobalStorageBehavior.resourceRemoved -= DisplayCubes;
        }
        //else
        //{
        //    usb.resourceDelivered -= DisplayCubes;
        //    usb.resourcePickedUp -= DisplayCubes;
        //    usb.resourceUsed -= DisplayCubes;
        //}
    }

    private void DisplayCubes(ResourceAmount resource)
    {
        if (gsb == null)
            return;

        DisplayCubes(gsb, resource);
    }

    private void DisplayCubes(UnitStorageBehavior usb, ResourceAmount resource)
    {
        List<KeyValuePair<ResourceType,int>> cubesToDisplay = new List<KeyValuePair<ResourceType, int>>();
        int totalCubes = 0;
        Queue<CargoCube> tempCubes = new();
        foreach(var _resource in PlayerResources.resourceStored)
        {
            float percent = PlayerResources.PercentFull(_resource);
            if(percent == 0)
                continue;   
            int numberOfCubes = Mathf.CeilToInt(maxCubesPerResource * percent);
            cubesToDisplay.Add(new KeyValuePair<ResourceType, int>(_resource.type, numberOfCubes));

            for (int i = 0; i < numberOfCubes; i++)
            {
                if (totalCubes >= cubePositions.Count)
                {
                    //Debug.LogError("Ran out of cube positions to display storage cubes.");
                    break; 
                }

                CargoCube cube = GetCargoCube(_resource.type);
                tempCubes.Enqueue(cube);

                cube.Transform.SetParent(this.transform);
                cube.Transform.SetLocalPositionAndRotation(cubePositions[totalCubes], Quaternion.identity);
                cube.Transform.localScale = Vector3.one * 25;
                totalCubes++;
            }

            //check again so we can finish and clean up
            if (totalCubes >= cubePositions.Count)
            {
                break;
            }
        }

        //clean up any extra cubes
        for (int i = 0; i < this.cargoCubes.Count; i++)
        {
            CargoCube cube = this.cargoCubes.Dequeue();
            cube.gameObject.SetActive(false); //return to pool
        }

        this.cargoCubes = tempCubes; //we're done so update queue
    }

    //attempt to reuse cubes already at the storage complex.
    //If we can't find a cube of the correct type then we'll pull a new cube from the pool
    private CargoCube GetCargoCube(ResourceType resourceType)
    {

        //are there any cubes left to reuse?
        if (cargoCubes.Count == 0)
            return cargoManager.GetCargoCube(resourceType).GetComponent<CargoCube>();

        //Attempt to reuse cubes already at the storage complex
        CargoCube cube = this.cargoCubes.Peek();
        if (cube.cargoType == resourceType)
            return this.cargoCubes.Dequeue();
        else if(cube.cargoType == resourceType - 1)
        {
            //we're still on the previous resource type so dequeue and try again
            cargoCubes.Dequeue().gameObject.SetActive(false);
            return GetCargoCube(resourceType);
        }
        else if(cube.cargoType == resourceType + 1)
        {
            //if we're on to the next resource type then pull new cube from pool
            //cargoCubes.Dequeue().gameObject.SetActive(false);
            return cargoManager.GetCargoCube(resourceType).GetComponent<CargoCube>();
        }
        else
        {
            cargoCubes.Dequeue().gameObject.SetActive(false);
            return cargoManager.GetCargoCube(resourceType).GetComponent<CargoCube>();
        }
    }

    [Button]
    private void GetCubePositions()
    {
        CargoCube[] cubes = this.GetComponentsInChildren<CargoCube>();

        cubePositions = cubes.OrderBy(c => c.transform.position.y)
                             .ThenBy(c => c.transform.position.x)
                             .ThenBy(c => c.transform.position.z)
                             .Select(c => c.transform.localPosition)
                             .ToList();
    }
}
