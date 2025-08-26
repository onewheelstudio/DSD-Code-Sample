using HexGame.Resources;
using HexGame.Units;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuildingCargoDisplay : MonoBehaviour
{
    private Queue<CargoCube> cargoCubes = new();
    private Queue<CargoCube> tempCubes; 
    [SerializeField] private List<Vector3> cubePositions = new List<Vector3>();
    private UnitStorageBehavior usb;
    private CargoManager cargoManager;
    //private GlobalStorageBehavior gsb;
    //private bool isGlobal => gsb != null;
    private int maxCubesPerResource = 10;

    private Transform _transform;
    public Transform Transform
    {
        get
        {
            if (_transform == null)
                _transform = this.transform;
            return _transform;
        }
    }

    public static Camera mainCamera;
    private bool upDateCubes = false;
    private int lastUpdateFrame = 0;

    private void Awake()
    {
        usb = this.gameObject.GetComponentInParent<UnitStorageBehavior>();

        //if (usb is GlobalStorageBehavior)
        //    gsb = usb as GlobalStorageBehavior;

        cargoCubes.Clear();
        cargoManager = FindFirstObjectByType<CargoManager>();
        tempCubes = new Queue<CargoCube>(cubePositions.Count);
    }

    private void OnEnable()
    {
        mainCamera ??= FindFirstObjectByType<CameraTransitions>().GetComponent<Camera>(); 

        //if(gsb != null)
        //{
        //    GlobalStorageBehavior.resourceAdded += DisplayCubes;
        //    GlobalStorageBehavior.resourceRemoved += DisplayCubes;
        //}
        //else
        //{
        //    usb.resourceDelivered += DisplayCubes;
        //    usb.resourcePickedUp += DisplayCubes;
        //    usb.resourceUsed += DisplayCubes;
        //}
    }

    private void OnDisable()
    {
        ReturnAllCubes();
    }

    private void DisplayCubes(ResourceAmount resource)
    {
        //if (gsb == null)
        //    return;

        
        //if(Time.frameCount > lastUpdateFrame)
        //{
        //    lastUpdateFrame = Time.frameCount;
        //    DisplayCubes(gsb);
        //}
    }

    private void DisplayCubes(UnitStorageBehavior usb)
    {
        //if (!IsPositionVisible(mainCamera))
        //    return;

        //int totalCubes = 0;
        //foreach(var _resource in PlayerResources.resourceStored)
        //{
        //    float percent = PlayerResources.PercentFull(_resource);
        //    if(percent <= 0)
        //        continue;   
        //    int numberOfCubes = Mathf.CeilToInt(maxCubesPerResource * percent);

        //    for (int i = 0; i < numberOfCubes; i++)
        //    {
        //        if (totalCubes >= cubePositions.Count)
        //        {
        //            //Debug.LogError("Ran out of cube positions to display storage cubes.");
        //            break; 
        //        }

        //        CargoCube cube = GetCargoCube(_resource.type);
        //        tempCubes.Enqueue(cube);

        //        cube.Transform.SetPositionAndRotation(this.Transform.rotation * cubePositions[totalCubes] + this.Transform.position, this.Transform.rotation);
        //        cube.Transform.localScale = Vector3.one * 25;
        //        totalCubes++;
        //    }

        //    //check again so we can finish and clean up
        //    if (totalCubes >= cubePositions.Count)
        //    {
        //        break;
        //    }
        //}

        ////clean up any extra cubes
        //for (int i = 0; i < this.cargoCubes.Count; i++)
        //{
        //    CargoCube cube = this.cargoCubes.Dequeue();
        //    cube.ReturnToPool();
        //}

        ////need to carefully create new instance of the queue and clear the temp queue
        //this.cargoCubes = new Queue<CargoCube>(tempCubes); //we're done so update queue
        //tempCubes.Clear();
    }

    //attempt to reuse cubes already at the storage complex.
    //If we can't find a cube of the correct type then we'll pull a new cube from the pool
    private CargoCube GetCargoCube(ResourceType resourceType)
    {

        //are there any cubes left to reuse?
        if (cargoCubes.Count == 0)
            return cargoManager.GetCargoCube(resourceType);

        //Attempt to reuse cubes already at the storage complex
        CargoCube cube = this.cargoCubes.Peek();
        if (cube.cargoType == resourceType)
            return this.cargoCubes.Dequeue();
        else if(cube.cargoType == resourceType - 1)
        {
            //we're still on the previous resource type so dequeue and try again
            cargoCubes.Dequeue().ReturnToPool();
            return GetCargoCube(resourceType);
        }
        else if(cube.cargoType == resourceType + 1)
        {
            //if we're on to the next resource type then pull new cube from pool
            //cargoCubes.Dequeue().gameObject.SetActive(false);
            return cargoManager.GetCargoCube(resourceType);
        }
        else
        {
            cargoCubes.Dequeue().ReturnToPool();
            return cargoManager.GetCargoCube(resourceType);
        }
    }

    [Button]
    private void GetCubePositions()
    {
        CargoCube[] cubes = this.GetComponentsInChildren<CargoCube>();

        cubePositions = cubes.OrderBy(c => c.transform.position.y)
                             .ThenBy(c => c.transform.position.x)
                             .ThenBy(c => c.transform.position.z)
                             .Select(c => c.transform.position)
                             .ToList();
    }

    private void ReturnAllCubes()
    {
        foreach (var cube in cargoCubes)
        {
            if(cube == null || cube.gameObject == null)
                continue;

            cube.ReturnToPool();
        }
    }

    public bool IsPositionVisible(Camera cam)
    {
        Vector3 viewportPoint = cam.WorldToViewportPoint(Transform.position);

        // Check if the point is in front of the camera
        if (viewportPoint.z < 0)
            return false;

        // Check if the point is within the camera's viewport rectangle (0 to 1 in x and y)
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1;
    }
}
