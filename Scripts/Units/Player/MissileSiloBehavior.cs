using DG.Tweening;
using HexGame.Grid;
using HexGame.Units;
using OWS.ObjectPooling;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using System;

public class MissileSiloBehavior : UnitBehavior, IWaitForTarget
{
    private CameraControlActions cameraControls;
    private InputAction leftClick;
    private InputAction rightClick;

    private static ObjectPool<PoolObject> missilePool;

    [SerializeField] private Hex3 targetLocation;
    [SerializeField] private GameObject misslePrefab;
    private Projectile projectile;
    [SerializeField] private Transform launchPoint;
    [SerializeField] private float _launchDelay = 1f;
    private float launchDelay
    {
        get { return _launchDelay; }// / storageBehavior.efficiency; }
    }
    [SerializeField] private GameObject rightDoor;
    [SerializeField] private GameObject leftDoor;
    [SerializeField] private ParticleSystem smoke;

    private UnitStorageBehavior storageBehavior;
    [SerializeField] private ProjectileData projectileData;

    private void Awake()
    {
        cameraControls = new CameraControlActions();
        leftClick = cameraControls.PointerInput.LeftClick;
        rightClick = cameraControls.PointerInput.RightClick;

        missilePool = new ObjectPool<PoolObject>(misslePrefab);
        projectile = misslePrefab.GetComponent<Projectile>();

        storageBehavior = this.GetComponent<UnitStorageBehavior>();
    }

    private void OnEnable()
    {
        smoke.Stop();
        leftClick.performed += SetTarget;
        rightClick.performed += CancelTarget;
        DayNightManager.toggleDay += RequestResources;
    }

    private void OnDisable()
    {
        leftClick.performed -= SetTarget;
        rightClick.performed -= CancelTarget;
        DOTween.Kill(this,true);
        DayNightManager.toggleDay -= RequestResources;
    }

    private void RequestResources(int obj)
    {
        storageBehavior.CheckResourceLevels();
    }

    public override void StartBehavior()
    {
        _isFunctional = true;
    }

    public override void StopBehavior()
    {
        _isFunctional = false;
    }

    public void StartListeningForTarget()
    {
        Debug.Log("Waiting for Target");
        leftClick.Enable();
        rightClick.Enable();
    }

    private void SetTarget(InputAction.CallbackContext obj)
    {
        if(storageBehavior.efficiency <= 0.01f)
        {
            MessagePanel.ShowMessage("No workers.", this.gameObject);
            CancelTarget(obj);
            return;
        }

        this.targetLocation = HelperFunctions.GetMouseVector3OnPlane(true);

        if (Hex3.DistanceBetween(this.transform.position, this.targetLocation) <= projectile.projectileData.GetStat(Stat.minRange))
        {
            MessagePanel.ShowMessage("Inside Minimum Distance", this.gameObject);
            return;
        }
        else if(Hex3.DistanceBetween(this.transform.position, this.targetLocation) > projectile.projectileData.GetStat(Stat.maxRange))
        {
            MessagePanel.ShowMessage("Outside Maximum Distance", this.gameObject);
            return;
        }
        else if(!storageBehavior.TryUseAllResources(projectileData.projectileCost))
        {
            MessagePanel.ShowMessage("Missing needed resources to build ICBM.", this.gameObject);
            return;
        }

        StartCoroutine(DoLaunch(this.targetLocation));
        CancelTarget(obj);
    }

    private void CancelTarget(InputAction.CallbackContext obj)
    {
        Debug.Log("Cancel -> Waiting for Target");
        leftClick.Disable();
        rightClick.Disable();

        GameObject.FindObjectOfType<CursorManager>().SetCursor(CursorType.hex);
    }

    [Button]
    private void ForceLaunch(Hex3 location)
    {
        StartCoroutine(DoLaunch(location));
    }

    IEnumerator DoLaunch(Hex3 location)
    {
        GameObject missile = missilePool.Pull(launchPoint.position).gameObject;
        yield return StartCoroutine(OpenDoors());
        smoke.Play();
        yield return new WaitForSeconds(launchDelay);
        missile.GetComponent<ParabolicProjectile>().SetTarget(location);
        yield return StartCoroutine(CloseDoors());
        smoke.Stop();
    }

    IEnumerator OpenDoors()
    {
        rightDoor.transform.DOLocalMoveX(0.00282f, launchDelay);
        Tween doors = leftDoor.transform.DOLocalMoveX(-0.00282f, launchDelay);
        yield return doors.WaitForCompletion();
    }

    IEnumerator CloseDoors()
    {
        yield return new WaitForSeconds(launchDelay);
        rightDoor.transform.DOLocalMoveX(0, launchDelay);
        Tween doors = leftDoor.transform.DOLocalMoveX(0, launchDelay);
        yield return doors.WaitForCompletion();
    }
}
