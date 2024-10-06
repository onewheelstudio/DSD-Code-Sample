using DG.Tweening;
using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using Nova;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class MarineBehavior : UnitBehavior, IHaveButtons, IMove
{
    [SerializeField] private List<Marine> marines;
    [SerializeField] private List<Marine> inactiveMarines;
    private UnitDetection unitDetection;
    [SerializeField] private LayerMask enemyLayer;
    private UnitStorageBehavior storageBehavior;
    private EnemyUnit target;
    private float hpPerMarine;
    private UnitStorageBehavior usb;
    private PlayerUnit playerUnit;
    private bool readyToMove = false;
    public bool ReadyToMove => readyToMove;

    public static event Action<MarineBehavior> marineMovedStarted;
    public static event Action<MarineBehavior, Hex3> maringMovingToLocation;
    public static event Action<MarineBehavior> marineMovedComplete;
    private bool unitsAreMoving
    {
        get
        {
            foreach (var marine in marines)
            {
                if (marine.isMoving)
                    return true;
            }

            return false;
        }
    }

    public bool UnitsAreMoving => unitsAreMoving;

    private List<Unit> enemyList = new List<Unit>();

    //stealth mode ;)
    private SphereCollider sphereCollider;
    private FogRevealer fogRevealer;
    private static EnemyCrystalManager ecm;
    private static List<EnemyCrystalBehavior> discoveredCrystals;
    [SerializeField] private CommunicationBase crystalWarning;
    private bool isMoving;

    private void Awake()
    {
        unitDetection = GetComponentInChildren<UnitDetection>();
        usb = this.GetComponent<UnitStorageBehavior>();
        playerUnit = this.GetComponent<PlayerUnit>();

        sphereCollider = this.GetComponent<SphereCollider>();
        fogRevealer = this.GetComponentInChildren<FogRevealer>();

        if(ecm == null)
        { 
            ecm = FindObjectOfType<EnemyCrystalManager>();
            discoveredCrystals = new List<EnemyCrystalBehavior>();
        }
    }

    private void OnEnable()
    {
        this.GetComponent<PlayerUnit>().unitDamaged += UnitDamaged;
        DayNightManager.toggleDay += RequestReinforcements;
        usb.resourceDelivered += WorkerDelivered;
        StartBehavior(); //pull this out later

        foreach (var marine in marines)
        {
            marine.reachedDestination += MarineReachedDestination;
        }

        DoFlyDown();
    }



    private void OnDisable()
    {
        this.GetComponent<PlayerUnit>().unitDamaged += UnitDamaged;
        DayNightManager.toggleDay -= RequestReinforcements;
        usb.resourceDelivered -= WorkerDelivered;

        foreach (var marine in marines)
        {
            marine.reachedDestination -= MarineReachedDestination;
        }

    }

    private void Update()
    {
        if (!isFunctional)
            return;

        if (!unitDetection.TargetIsInList(target))
            target = null;

        if (target == null || !target.gameObject.activeInHierarchy)
        {
            target = unitDetection.GetNearestTarget() as EnemyUnit; 
            return;
        }

        foreach (var marine in marines)
            marine.SetTarget(target, GetStat(Stat.reloadTime), GetStat(Stat.maxRange), GetStat(Stat.damage), TargetIsValid);

        //storageBehavior.CheckResourceLevels();
    }

    private void WorkerDelivered(UnitStorageBehavior usb, ResourceAmount resourceAmount)
    {
        if (resourceAmount.type != ResourceType.Workers)
            return;

        for (int i = 0; i < resourceAmount.amount; i++)
        {
            if (inactiveMarines.Count == 0)
                break;

            Marine marine = inactiveMarines[0];
            marine.gameObject.SetActive(true);
            inactiveMarines.RemoveAt(0);
            marines.Add(marine);
            playerUnit.RestoreHP(hpPerMarine);
        }
    }

    private void RequestReinforcements(int dayNumber = 0)
    {
        int neededWorkers = inactiveMarines.Count;
        if (neededWorkers == 0)
            return;
        for (int i = 0; i < neededWorkers; i++)
            usb.MakeDeliveryRequest(new HexGame.Resources.ResourceAmount(HexGame.Resources.ResourceType.Workers, 1));
    }

    private void UnitDamaged(Unit unit, float hitPoints)
    {
        if (marines.Count == 0)
            return;

        int value = Mathf.CeilToInt(hitPoints / hpPerMarine);
        value = marines.Count - value;

        if (marines.Count == 0)
            return;

        for (int i = 0; i < value; i++)
        {
            int marine = UnityEngine.Random.Range(0, marines.Count);
            marines[marine].gameObject.SetActive(false);
            inactiveMarines.Add(marines[marine]);
            marines.RemoveAt(marine);
        }
    }

    public override void StartBehavior()
    {
        isFunctional = true;
        marines = this.GetComponentsInChildren<Marine>().ToList();
        hpPerMarine = GetStat(Stat.hitPoints) / marines.Count;
    }

    public override void StopBehavior()
    {
        isFunctional = false;
    }


    private void DestinationSet(Vector3 position)
    {
        foreach (var marine in marines)
        {
            marine.SetDestination(position);
        }
    }

    private EnemyUnit GetTarget()
    {
        EnemyUnit target = null;
        float distance = Mathf.Infinity;

        foreach (var enemy in enemyList)
        {
            if (enemy == null || !enemy.gameObject.activeSelf)
                continue;

            float dist = (enemy.transform.position - this.transform.position).sqrMagnitude;
            //removed the "can see" as it seemly was causing issues with enemies not being seen, but directly above player unit
            if (dist < distance && dist > GetStat(Stat.minRange))// && CanSeeTarget(enemy))
            {
                distance = dist;
                target = enemy as EnemyUnit;
            }
        }
        enemyList.Remove(target);
        return target;
    }
    private bool CanSeeTarget(Unit target)
    {
        Ray ray = new Ray(this.transform.position + Vector3.up * 0.5f, target.transform.position + Vector3.up * 0.1f - this.transform.position - Vector3.up * 0.5f);

        if (Physics.Raycast(ray, out RaycastHit hit, GetStat(Stat.maxRange), enemyLayer))
        {
            if (enemyLayer == (enemyLayer | 1 << hit.transform.gameObject.layer))
                return true;
        }

        return false;
    }

    public bool TargetIsValid()
    {
        return target != null && !unitsAreMoving;
    }

    public List<PopUpButtonInfo> GetButtons()
    {
        return new List<PopUpButtonInfo>
        {
            new PopUpButtonInfo(ButtonType.move, null),
        };
    }

    private void DoFlyDown()
    {
        float startHeight = FindAnyObjectByType<CameraMovement>().GetComponentInChildren<Camera>().transform.position.y;
        foreach (var marine in marines)
        {
            marine.SetJetPack(true);
            marine.DoFlyDown(startHeight);
        }
    }

    #region IMove
    public void ToggleReadyToMove()
    {
        if (unitsAreMoving)
            return;

        if (readyToMove)
            CancelMove();
        else
            StartMove();
    }

    public void StartMove()
    {
        if (unitsAreMoving || readyToMove)
            return;

        SFXManager.PlaySFX(SFXType.click);
        readyToMove = true;
        //cursorManager.SetCursor(CursorType.moveUnit);
        foreach (var marine in marines)
        {
            marine.SetJetPack(true);
        }
    }



    public void DoMove(Hex3 location)
    {
        if (unitsAreMoving)
        {
            //if (!waitingToMove)
            //    StartCoroutine(MoveWhenReady(tempLocation));
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

        marineMovedStarted?.Invoke(this);
        maringMovingToLocation?.Invoke(this, location);
        DestinationSet(location);
        //cursorManager.SetCursor(CursorType.hex);
        sphereCollider.enabled = false;
        //fogRevealer.ToggleFogReveler(false, () => this.transform.position = location);
        StartCoroutine(DoFogMove(location.ToVector3()));
        this.transform.position = location.ToVector3();
        readyToMove = false;
    }

    public void CancelMove()
    {
        foreach (var marine in marines)
        {
            marine.SetJetPack(false);
        }
        SFXManager.PlaySFX(SFXType.click);
        //cursorManager.SetCursor(CursorType.hex);
        readyToMove = false;
    }

    private void MarineReachedDestination()
    {
        sphereCollider.enabled = true;
        //fogRevealer.ToggleFogReveler(true);
    }

    private IEnumerator DoFogMove(Vector3 position)
    {
        if ((position - this.transform.position).sqrMagnitude < 0.1f) //attempt to prevent moving up and down if already at destination
        {
            yield break;
        }

        float speed = GetStat(Stat.speed);
        float moveTime = (this.transform.position - position).magnitude / speed;
        float verticalTime = Mathf.Abs(1.25f) / speed;
        float totalTime = moveTime + verticalTime;
        float timer = 0f;
        Vector3 move = position - fogRevealer.transform.position;

        fogRevealer.transform.SetParent(null);
        //fogRevealer.StartMove();
        //yield return new WaitForSeconds(verticalTime * 2);
        Hex3 fogPosition = GetFogRevealerLocation().ToHex3();
        Hex3 targetPosition = position.ToHex3();
        while (fogPosition != targetPosition)
        {
            fogPosition = GetFogRevealerLocation().ToHex3();
            //timer += Time.fixedDeltaTime;
            timer += Time.deltaTime;
            //fogRevealer.transform.position += move * (Time.fixedDeltaTime / totalTime);
            //yield return new WaitForFixedUpdate();
            fogRevealer.transform.position = GetFogRevealerLocation();
            fogRevealer.UpdatePosition(fogPosition);
            Debug.Log("FogRevealer Position: " + fogRevealer.transform.position.ToHex3());
            yield return null;
        }
        fogRevealer.transform.position = position;
        fogRevealer.transform.SetParent(this.transform);
        //fogRevealer.CompleteMove();
        marineMovedComplete?.Invoke(this);
    }


    private Vector3 GetFogRevealerLocation()
    {
        Vector3 position = Vector3.zero;
        foreach (var marine in marines)
        {
            if(inactiveMarines.Contains(marine))
                continue;
            position += marine.transform.position;
        }

        position /= marines.Count;
        position.y = 0;
        return position;
    }
    #endregion
}
