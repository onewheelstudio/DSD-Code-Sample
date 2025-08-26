using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProduceUnitBehavior : UnitBehavior, IHavePopupInfo, IHaveButtons
{
    [SerializeField] private PlayerUnitType unitType;
    [SerializeField] private Stats unitStats;
    private UnitManager unitManager;
    private HexTileManager tileManager;
    private UnitStorageBehavior usb;
    private int numberRequested = 0;
    private WaitForSeconds delay = new WaitForSeconds(1f);

    [SerializeField,BoxGroup("Create On Awake")] 
    private bool createOnAwake = false;
    [SerializeField, ShowIf("createOnAwake"), BoxGroup("Create On Awake")] 
    private int numberToCreate = 1;
    private int maxNumber = 5;
    [SerializeField] private int range = 1;
    [SerializeField] private bool useButtons = true;

    private void Awake()
    {
        unitManager = GameObject.FindObjectOfType<UnitManager>();
        tileManager = GameObject.FindObjectOfType<HexTileManager>();
        usb = this.GetComponent<UnitStorageBehavior>();
        if(createOnAwake && !SaveLoadManager.Loading)
        {
            for (int i = 0; i < numberToCreate; i++)
            {
                CreateUnit(true);
            }
        }
    }

    private void OnEnable()
    {
        usb.resourceDelivered += CheckResources;
    }

    private void CheckResources()
    {
        CheckResources(usb, new ResourceAmount());
    }

    private void CheckResources(UnitStorageBehavior storage, ResourceAmount resource)
    {
        if (numberRequested <= 0)
            return;

        if (usb.HasAllResources(unitManager.GetUnitCost(unitType).ToList()))
            CreateUnit();
    }

    private void OnDisable()
    {
        usb.resourceDelivered += CheckResources;
    }

    public override void StartBehavior()
    {
        isFunctional = true;
        StartCoroutine(CheckCanCreate());
    }

    public override void StopBehavior()
    {
        isFunctional = false;
        StopAllCoroutines();
    }

    private void RequestUnit()
    {
        Debug.Log("REquesting unit");
        if (numberRequested >= maxNumber)
        {
            MessagePanel.ShowMessage($"Queue is full. Can't create more {unitType} yet.", this.gameObject);
            return;
        }

        numberRequested++;
        //RequestResources();
    }

    private bool CreateUnit(bool unitsAreFree = false)
    {
        List<Hex3> locationList = HexTileManager.GetFilledNeighborLocations(this.transform.position, range);
        if (locationList.Count == 0)
        {
            MessagePanel.ShowMessage($"No room to add {unitType}", this.gameObject);
            return false;
        }

        List<Vector3> positionList = new List<Vector3>();
        foreach (var location in locationList)
        {
            if (UnitManager.PlayerUnitAtLocation(location) == null && CanPlaceUnitAtPosition(location))
                positionList.Add(location);
        }

        if (positionList.Count == 0)
        {
            MessagePanel.ShowMessage($"No room to add {unitType}", this.gameObject);
            return false;
        }

        if(!unitsAreFree && !usb.TryUseAllResources(unitManager.GetUnitCost(unitType).ToList()))
            return false;

        Vector3 position = positionList[HexTileManager.GetNextInt(0, positionList.Count - 1)];
        GameObject newUnit = unitManager.InstantiateUnitByType(unitType, position);
        numberRequested--;
        MessagePanel.ShowMessage($"{unitType.ToNiceString()} created", newUnit);
        return true;
    }

    private bool CanPlaceUnitAtPosition(Vector3 position)
    {
        return unit.PlacementListContains(HexTileManager.GetHexTileAtLocation(position).TileType);
    }

    private IEnumerator CheckCanCreate()
    {
        while(true)
        {
            yield return delay;
            if (numberRequested > 0)
                CheckResources();
        }
    }

    private void CancelUnit()
    {
        Debug.Log("Remove Unit");
        numberRequested--;
    }

    List<PopUpInfo> IHavePopupInfo.GetPopupInfo()
    {
        if (createOnAwake)
            return new List<PopUpInfo>();

        return new List<PopUpInfo>()
        {
            new PopUpInfo($"\nUnits Requested: {numberRequested}\n", 100, PopUpInfo.PopUpInfoType.stats),
        };
    }

    public List<PopUpButtonInfo> GetButtons()
    {
        if (useButtons)
        {
            return new List<PopUpButtonInfo>()
            {
                new PopUpButtonInfo(ButtonType.addUnit, () => RequestUnit()),
                new PopUpButtonInfo(ButtonType.removeUnit, () => CancelUnit()),
            };
        }
        else
            return new List<PopUpButtonInfo>();
    }
}
