using DG.Tweening;
using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HexGame.Resources.ResourceProductionBehavior;

public class RepairBehavior : UnitBehavior, IMove
{
    private UnitStorageBehavior usb;
    private PlayerUnit repairTarget;
    private List<PlayerUnit> repairTargets;
    private UnitManager unitManger;
    [SerializeField]
    private Drone[] repairDrones = new Drone[0];
    [SerializeField]
    private ResourceType repairResource = ResourceType.Energy;
    [SerializeField]
    private int repairAmount = 10;
    [SerializeField]
    private float repairTime = 1f;

    private bool readyToMove;
    public bool ReadyToMove => readyToMove;

    private bool unitsAreMoving;
    public bool UnitsAreMoving => unitsAreMoving;

    private WaitForSeconds repairInterval;

    public static event Action<RepairBehavior, UnitStorageBehavior> repairMovedStarted;
    public static event Action<RepairBehavior, Hex3> repairMovingToLocation;
    public static event Action<RepairBehavior, UnitStorageBehavior> repairMovedComplete;

    [Header("Movement")]
    [SerializeField] private HoverMoveBehavior hmb;
    [SerializeField] private CommunicationBase crystalWarning;
    private SphereCollider sphereCollider;
    private FogRevealer fogRevealer;
    private static EnemyCrystalManager ecm;
    private static List<EnemyCrystalBehavior> discoveredCrystals;
    public MeshRenderer movementTimer;
    private Material movementMaterial;
    private Tween movementIconTween;
    private Tween fadeTween;
    private Hex3 moveLocation;

    [Header("Repair Bits")]
    [SerializeField] private RepairIcon repairIcon;
    private List<RepairIcon> activeRepairIcons = new();
    private static ObjectPool<RepairIcon> repairIconPool;

    private void Awake()
    {
        unitManger = FindFirstObjectByType<UnitManager>();
        repairInterval = new WaitForSeconds(repairTime);
        sphereCollider = this.GetComponent<SphereCollider>();
        fogRevealer = this.GetComponentInChildren<FogRevealer>();
        hmb = this.GetComponentInChildren<HoverMoveBehavior>();
        usb = this.GetComponent<UnitStorageBehavior>();

        if (ecm == null)
        {
            ecm = FindFirstObjectByType<EnemyCrystalManager>();
            discoveredCrystals = new List<EnemyCrystalBehavior>();
        }

        //create instance of material to prevent changing the original material
        movementMaterial = Instantiate(movementTimer.material);
        movementTimer.material = movementMaterial;
        movementTimer.gameObject.SetActive(false);

        if(repairIconPool == null)
            repairIconPool = new ObjectPool<RepairIcon>(repairIcon, 5);
    }

    private void OnEnable()
    {
        usb = this.GetComponent<UnitStorageBehavior>();
        usb.resourceDelivered += ResourceDelivered;

        DayNightManager.toggleDay += StartRepairs;
        hmb.reachedDestination += ReachedDestination;
    }

    private void OnDisable()
    {
        usb.resourceDelivered -= ResourceDelivered;
        DayNightManager.toggleDay -= StartRepairs;
        hmb.reachedDestination -= ReachedDestination;
    }

    public override void StartBehavior()
    {
        isFunctional = numberOfWorkers > 0;
        //usb.RequestWorkers();
        DisplayWarning();
    }

    public override void StopBehavior()
    {
        isFunctional = false;
    }

    [Button]
    private void StartRepairs(int dayNumber)
    {
        repairTargets = GetRepairableTargets();
        StartCoroutine(DoRepairs(repairTargets));
    }

    private IEnumerator DoRepairs(List<PlayerUnit> repairTargets)
    {
        while(repairTargets.Count > 0 && DayNightManager.isDay && !UnitsAreMoving)
        {
            repairTarget = repairTargets[0];
            repairTargets.RemoveAt(0);
            Vector3 position = repairTarget.transform.position;

            while (GetRepairAmountNeeded(repairTarget) > 0 && !UnitsAreMoving && repairTarget != null)
            {
                yield return MoveDrones(position, repairDrones, repairInterval);
                if (repairTarget == null)
                    break;
                if (usb.TryUseResource(new ResourceAmount(repairResource, 1)))
                {
                    repairTarget.RestoreHP(repairAmount * numberOfWorkers / requiredNumberOfWorkers);
                }

                //usb.CheckResourceLevels(new ResourceAmount(ResourceType.Energy,1));
            }
        }
        
        ResetDrones();
    }

    private IEnumerator MoveDrones(Vector3 position, Drone[] repairDrones, WaitForSeconds repairInterval)
    {
        for (int i = 0; i < repairDrones.Length; i++)
        {
            if (i == repairDrones.Length - 1)
            {
                yield return repairDrones[i].DoDroneAction(position, repairInterval);
            }
            else
            {
                StartCoroutine(repairDrones[i].DoDroneAction(position));
            }
        }
    }

    private void ResourceDelivered(UnitStorageBehavior behavior, ResourceAmount amount)
    {
        if(amount.type == ResourceType.Workers)
        {
            numberOfWorkers = (int)amount.amount;
            isFunctional =numberOfWorkers > 0;
            DisplayWarning();
        }
        else if(amount.type == repairResource && amount.amount == usb.GetAmountStored(amount.type))
        {
            StartRepairs(0);
        }

        usb.CheckResourceLevels();
    }

    private void ResetDrones()
    {
        repairTarget = null;
        foreach (var drone in repairDrones)
        {
            drone.DoReturnToPosition();
        }
    }

    private List<PlayerUnit> GetRepairableTargets()
    {
        List<PlayerUnit> playerUnits = UnitManager.PlayerUnitsInRange(this.transform.position.ToHex3(), GetIntStat(Stat.maxRange));
        playerUnits.Sort((a, b) => GetRepairAmountNeeded(b).CompareTo(GetRepairAmountNeeded(a)));

        for (int i = playerUnits.Count - 1; i >= 0; i--)
        {
            if (GetRepairAmountNeeded(playerUnits[i]) <= 0 || playerUnits[i].unitType == PlayerUnitType.infantry)
                playerUnits.RemoveAt(i);
        }

        return playerUnits;
    }

    private float GetRepairAmountNeeded(PlayerUnit repairTarget)
    {
        return repairTarget.GetStat(Stat.hitPoints) - repairTarget.GetHP();
    }

    private void DisplayWarning()
    {
        List<ProductionIssue> issueList = new List<ProductionIssue>();
        if (requiredNumberOfWorkers > numberOfWorkers)
            issueList.Add(ProductionIssue.missingWorkers);
        else if (numberOfWorkers == 0)
            issueList.Add(ProductionIssue.noWorkers);

        if (issueList.Count == 0 && hasWarningIcon)
        {
            warningIconInstance.ToggleIconsOff();
            return;
        }

        if (warningIconInstance == null)
        {
            warningIconInstance = UnitManager.warningIcons.PullGameObject(this.transform.position, Quaternion.identity).GetComponent<WarningIcons>();
            warningIconInstance.transform.SetParent(this.transform);
        }

        warningIconInstance.SetWarnings(issueList);
    }

    public void ToggleReadyToMove()
    {
        if (UnitsAreMoving)
            return;

        if (ReadyToMove)
            CancelMove();
        else
            StartMove();
    }

    public void StartMove()
    {
        if (unitsAreMoving || ReadyToMove)
            return;
        SFXManager.PlaySFX(SFXType.click);
        readyToMove = true;
    }

    public void DoMove(Hex3 location)
    {
        if (unitsAreMoving)
        {
            return;
        }

        //flat out prevent move near enemy crystals
        if (ecm.IsCrystalNearBy(location, out EnemyCrystalBehavior crystal))
        {
            if (!discoveredCrystals.Contains(crystal))
            {
                discoveredCrystals.Add(crystal);
                ecm.DiscoverCrystal(crystal);
                CommunicationMenu.AddCommunication(crystalWarning, false);
            }
            return;
        }

        moveLocation = location;
        repairMovedStarted?.Invoke(this, usb);
        repairMovingToLocation?.Invoke(this, location);
        sphereCollider.enabled = false;
        readyToMove = false;
        StartCoroutine(DoMove(location.ToVector3())); 
    }

    public void CancelMove()
    {
        readyToMove = false;
        unitsAreMoving = false;
    }

    private void DestinationSet(Vector3 position)
    {
        Vector3 offset = Vector3.zero;
        HexTile tile = HexTileManager.GetHexTileAtLocation(position.ToHex3());

        if (tile != null && tile.TileType == HexTileType.hill)
            offset = new Vector3(0f, UnitManager.HillOffset, 0f);

        Vector3 startPosition = this.transform.position;
        this.transform.position = position + offset;
        hmb.transform.position = startPosition;
        hmb.SetDestination(position + offset);
    }

    private void ReachedDestination()
    {
        sphereCollider.enabled = true;
        unitsAreMoving = false;

        if (UnitSelectionManager.selectedUnit != null &&
            UnitSelectionManager.selectedUnit.gameObject == this.gameObject)
            StartMove();

        repairMovedComplete?.Invoke(this, usb);
        StopMovementIcon();
        StartRepairs(0);
    }


    private IEnumerator DoMove(Vector3 position)
    {
        if ((position - hmb.transform.position).sqrMagnitude < 0.1f) //attempt to prevent moving up and down if already at destination
        {
            yield break;
        }
        StartMovementIcon(position);
        unitsAreMoving = true;
        
        if(!AllDronesReturned())
        {
            StopCoroutine("DoRepairs"); //yuck
            ResetDrones();
            yield return new WaitWhile(() => !AllDronesReturned());
        }
        DestinationSet(position);
    }

    private bool AllDronesReturned()
    {
        foreach (var drone in repairDrones)
        {
            if(drone.IsDoing)
                return false;
        }

        return true;
    }

    private void StartMovementIcon(Vector3 position)
    {
        movementTimer.transform.SetParent(null);
        Quaternion rotation = new Quaternion();
        rotation.eulerAngles = new Vector3(90f, 30f, 0);
        movementTimer.transform.SetLocalPositionAndRotation(position + Vector3.up * 0.05f, rotation);
        movementTimer.gameObject.SetActive(true);
        fadeTween = movementMaterial.DOFade(0.7f, 0.25f);
        movementTimer.transform.localScale = Vector3.one * 2.1f;
        movementIconTween = movementTimer.transform.DOScale(Vector3.one * 1.8f, 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
    }

    private void StopMovementIcon()
    {
        fadeTween.Kill();
        movementMaterial.DOFade(0f, 0.25f)
                        .OnComplete(() => { movementIconTween.Kill(); movementTimer.gameObject.SetActive(false); });
        movementTimer.transform.SetParent(this.transform);
    }
}
